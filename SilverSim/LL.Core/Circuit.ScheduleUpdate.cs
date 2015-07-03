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
        private ThreadedClasses.BlockingQueue<UDPPacket> m_TxObjectPool = new BlockingQueue<UDPPacket>();
        private ThreadedClasses.BlockingQueue<ObjectUpdateInfo> m_TxObjectQueue = new BlockingQueue<ObjectUpdateInfo>();
        private bool m_TriggerFirstUpdate = false;

        public void ScheduleUpdate(ObjectUpdateInfo info)
        {
            m_TxObjectQueue.Enqueue(info);
        }

        public void AddScheduleUpdate(ObjectUpdateInfo info)
        {
            m_TxObjectQueue.Enqueue(info);
        }

        public void ScheduleFirstUpdate()
        {
            m_TriggerFirstUpdate = true;
            m_TxObjectQueue.Enqueue(null);
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
                lock (m_UnackedPacketsHash)
                {
                    m_UnackedPacketsHash.Add(p.SequenceNumber, p);
                }
                lock (m_UnackedBytesLock)
                {
                    m_UnackedBytes += p.DataLength;
                }
            }
            m_Server.SendPacketTo(p, RemoteEndPoint);
        }

        private UDPPacket GetTxObjectPoolPacket()
        {
            while (m_ObjectUpdateThreadRunning)
            {
                try
                {
                    UDPPacket p = m_TxObjectPool.Dequeue(1000);
                    p.Reset();
                    return p;
                }
                catch
                {
                    continue;
                }
            }
            return null;
        }

        private void SendFullUpdateMsg(UDPPacket full_packet, List<KeyValuePair<ObjectUpdateInfo, byte[]>> full_packet_data)
        {
            UInt64 regionHandle;
            try
            {
                regionHandle = Scene.RegionData.Location.RegionHandle;
            }
            catch
            {
                return;
            }
            full_packet.IsReliable = true;
            full_packet.WriteMessageType(MessageType.ObjectUpdate);
            full_packet.WriteUInt64(regionHandle);
            full_packet.WriteUInt16(65535); /* dilation */
            full_packet.WriteUInt8((byte)full_packet_data.Count);

            int offset = full_packet.DataPos;
            foreach (KeyValuePair<ObjectUpdateInfo, byte[]> kvp in full_packet_data)
            {
                full_packet.WriteBytes(kvp.Value);
                if (kvp.Key.Part.Owner.ID == AgentID)
                {
                    full_packet.Data[offset + (int)ObjectPart.FullFixedBlock1Offset.UpdateFlags] |= (byte)Types.Primitive.PrimitiveFlags.ObjectYouOwner;
                }
            }

            SendObjectUpdateMsg(full_packet);
        }

        private void HandleObjectUpdates()
        {
            UInt64 regionHandle;
            C5.TreeDictionary<UInt32, int> LastObjSerialNo = new C5.TreeDictionary<uint, int>();
            Queue<ObjectUpdateInfo>[] queues = new Queue<ObjectUpdateInfo>[2];
            Queue<ObjectUpdateInfo> physicalOutQueue = new Queue<ObjectUpdateInfo>();
            Queue<ObjectUpdateInfo> nonPhysicalOutQueue = new Queue<ObjectUpdateInfo>();
            queues[0] = physicalOutQueue;
            queues[1] = nonPhysicalOutQueue;
            regionHandle = Scene.RegionData.Location.RegionHandle;
            ObjectUpdateInfo objinfo;

            while (m_ObjectUpdateThreadRunning)
            {

                if ((physicalOutQueue.Count != 0 || nonPhysicalOutQueue.Count != 0) && m_AckThrottlingCount[(int)Message.QueueOutType.Object] < 100)
                {
                }
                else
                {
                    try
                    {
                        objinfo = m_TxObjectQueue.Dequeue(1000);
                    }
                    catch
                    {
                        continue;
                    }
                    try
                    {
                        if (objinfo.Part.ObjectGroup.IsAttachedToPrivate && objinfo.Part.ObjectGroup.Owner != Agent.Owner)
                        {
                            /* do not signal private attachments to anyone else than the owner */
                        }
                        else if (objinfo.IsPhysics && !objinfo.IsKilled && !objinfo.Part.ObjectGroup.IsAttached)
                        {
                            physicalOutQueue.Enqueue(objinfo);
                        }
                        else
                        {
                            nonPhysicalOutQueue.Enqueue(objinfo);
                        }

                    }
                    catch
                    {

                    }
                }

                int qcount = m_TxObjectQueue.Count;
                while(qcount-- > 0)
                {
                    objinfo = m_TxObjectQueue.Dequeue(0);
                    try
                    {
                        if (objinfo.Part.ObjectGroup.IsAttachedToPrivate && objinfo.Part.ObjectGroup.Owner != Agent.Owner)
                        {
                            /* do not signal private attachments to anyone else than the owner */
                        }
                        else if (objinfo.IsPhysics && !objinfo.IsKilled && !objinfo.Part.ObjectGroup.IsAttached)
                        {
                            physicalOutQueue.Enqueue(objinfo);
                        }
                        else
                        {
                            nonPhysicalOutQueue.Enqueue(objinfo);
                        }
                    }
                    catch
                    {

                    }
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
                UDPPacket terse_packet = null;
                byte terse_packet_count = 0;
                List<KeyValuePair<ObjectUpdateInfo, byte[]>> full_packet_data = new List<KeyValuePair<ObjectUpdateInfo, byte[]>>();
                int full_packet_data_length = 0;

                while (physicalOutQueue.Count != 0 || nonPhysicalOutQueue.Count != 0)
                {
                    int queueidx;
                    if (!m_ObjectUpdateThreadRunning)
                    {
                        break;
                    }
                    for (queueidx = 0; queueidx < queues.Length; ++queueidx)
                    {
                        Queue<ObjectUpdateInfo> q = queues[queueidx];
                        ObjectUpdateInfo ui;
                        if (q.Count == 0)
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

                        if (ui.IsKilled)
                        {
                            if (ko == null)
                            {
                                ko = new Messages.Object.KillObject();
                            }

                            ko.LocalIDs.Add(ui.LocalID);
                            LastObjSerialNo.Remove(ui.LocalID);
                            if (ko.LocalIDs.Count > 250)
                            {
                                SendMessage(ko);
                                ko = null;
                            }
                        }
                        else
                        {
                            bool dofull = false;
                            if(LastObjSerialNo.Contains(ui.LocalID))
                            {
                                int serialno = LastObjSerialNo[ui.LocalID];
                                dofull = serialno != ui.SerialNumber;
                            }
                            else
                            {
                                dofull = true;
                            }

                            if (dofull)
                            {
                                byte[] fullUpdate = ui.FullUpdate;

                                if (null != fullUpdate)
                                {

                                    if (full_packet_data != null && fullUpdate.Length + full_packet_data_length > 1400)
                                    {
                                        UDPPacket full_packet = GetTxObjectPoolPacket();
                                        if (full_packet == null)
                                        {
                                            break;
                                        }
                                        SendFullUpdateMsg(full_packet, full_packet_data);
                                        full_packet_data = null;
                                    }

                                    if (null == full_packet_data)
                                    {
                                        full_packet_data = new List<KeyValuePair<ObjectUpdateInfo, byte[]>>();
                                        full_packet_data_length = 0;
                                    }


                                    full_packet_data.Add(new KeyValuePair<ObjectUpdateInfo, byte[]>(ui, fullUpdate));
                                    full_packet_data_length += fullUpdate.Length;
                                }
                            }
                            else
                            {
                                byte[] terseUpdate = ui.TerseUpdate;

                                if (null != terseUpdate)
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
                                        terse_packet = GetTxObjectPoolPacket();
                                        if (terse_packet == null)
                                        {
                                            break;
                                        }
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

                if(full_packet_data != null)
                {
                    UDPPacket full_packet = GetTxObjectPoolPacket();
                    if (full_packet == null)
                    {
                        break;
                    }
                    SendFullUpdateMsg(full_packet, full_packet_data);
                    full_packet_data = null;
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
