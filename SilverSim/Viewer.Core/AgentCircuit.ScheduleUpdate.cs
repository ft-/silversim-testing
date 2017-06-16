﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Object;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Viewer.Core
{
    public partial class AgentCircuit
    {
        #region ObjectProperties handler
        private sealed class ObjectPropertiesTriggerMessage : Message
        {
            private readonly AgentCircuit m_Circuit;

            public ObjectPropertiesTriggerMessage(AgentCircuit circuit)
            {
                m_Circuit = circuit;
                OnSendCompletion += HandleCompletion;
            }

            public List<ObjectPart> ObjectParts = new List<ObjectPart>();

            private void HandleCompletion(bool f)
            {
                ViewerAgent agent = m_Circuit.Agent;
                if(agent == null)
                {
                    return;
                }
                ObjectProperties props = null;
                int bytelen = 0;

                foreach (var part in ObjectParts)
                {
                    byte[] propUpdate = part.PropertiesUpdateData;
                    if (propUpdate == null)
                    {
                        return;
                    }

                    if (bytelen + propUpdate.Length > 1400)
                    {
                        m_Circuit.SendMessage(props);
                        bytelen = 0;
                        props = null;
                    }

                    if (props == null)
                    {
                        props = new ObjectProperties();
                    }

                    props.ObjectData.Add(propUpdate);
                    bytelen += propUpdate.Length;
                }

                if (props != null)
                {
                    m_Circuit.SendMessage(props);
                }
            }
        }
        #endregion

        private readonly BlockingQueue<ObjectUpdateInfo> m_TxObjectQueue = new BlockingQueue<ObjectUpdateInfo>();
        private bool m_TriggerFirstUpdate;

        public void ScheduleUpdate(ObjectUpdateInfo info)
        {
            m_TxObjectQueue.Enqueue(info);
        }

        public void ScheduleFirstUpdate()
        {
            m_TriggerFirstUpdate = true;
            m_EnableObjectUpdates = true;
            m_TxObjectQueue.Enqueue(null);
        }

        private void SendObjectUpdateMsg(UDPPacket p)
        {
            p.OutQueue = Message.QueueOutType.Object;
            p.Flush();
            p.SequenceNumber = NextSequenceNumber;

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
            Server.SendPacketTo(p, RemoteEndPoint);
        }

        private UDPPacket GetTxObjectPoolPacket()
        {
            while (m_ObjectUpdateThreadRunning)
            {
                try
                {
                    var p = m_TxObjectPool.Dequeue(1000);
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
                regionHandle = Scene.GridPosition.RegionHandle;
            }
            catch
            {
                return;
            }
            full_packet.IsReliable = true;
            full_packet.WriteMessageNumber(MessageType.ObjectUpdate);
            full_packet.WriteUInt64(regionHandle);
            full_packet.WriteUInt16(65535); /* dilation */
            full_packet.WriteUInt8((byte)full_packet_data.Count);

            foreach (var kvp in full_packet_data)
            {
                int offset = full_packet.DataPos;
                full_packet.WriteBytes(kvp.Value);
                var b = new byte[4];
                Buffer.BlockCopy(full_packet.Data, offset + (int)ObjectPart.FullFixedBlock1Offset.UpdateFlags, b, 0, 4);
                if(!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }
                var flags = (PrimitiveFlags)BitConverter.ToUInt32(b, 0);

                if(SelectedObjects.Contains(kvp.Key.Part.ID))
                {
                    flags |= PrimitiveFlags.CreateSelected;
                }
                if(kvp.Key.Part.ObjectGroup.IsGroupOwned)
                {
                    flags |= PrimitiveFlags.ObjectGroupOwned;
                }
                if(0 != (kvp.Key.Part.ObjectGroup.VehicleFlags & SilverSim.Scene.Types.Physics.Vehicle.VehicleFlags.CameraDecoupled))
                {
                    flags |= PrimitiveFlags.CameraDecoupled;
                }
                if (kvp.Key.Part.Owner.EqualsGrid(Agent.Owner) && !kvp.Key.Part.ObjectGroup.IsGroupOwned)
                {
                    flags |= PrimitiveFlags.ObjectYouOwner;
                    if ((kvp.Key.Part.OwnerMask & InventoryPermissionsMask.Move) != 0)
                    {
                        flags |= PrimitiveFlags.ObjectMove;
                    }
                    if ((kvp.Key.Part.OwnerMask & InventoryPermissionsMask.Transfer) != 0)
                    {
                        flags |= PrimitiveFlags.ObjectTransfer;
                    }
                    if ((kvp.Key.Part.OwnerMask & InventoryPermissionsMask.Modify) != 0)
                    {
                        flags |= PrimitiveFlags.ObjectModify | Types.Primitive.PrimitiveFlags.ObjectOwnerModify;
                    }
                    if ((kvp.Key.Part.OwnerMask & InventoryPermissionsMask.Copy) != 0)
                    {
                        flags |= PrimitiveFlags.ObjectCopy;
                    }
                }
                else
                {
                    flags &= ~PrimitiveFlags.ObjectYouOwner;
                    if ((kvp.Key.Part.EveryoneMask & InventoryPermissionsMask.Move) != 0)
                    {
                        flags |= PrimitiveFlags.ObjectMove;
                    }
                    if ((kvp.Key.Part.EveryoneMask & InventoryPermissionsMask.Transfer) != 0)
                    {
                        flags |= PrimitiveFlags.ObjectTransfer;
                    }
                    if ((kvp.Key.Part.EveryoneMask & InventoryPermissionsMask.Modify) != 0)
                    {
                        flags |= PrimitiveFlags.ObjectModify;
                    }
                    if ((kvp.Key.Part.EveryoneMask & InventoryPermissionsMask.Copy) != 0)
                    {
                        flags |= PrimitiveFlags.ObjectCopy;
                    }

                    if(Agent.Group.Equals(kvp.Key.Part.Group))
                    {
                        if ((kvp.Key.Part.GroupMask & InventoryPermissionsMask.Move) != 0)
                        {
                            flags |= PrimitiveFlags.ObjectMove;
                        }
                        if ((kvp.Key.Part.GroupMask & InventoryPermissionsMask.Transfer) != 0)
                        {
                            flags |= PrimitiveFlags.ObjectTransfer;
                        }
                        if ((kvp.Key.Part.GroupMask & InventoryPermissionsMask.Modify) != 0)
                        {
                            flags |= PrimitiveFlags.ObjectModify;
                        }
                        if ((kvp.Key.Part.GroupMask & InventoryPermissionsMask.Copy) != 0)
                        {
                            flags |= PrimitiveFlags.ObjectCopy;
                        }
                    }
                }
                flags |= PrimitiveFlags.ObjectAnyOwner;

                b = BitConverter.GetBytes((UInt32)flags);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }

                Buffer.BlockCopy(b, 0, full_packet.Data, offset + (int)ObjectPart.FullFixedBlock1Offset.UpdateFlags, 4);
            }

            SendObjectUpdateMsg(full_packet);
        }

        private void HandleObjectUpdates()
        {
            UInt64 regionHandle;
            var LastObjSerialNo = new C5.TreeDictionary<uint, int>();
            var SendSelectedObjects = new C5.TreeSet<UUID>();
            var queues = new Queue<ObjectUpdateInfo>[2];
            var physicalOutQueue = new Queue<ObjectUpdateInfo>();
            var nonPhysicalOutQueue = new Queue<ObjectUpdateInfo>();
            queues[0] = physicalOutQueue;
            queues[1] = nonPhysicalOutQueue;
            regionHandle = Scene.GridPosition.RegionHandle;
            ObjectUpdateInfo objinfo;

            while (m_ObjectUpdateThreadRunning)
            {
                if (!((physicalOutQueue.Count != 0 || nonPhysicalOutQueue.Count != 0) && m_AckThrottlingCount[(int)Message.QueueOutType.Object] < 100))
                {
                    try
                    {
                        objinfo = m_TxObjectQueue.Dequeue(1000);
                    }
                    catch
                    {
                        continue;
                    }

                    if(!m_EnableObjectUpdates)
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
                        /* ensure that no exception kills this thread unexpectedly */
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
                        /* ensure that no exception kills this thread unexpectedly */
                    }
                }

                if(m_TriggerFirstUpdate)
                {
                    foreach (var agent in Scene.RootAgents)
                    {
                        if (agent != this)
                        {
                            Scene.SendAgentObjectToAgent(agent, Agent);
                        }
                    }
                    foreach (var ui in Scene.UpdateInfos)
                    {
                        ScheduleUpdate(ui);
                    }
                    m_TriggerFirstUpdate = false;
                }

                KillObject ko = null;

                UDPPacket phys_terse_packet = null;
                byte phys_terse_packet_count = 0;
                List<KeyValuePair<ObjectUpdateInfo, byte[]>> phys_full_packet_data = null;
                int phys_full_packet_data_length = 0;

                UDPPacket nonphys_terse_packet = null;
                byte nonphys_terse_packet_count = 0;
                List<KeyValuePair<ObjectUpdateInfo, byte[]>> nonphys_full_packet_data = null;
                int nonphys_full_packet_data_length = 0;

                ObjectPropertiesTriggerMessage full_packet_objprop = null;

                while (physicalOutQueue.Count != 0 || nonPhysicalOutQueue.Count != 0)
                {
                    int queueidx;
                    if (!m_ObjectUpdateThreadRunning)
                    {
                        break;
                    }
                    for (queueidx = 0; queueidx < queues.Length; ++queueidx)
                    {
                        var q = queues[queueidx];
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
                                ko = new KillObject();
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
                            var dofull = false;
                            if(LastObjSerialNo.Contains(ui.LocalID))
                            {
                                int serialno = LastObjSerialNo[ui.LocalID];
                                dofull = serialno != ui.SerialNumber;
                            }
                            else
                            {
                                dofull = true;
                            }

                            var isSelected = SelectedObjects.Contains(ui.Part.ID);
                            var wasSelected = SendSelectedObjects.Contains(ui.Part.ID);
                            dofull |= (wasSelected && !isSelected) || (isSelected && !wasSelected);
                            if(wasSelected && !isSelected)
                            {
                                SendSelectedObjects.Remove(ui.Part.ID);
                            }
                            else if(isSelected && !wasSelected)
                            {
                                SendSelectedObjects.Add(ui.Part.ID);
                                if(full_packet_objprop == null)
                                {
                                    full_packet_objprop = new ObjectPropertiesTriggerMessage(this);
                                }
                                full_packet_objprop.ObjectParts.Add(ui.Part);
                            }

                            if (dofull)
                            {
                                byte[] fullUpdate = ui.FullUpdate;

                                if (fullUpdate != null)
                                {
                                    if (ui.IsPhysics)
                                    {
                                        if (phys_full_packet_data != null && fullUpdate.Length + phys_full_packet_data_length > 1400)
                                        {
                                            var full_packet = GetTxObjectPoolPacket();
                                            if (full_packet == null)
                                            {
                                                break;
                                            }
                                            full_packet.IsReliable = false;
                                            full_packet.AckMessage = full_packet_objprop;
                                            full_packet_objprop = null;
                                            SendFullUpdateMsg(full_packet, phys_full_packet_data);
                                            phys_full_packet_data = null;
                                        }

                                        if (phys_full_packet_data == null)
                                        {
                                            phys_full_packet_data = new List<KeyValuePair<ObjectUpdateInfo, byte[]>>();
                                            phys_full_packet_data_length = 0;
                                        }

                                        phys_full_packet_data.Add(new KeyValuePair<ObjectUpdateInfo, byte[]>(ui, fullUpdate));
                                        phys_full_packet_data_length += fullUpdate.Length;
                                    }
                                    else
                                    {
                                        bool foundobject = false;
send_nonphys_packet:
                                        if (nonphys_full_packet_data != null && (fullUpdate.Length + nonphys_full_packet_data_length > 1400 || foundobject))
                                        {
                                            var full_packet = GetTxObjectPoolPacket();
                                            if (full_packet == null)
                                            {
                                                break;
                                            }
                                            full_packet.IsReliable = true;
                                            full_packet.AckMessage = full_packet_objprop;
                                            full_packet_objprop = null;
                                            SendFullUpdateMsg(full_packet, nonphys_full_packet_data);
                                            nonphys_full_packet_data = null;
                                        }

                                        if (nonphys_full_packet_data == null)
                                        {
                                            nonphys_full_packet_data = new List<KeyValuePair<ObjectUpdateInfo, byte[]>>();
                                            nonphys_full_packet_data_length = 0;
                                        }
                                        else
                                        {
                                            foreach (KeyValuePair<ObjectUpdateInfo, byte[]> kvp in nonphys_full_packet_data)
                                            {
                                                if(kvp.Key.LocalID == ui.LocalID)
                                                {
                                                    foundobject = true;
                                                    goto send_nonphys_packet;
                                                }
                                            }
                                        }

                                        nonphys_full_packet_data.Add(new KeyValuePair<ObjectUpdateInfo, byte[]>(ui, fullUpdate));
                                        nonphys_full_packet_data_length += fullUpdate.Length;
                                    }
                                }
                            }
                            else
                            {
                                byte[] terseUpdate = ui.TerseUpdate;

                                if (terseUpdate != null)
                                {
                                    if (ui.IsPhysics)
                                    {
                                        if (phys_terse_packet != null && terseUpdate.Length + phys_terse_packet.DataLength > 1400)
                                        {
                                            phys_terse_packet.Data[17] = phys_terse_packet_count;
                                            SendObjectUpdateMsg(phys_terse_packet);
                                            phys_terse_packet = null;
                                            phys_terse_packet_count = 0;
                                        }

                                        if (phys_terse_packet == null)
                                        {
                                            phys_terse_packet = GetTxObjectPoolPacket();
                                            if (phys_terse_packet == null)
                                            {
                                                break;
                                            }
                                            phys_terse_packet.IsReliable = false;
                                            phys_terse_packet.WriteMessageNumber(MessageType.ImprovedTerseObjectUpdate);
                                            phys_terse_packet.WriteUInt64(regionHandle);
                                            phys_terse_packet.WriteUInt16(65535); /* dilation */
                                            phys_terse_packet.WriteUInt8(0);
                                        }
                                        phys_terse_packet.WriteBytes(terseUpdate);
                                        ++phys_terse_packet_count;
                                    }
                                    else
                                    {
                                        if (nonphys_terse_packet != null && terseUpdate.Length + nonphys_terse_packet.DataLength > 1400)
                                        {
                                            nonphys_terse_packet.Data[17] = nonphys_terse_packet_count;
                                            SendObjectUpdateMsg(nonphys_terse_packet);
                                            nonphys_terse_packet = null;
                                            nonphys_terse_packet_count = 0;
                                        }

                                        if (nonphys_terse_packet == null)
                                        {
                                            nonphys_terse_packet = GetTxObjectPoolPacket();
                                            if (nonphys_terse_packet == null)
                                            {
                                                break;
                                            }
                                            nonphys_terse_packet.IsReliable = true;
                                            nonphys_terse_packet.WriteMessageNumber(MessageType.ImprovedTerseObjectUpdate);
                                            nonphys_terse_packet.WriteUInt64(regionHandle);
                                            nonphys_terse_packet.WriteUInt16(65535); /* dilation */
                                            nonphys_terse_packet.WriteUInt8(0);
                                        }
                                        nonphys_terse_packet.WriteBytes(terseUpdate);
                                        ++nonphys_terse_packet_count;
                                    }
                                }
                            }
                        }
                    }

                    if (phys_full_packet_data != null && physicalOutQueue.Count == 0)
                    {
                        var full_packet = GetTxObjectPoolPacket();
                        if (full_packet == null)
                        {
                            break;
                        }
                        full_packet.IsReliable = true;
                        full_packet.AckMessage = full_packet_objprop;
                        SendFullUpdateMsg(full_packet, phys_full_packet_data);
                    }

                    if (phys_terse_packet != null && physicalOutQueue.Count == 0)
                    {
                        phys_terse_packet.Data[17] = phys_terse_packet_count;
                        SendObjectUpdateMsg(phys_terse_packet);
                    }
                }

                if (nonphys_full_packet_data != null)
                {
                    var full_packet = GetTxObjectPoolPacket();
                    if (full_packet == null)
                    {
                        break;
                    }
                    full_packet.IsReliable = true;
                    full_packet.AckMessage = full_packet_objprop;
                    SendFullUpdateMsg(full_packet, nonphys_full_packet_data);
                }

                if (nonphys_terse_packet != null)
                {
                    nonphys_terse_packet.Data[17] = nonphys_terse_packet_count;
                    SendObjectUpdateMsg(nonphys_terse_packet);
                }

                if(ko != null)
                {
                    SendMessage(ko);
                }
            }
        }
    }
}
