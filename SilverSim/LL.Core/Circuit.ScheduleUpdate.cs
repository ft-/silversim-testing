/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Scene.Types.Object;
using ThreadedClasses;
using System.Threading;
using SilverSim.Scene.Types.Agent;
using SilverSim.LL.Messages;

namespace SilverSim.LL.Core
{
    public partial class Circuit
    {
        private ThreadedClasses.NonblockingQueue<ObjectUpdateInfo> m_PhysicalOutQueue = new NonblockingQueue<ObjectUpdateInfo>();
        private ThreadedClasses.NonblockingQueue<ObjectUpdateInfo> m_NonPhysicalOutQueue = new NonblockingQueue<ObjectUpdateInfo>();
        private AutoResetEvent m_ObjectUpdateSignal = new AutoResetEvent(false);
        private bool m_TriggerFirstUpdate = false;

        public void ScheduleUpdate(ObjectUpdateInfo info)
        {
            AddScheduleUpdate(info);
            m_ObjectUpdateSignal.Set();
        }

        public void AddScheduleUpdate(ObjectUpdateInfo info)
        {
            if (info.Part.ObjectGroup.IsAttachedToPrivate && info.Part.ObjectGroup.Owner != Agent.Owner)
            {
                /* do not signal private attachments to anyone else than the owner */
            }
            else if (info.IsPhysics && !info.IsKilled && !info.Part.ObjectGroup.IsAttached)
            {
                m_PhysicalOutQueue.Enqueue(info);
            }
            else
            {
                m_NonPhysicalOutQueue.Enqueue(info);
            }
        }

        public void ScheduleFirstUpdate()
        {
            m_TriggerFirstUpdate = true;
            m_ObjectUpdateSignal.Set();
        }


        private void SendObjectUpdateMsg(UDPPacket p)
        {
            p.OutQueue = Message.QueueOutType.Object;
            p.Flush();
            p.SequenceNumber = NextSequenceNumber;
            int savedDataLength = p.DataLength;

            Interlocked.Increment(ref m_PacketsSent);
            p.EnqueuedAtTime = Environment.TickCount;
            p.TransferredAtTime = Environment.TickCount;
            if (p.IsReliable)
            {
                Interlocked.Increment(ref m_AckThrottlingCount[(int)Message.QueueOutType.Object]);
                p.IsResent = true;
                m_UnackedPackets[p.SequenceNumber] = p;
                lock (m_UnackedBytesLock)
                {
                    m_UnackedBytes += p.DataLength;
                }
            }
            m_Server.SendPacketTo(p, RemoteEndPoint);
        }

        private void HandleObjectUpdates()
        {
            UInt64 regionHandle;
            Dictionary<UInt32, int> LastObjSerialNo = new Dictionary<uint, int>();
            NonblockingQueue<ObjectUpdateInfo>[] queues = new NonblockingQueue<ObjectUpdateInfo>[2];
            queues[0] = m_PhysicalOutQueue;
            queues[1] = m_NonPhysicalOutQueue;
            regionHandle = Scene.RegionData.Location.RegionHandle;

            while (m_ObjectUpdateThreadRunning)
            {
                if ((m_PhysicalOutQueue.Count != 0 || m_NonPhysicalOutQueue.Count != 0) && m_AckThrottlingCount[(int)Message.QueueOutType.Object] < 100)
                {
                }
                else if(!m_ObjectUpdateSignal.WaitOne(1000))
                {
                    continue;
                }

                if(m_TriggerFirstUpdate)
                {
                    foreach (IAgent agent in Scene.RootAgents)
                    {
                        if (agent != this)
                        {
                            Scene.SendAgentObjectToAgent(agent, Agent);
                        }
                    }
                    foreach (ObjectUpdateInfo ui in Scene.UpdateInfos)
                    {
                        AddScheduleUpdate(ui);
                    }
                    m_TriggerFirstUpdate = false;
                }

                Messages.Object.KillObject ko = null;
                UDPPacket full_packet = null;
                UDPPacket terse_packet = null;
                byte full_packet_count = 0;
                byte terse_packet_count = 0;

                while (m_PhysicalOutQueue.Count != 0 || m_NonPhysicalOutQueue.Count != 0)
                {
                    foreach (NonblockingQueue<ObjectUpdateInfo> q in queues)
                    {
                        ObjectUpdateInfo ui;
                        if(q.Count == 0)
                        {
                            continue;
                        }
                        try
                        {
                            ui = q.Dequeue();
                        }
                        catch
                        {
                            continue;
                        }

                        if(ui.IsKilled)
                        {
                            if(ko == null)
                            {
                                ko = new Messages.Object.KillObject();
                            }

                            ko.LocalIDs.Add(ui.LocalID);
                            if(ko.LocalIDs.Count > 250)
                            {
                                SendMessage(ko);
                                ko = null;
                            }
                        }
                        else
                        {
                            bool dofull = false;
                            try
                            {
                                int serialno = LastObjSerialNo[ui.LocalID];
                                dofull = serialno != ui.SerialNumber;
                            }
                            catch
                            {
                                dofull = true;
                            }

                            if(dofull)
                            {
                                byte[] fullUpdate = ui.FullUpdate;

                                if(null != fullUpdate)
                                {

                                    if(full_packet != null && fullUpdate.Length + full_packet.DataLength > 1400)
                                    {
                                        full_packet.Data[17] = full_packet_count;
                                        SendObjectUpdateMsg(full_packet);
                                        full_packet = null;
                                        full_packet_count = 0;
                                    }

                                    if (null == full_packet)
                                    {
                                        full_packet = new UDPPacket();
                                        full_packet.IsReliable = true;
                                        full_packet.WriteMessageType(MessageType.ObjectUpdate);
                                        full_packet.WriteUInt64(regionHandle);
                                        full_packet.WriteUInt16(65535); /* dilation */
                                        full_packet.WriteUInt8(0);
                                    }


                                    int offset = full_packet.DataPos;
                                    full_packet.WriteBytes(fullUpdate);
                                    if (ui.Part.Owner.ID == AgentID)
                                    {
                                        full_packet.Data[offset + (int)ObjectPart.FullFixedBlock1Offset.UpdateFlags] |= (byte)Types.Primitive.PrimitiveFlags.ObjectYouOwner;
                                    }
                                    ++full_packet_count;
                                }
                            }
                            else
                            {
                                byte[] terseUpdate = ui.TerseUpdate;

                                if(null != terseUpdate)
                                {
                                    if (terse_packet != null && terseUpdate.Length + terse_packet.DataLength > 1400)
                                    {
                                        terse_packet.Data[17] = terse_packet_count;
                                        SendObjectUpdateMsg(terse_packet);
                                        terse_packet = null;
                                        terse_packet_count = 0;
                                    }

                                    if (null == terse_packet)
                                    {
                                        terse_packet = new UDPPacket();
                                        terse_packet.IsReliable = true;
                                        terse_packet.WriteMessageType(MessageType.ImprovedTerseObjectUpdate);
                                        terse_packet.WriteUInt64(regionHandle);
                                        terse_packet.WriteUInt16(65535); /* dilation */
                                        terse_packet.WriteUInt8(0);
                                    }
                                    terse_packet.WriteBytes(terseUpdate);
                                    ++terse_packet_count;
                                }
                            }
                        }
                    }
                }

                if(full_packet != null)
                {
                    full_packet.Data[17] = full_packet_count;
                    SendObjectUpdateMsg(full_packet);
                    full_packet = null;
                    full_packet_count = 0;
                }

                if (terse_packet != null)
                {
                    terse_packet.Data[17] = terse_packet_count;
                    SendObjectUpdateMsg(terse_packet);
                    terse_packet = null;
                    terse_packet_count = 0;
                }

                if(ko != null)
                {
                    SendMessage(ko);
                    ko = null;
                }
            }
        }
    }
}
