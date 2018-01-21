// SilverSim is distributed under the terms of the
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
using SilverSim.Scene.Types.Object.Localization;
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
                    byte[] propUpdate = part.GetPropertiesUpdateData(agent.CurrentCulture);
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

        private readonly BlockingQueue<IObjUpdateInfo> m_TxObjectQueue = new BlockingQueue<IObjUpdateInfo>();
        private bool m_TriggerFirstUpdate;

        public void ScheduleUpdate(ObjectUpdateInfo info)
        {
            if((info.IsPhysics || info.IsMoving) && !info.IsKilled && !info.IsAttached && !EnablePhysicalOutQueue)
            {
                return;
            }
            m_TxObjectQueue.Enqueue(info);
        }

        public void ScheduleUpdate(AgentUpdateInfo info)
        {
            if ((info.IsPhysics || info.IsMoving) && !info.IsKilled && !info.IsAttached && !EnablePhysicalOutQueue)
            {
                return;
            }
            m_TxObjectQueue.Enqueue(info);
        }

        public void ScheduleFirstUpdate()
        {
            m_TriggerFirstUpdate = true;
            m_EnableObjectUpdates = true;
            EnablePhysicalOutQueue = true;
            m_TxObjectQueue.Enqueue(null);
        }

        private void SendObjectUpdateMsg(UDPPacket p)
        {
            p.OutQueue = Message.QueueOutType.Object;
            p.Flush();

            Interlocked.Increment(ref m_PacketsSent);
            p.EnqueuedAtTime = Environment.TickCount;
            p.TransferredAtTime = Environment.TickCount;
            SendCircuitPacket(p, (pck) =>
            {
                if (pck.IsReliable)
                {
                    Interlocked.Increment(ref m_AckThrottlingCount[(int)Message.QueueOutType.Object]);
                    lock (m_UnackedPacketsHash)
                    {
                        m_UnackedPacketsHash.Add(pck.SequenceNumber, pck);
                    }
                    lock (m_UnackedBytesLock)
                    {
                        m_UnackedBytes += pck.DataLength;
                    }
                }
            });
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

        private void SendFullUpdateMsg(UDPPacket full_packet, List<KeyValuePair<IObjUpdateInfo, byte[]>> full_packet_data)
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
            full_packet.WriteMessageNumber(MessageType.ObjectUpdate);
            full_packet.WriteUInt64(regionHandle);
            full_packet.WriteUInt16(65535); /* dilation */
            full_packet.WriteUInt8((byte)full_packet_data.Count);

            foreach (var kvp in full_packet_data)
            {
                int offset = full_packet.DataPos;
                full_packet.WriteBytes(kvp.Value);

                if(kvp.Key.GetType() == typeof(AgentUpdateInfo))
                {
                    var aui = kvp.Key as AgentUpdateInfo;
                    uint parentID = aui.ParentID;
                    uint localID = aui.LocalID;

                    full_packet.Data[offset + (int)AgentUpdateInfo.FullFixedBlock1Offset.LocalID] = (byte)(localID & 0xFF);
                    full_packet.Data[offset + (int)AgentUpdateInfo.FullFixedBlock1Offset.LocalID + 1] = (byte)((localID >> 8) & 0xFF);
                    full_packet.Data[offset + (int)AgentUpdateInfo.FullFixedBlock1Offset.LocalID + 2] = (byte)((localID >> 16) & 0xFF);
                    full_packet.Data[offset + (int)AgentUpdateInfo.FullFixedBlock1Offset.LocalID + 3] = (byte)((localID >> 24) & 0xFF);

                    full_packet.Data[offset + (int)AgentUpdateInfo.FullFixedBlock1Offset.ParentID] = (byte)(parentID & 0xFF);
                    full_packet.Data[offset + (int)AgentUpdateInfo.FullFixedBlock1Offset.ParentID + 1] = (byte)((parentID >> 8) & 0xFF);
                    full_packet.Data[offset + (int)AgentUpdateInfo.FullFixedBlock1Offset.ParentID + 2] = (byte)((parentID >> 16) & 0xFF);
                    full_packet.Data[offset + (int)AgentUpdateInfo.FullFixedBlock1Offset.ParentID + 3] = (byte)((parentID >> 24) & 0xFF);
                }
                else if (kvp.Key.GetType() == typeof(ObjectUpdateInfo))
                {
                    var oui = kvp.Key as ObjectUpdateInfo;
                    ObjectPart part = oui.Part;

                    uint parentID = oui.ParentID;
                    uint localID = oui.LocalID;

                    full_packet.Data[offset + (int)ObjectPartLocalizedInfo.FullFixedBlock1Offset.LocalID] = (byte)(localID & 0xFF);
                    full_packet.Data[offset + (int)ObjectPartLocalizedInfo.FullFixedBlock1Offset.LocalID + 1] = (byte)((localID >> 8) & 0xFF);
                    full_packet.Data[offset + (int)ObjectPartLocalizedInfo.FullFixedBlock1Offset.LocalID + 2] = (byte)((localID >> 16) & 0xFF);
                    full_packet.Data[offset + (int)ObjectPartLocalizedInfo.FullFixedBlock1Offset.LocalID + 3] = (byte)((localID >> 24) & 0xFF);

                    full_packet.Data[offset + (int)ObjectPartLocalizedInfo.FullFixedBlock1Offset.ParentID] = (byte)(parentID & 0xFF);
                    full_packet.Data[offset + (int)ObjectPartLocalizedInfo.FullFixedBlock1Offset.ParentID + 1] = (byte)((parentID >> 8) & 0xFF);
                    full_packet.Data[offset + (int)ObjectPartLocalizedInfo.FullFixedBlock1Offset.ParentID + 2] = (byte)((parentID >> 16) & 0xFF);
                    full_packet.Data[offset + (int)ObjectPartLocalizedInfo.FullFixedBlock1Offset.ParentID + 3] = (byte)((parentID >> 24) & 0xFF);

                    var b = new byte[4];
                    Buffer.BlockCopy(full_packet.Data, offset + (int)ObjectPartLocalizedInfo.FullFixedBlock1Offset.UpdateFlags, b, 0, 4);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(b);
                    }
                    var flags = (PrimitiveFlags)BitConverter.ToUInt32(b, 0);

                    if (SelectedObjects.Contains(kvp.Key.ID))
                    {
                        flags |= PrimitiveFlags.CreateSelected;
                    }
                    if (part.ObjectGroup.IsGroupOwned)
                    {
                        flags |= PrimitiveFlags.ObjectGroupOwned;
                    }
                    if (0 != (part.ObjectGroup.VehicleFlags & SilverSim.Scene.Types.Physics.Vehicle.VehicleFlags.CameraDecoupled))
                    {
                        flags |= PrimitiveFlags.CameraDecoupled;
                    }
                    if (part.Owner.EqualsGrid(Agent.Owner) && !part.ObjectGroup.IsGroupOwned)
                    {
                        flags |= PrimitiveFlags.ObjectYouOwner;
                        if ((part.OwnerMask & InventoryPermissionsMask.Move) != 0)
                        {
                            flags |= PrimitiveFlags.ObjectMove;
                        }
                        if ((part.OwnerMask & InventoryPermissionsMask.Transfer) != 0)
                        {
                            flags |= PrimitiveFlags.ObjectTransfer;
                        }
                        if ((part.OwnerMask & InventoryPermissionsMask.Modify) != 0)
                        {
                            flags |= PrimitiveFlags.ObjectModify | PrimitiveFlags.ObjectOwnerModify;
                        }
                        if ((part.OwnerMask & InventoryPermissionsMask.Copy) != 0)
                        {
                            flags |= PrimitiveFlags.ObjectCopy;
                        }
                    }
                    else
                    {
                        flags &= ~PrimitiveFlags.ObjectYouOwner;
                        if ((part.EveryoneMask & InventoryPermissionsMask.Move) != 0)
                        {
                            flags |= PrimitiveFlags.ObjectMove;
                        }
                        if ((part.EveryoneMask & InventoryPermissionsMask.Transfer) != 0)
                        {
                            flags |= PrimitiveFlags.ObjectTransfer;
                        }
                        if ((part.EveryoneMask & InventoryPermissionsMask.Modify) != 0)
                        {
                            flags |= PrimitiveFlags.ObjectModify;
                        }
                        if ((part.EveryoneMask & InventoryPermissionsMask.Copy) != 0)
                        {
                            flags |= PrimitiveFlags.ObjectCopy;
                        }

                        if (Agent.Group.Equals(part.Group))
                        {
                            if ((part.GroupMask & InventoryPermissionsMask.Move) != 0)
                            {
                                flags |= PrimitiveFlags.ObjectMove;
                            }
                            if ((part.GroupMask & InventoryPermissionsMask.Transfer) != 0)
                            {
                                flags |= PrimitiveFlags.ObjectTransfer;
                            }
                            if ((part.GroupMask & InventoryPermissionsMask.Modify) != 0)
                            {
                                flags |= PrimitiveFlags.ObjectModify;
                            }
                            if ((part.GroupMask & InventoryPermissionsMask.Copy) != 0)
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

                    Buffer.BlockCopy(b, 0, full_packet.Data, offset + (int)ObjectPartLocalizedInfo.FullFixedBlock1Offset.UpdateFlags, 4);
                }
            }

            SendObjectUpdateMsg(full_packet);
        }

        private readonly object m_PhysQueueThrottleLock = new object();
        private C5.TreeDictionary<uint, uint> m_PhysFullQueueThrottleInflight = new C5.TreeDictionary<uint, uint>();
        private C5.TreeDictionary<uint, uint> m_PhysTerseQueueThrottleInflight = new C5.TreeDictionary<uint, uint>();

        internal sealed class PhysFullObjectReleaseThrottle : Message
        {
            private readonly AgentCircuit m_Circuit;
            private readonly List<uint> m_ObjectsInFlight = new List<uint>();

            public PhysFullObjectReleaseThrottle(AgentCircuit circ)
            {
                m_Circuit = circ;
                OnSendCompletion += Acked;
            }

            ~PhysFullObjectReleaseThrottle()
            {
                OnSendCompletion -= Acked;
            }

            public void Add(uint localid)
            {
                m_ObjectsInFlight.Add(localid);
                lock (m_Circuit.m_PhysQueueThrottleLock)
                {
                    uint val = 0;
                    if(m_Circuit.m_PhysFullQueueThrottleInflight.Contains(localid))
                    {
                        val = m_Circuit.m_PhysFullQueueThrottleInflight[localid];
                    }
                    m_Circuit.m_PhysFullQueueThrottleInflight[localid] = val + 1;
                }
            }

            private void Acked(bool success)
            {
                foreach(uint localid in m_ObjectsInFlight)
                {
                    lock(m_Circuit.m_PhysQueueThrottleLock)
                    {
                        if(--m_Circuit.m_PhysFullQueueThrottleInflight[localid] == 0)
                        {
                            m_Circuit.m_PhysFullQueueThrottleInflight.Remove(localid);
                        }
                    }
                }
            }
        }

        internal sealed class PhysTerseObjectReleaseThrottle : Message
        {
            private readonly AgentCircuit m_Circuit;
            private readonly List<uint> m_ObjectsInFlight = new List<uint>();

            public PhysTerseObjectReleaseThrottle(AgentCircuit circ)
            {
                m_Circuit = circ;
                OnSendCompletion += Acked;
            }

            ~PhysTerseObjectReleaseThrottle()
            {
                OnSendCompletion -= Acked;
            }

            public void Add(uint localid)
            {
                m_ObjectsInFlight.Add(localid);
                lock (m_Circuit.m_PhysQueueThrottleLock)
                {
                    uint val = 0;
                    if (m_Circuit.m_PhysTerseQueueThrottleInflight.Contains(localid))
                    {
                        val = m_Circuit.m_PhysTerseQueueThrottleInflight[localid];
                    }
                    m_Circuit.m_PhysTerseQueueThrottleInflight[localid] = val + 1;
                }
            }

            private void Acked(bool success)
            {
                foreach (uint localid in m_ObjectsInFlight)
                {
                    lock (m_Circuit.m_PhysQueueThrottleLock)
                    {
                        if (--m_Circuit.m_PhysTerseQueueThrottleInflight[localid] == 0)
                        {
                            m_Circuit.m_PhysTerseQueueThrottleInflight.Remove(localid);
                        }
                    }
                }
            }
        }

        private bool CanPhysFullBeSent(uint localid)
        {
            lock(m_PhysQueueThrottleLock)
            {
                return !m_PhysFullQueueThrottleInflight.Contains(localid) || m_PhysFullQueueThrottleInflight[localid] < 10;
            }
        }

        private bool CanPhysTerseBeSent(uint localid)
        {
            lock (m_PhysQueueThrottleLock)
            {
                return !m_PhysTerseQueueThrottleInflight.Contains(localid) || m_PhysTerseQueueThrottleInflight[localid] < 10;
            }
        }

        private void HandleObjectUpdates()
        {
            Thread.CurrentThread.Name = $"Agent:ObjectUpdateHandler:Agent={AgentID}:Scene={m_Scene.ID}";
            UInt64 regionHandle;
            var LastObjSerialNo = new C5.TreeDictionary<uint, int>();
            var SendSelectedObjects = new C5.TreeSet<UUID>();
            var queues = new Queue<IObjUpdateInfo>[2];
            var physicalOutQueue = new Queue<IObjUpdateInfo>();
            var nonPhysicalOutQueue = new Queue<IObjUpdateInfo>();
            queues[0] = physicalOutQueue;
            queues[1] = nonPhysicalOutQueue;

            /* guard for a certain type of race condition here */
            while(Scene == null)
            {
                Thread.Sleep(100);
                if(!m_ObjectUpdateThreadRunning)
                {
                    return;
                }
            }
            regionHandle = Scene.GridPosition.RegionHandle;
            IObjUpdateInfo objinfo;

            while (m_ObjectUpdateThreadRunning)
            {
                objinfo = null;
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
                }
                else if(m_TxObjectQueue.Count > 0)
                {
                    objinfo = m_TxObjectQueue.Dequeue(0);
                }

                int qcount = m_TxObjectQueue.Count;
                while (objinfo != null)
                {
                    /* better use a compare here than the "is" */
                    if(objinfo.GetType() == typeof(AgentUpdateInfo))
                    {
                        /* agents pass */
                    }
                    else if (!m_EnableObjectUpdates)
                    {
                        continue;
                    }
                    try
                    {
                        if (objinfo.IsAttachedToPrivate && objinfo.Owner != Agent.Owner)
                        {
                            /* do not signal private attachments to anyone else than the owner */
                        }
                        else if ((objinfo.IsPhysics || objinfo.IsMoving) && !objinfo.IsKilled && !objinfo.IsAttached)
                        {
                            if (EnablePhysicalOutQueue)
                            {
                                /* only send physical updates on fully enabled circuits */
                                physicalOutQueue.Enqueue(objinfo);
                            }
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

                    objinfo = (qcount-- > 0) ? m_TxObjectQueue.Dequeue(0) : null;
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
                List<KeyValuePair<IObjUpdateInfo, byte[]>> phys_full_packet_data = null;
                int phys_full_packet_data_length = 0;
                PhysFullObjectReleaseThrottle phys_full_object_release = null;
                PhysTerseObjectReleaseThrottle phys_terse_object_release = null;

                UDPPacket nonphys_terse_packet = null;
                byte nonphys_terse_packet_count = 0;
                List<KeyValuePair<IObjUpdateInfo, byte[]>> nonphys_full_packet_data = null;
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
                        IObjUpdateInfo ui;
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

                            RemoveObjectImageUpdateLock(ui.ID);
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
                            var haveobjprop = false;
                            if(!ui.IsAlwaysFull && LastObjSerialNo.Contains(ui.LocalID))
                            {
                                int serialno = LastObjSerialNo[ui.LocalID];
                                dofull = serialno != ui.SerialNumber;
                            }
                            else
                            {
                                dofull = true;
                            }

                            byte[] propUpdate = ui.GetPropertiesUpdate(Agent.CurrentCulture);
                            if (propUpdate != null)
                            {
                                var oui = ui as ObjectUpdateInfo;
                                if (oui != null)
                                {
                                    var isSelected = SelectedObjects.Contains(ui.ID);
                                    var wasSelected = SendSelectedObjects.Contains(ui.ID);
                                    dofull |= (wasSelected && !isSelected) || (isSelected && !wasSelected);
                                    if (wasSelected && !isSelected)
                                    {
                                        SendSelectedObjects.Remove(ui.ID);
                                    }
                                    else if (isSelected && !wasSelected)
                                    {
                                        haveobjprop = true;
                                        SendSelectedObjects.Add(ui.ID);
                                        if (full_packet_objprop == null)
                                        {
                                            full_packet_objprop = new ObjectPropertiesTriggerMessage(this);
                                        }
                                        full_packet_objprop.ObjectParts.Add(oui.Part);
                                    }
                                }
                            }

                            if (dofull)
                            {
                                byte[] fullUpdate = ui.GetFullUpdate(Agent.CurrentCulture);

                                if (fullUpdate != null)
                                {
                                    if (ui.IsPhysics && !haveobjprop)
                                    {
                                        if (CanPhysFullBeSent(ui.LocalID))
                                        {
                                            bool foundobject = false;
                                            send_phys_packet:
                                            if (phys_full_packet_data != null && (fullUpdate.Length + phys_full_packet_data_length > 1400 || foundobject))
                                            {
                                                var full_packet = new UDPPacket();
                                                if (full_packet == null)
                                                {
                                                    break;
                                                }
                                                full_packet.IsReliable = true;
                                                full_packet.AckMessage = phys_full_object_release;
                                                phys_full_object_release = null;
                                                SendFullUpdateMsg(full_packet, phys_full_packet_data);
                                                phys_full_packet_data = null;
                                            }

                                            if (phys_full_packet_data == null)
                                            {
                                                phys_full_packet_data = new List<KeyValuePair<IObjUpdateInfo, byte[]>>();
                                                phys_full_packet_data_length = 0;
                                                phys_full_object_release = new PhysFullObjectReleaseThrottle(this);
                                            }
                                            else
                                            {
                                                foreach (KeyValuePair<IObjUpdateInfo, byte[]> kvp in phys_full_packet_data)
                                                {
                                                    if (kvp.Key.LocalID == ui.LocalID)
                                                    {
                                                        foundobject = true;
                                                        goto send_phys_packet;
                                                    }
                                                }
                                            }

                                            phys_full_object_release.Add(ui.LocalID);
                                            phys_full_packet_data.Add(new KeyValuePair<IObjUpdateInfo, byte[]>(ui, fullUpdate));
                                            phys_full_packet_data_length += fullUpdate.Length;
                                        }
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
                                            nonphys_full_packet_data = new List<KeyValuePair<IObjUpdateInfo, byte[]>>();
                                            nonphys_full_packet_data_length = 0;
                                        }
                                        else
                                        {
                                            foreach (KeyValuePair<IObjUpdateInfo, byte[]> kvp in nonphys_full_packet_data)
                                            {
                                                if(kvp.Key.LocalID == ui.LocalID)
                                                {
                                                    foundobject = true;
                                                    goto send_nonphys_packet;
                                                }
                                            }
                                        }

                                        nonphys_full_packet_data.Add(new KeyValuePair<IObjUpdateInfo, byte[]>(ui, fullUpdate));
                                        nonphys_full_packet_data_length += fullUpdate.Length;
                                    }
                                }
                            }
                            else
                            {
                                byte[] terseUpdate = ui.GetTerseUpdate(Agent.CurrentCulture);

                                if (terseUpdate != null)
                                {
                                    uint localID = ui.LocalID;
                                    terseUpdate[0] = (byte)(localID & 0xFF);
                                    terseUpdate[1] = (byte)((localID >> 8) & 0xFF);
                                    terseUpdate[2] = (byte)((localID >> 16) & 0xFF);
                                    terseUpdate[3] = (byte)((localID >> 24) & 0xFF);

                                    if (ui.IsPhysics)
                                    {
                                        if (CanPhysTerseBeSent(localID))
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
                                                phys_terse_packet = new UDPPacket();
                                                if (phys_terse_packet == null)
                                                {
                                                    break;
                                                }
                                                phys_terse_object_release = new PhysTerseObjectReleaseThrottle(this);
                                                phys_terse_packet.IsReliable = true;
                                                phys_terse_packet.AckMessage = phys_terse_object_release;
                                                phys_terse_packet.WriteMessageNumber(MessageType.ImprovedTerseObjectUpdate);
                                                phys_terse_packet.WriteUInt64(regionHandle);
                                                phys_terse_packet.WriteUInt16(65535); /* dilation */
                                                phys_terse_packet.WriteUInt8(0);
                                            }

                                            phys_terse_packet.WriteBytes(terseUpdate);
                                            phys_terse_object_release.Add(ui.LocalID);
                                            ++phys_terse_packet_count;
                                        }
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
                        var full_packet = new UDPPacket();
                        if (full_packet == null)
                        {
                            break;
                        }
                        full_packet.IsReliable = true;
                        full_packet.AckMessage = phys_full_object_release;
                        phys_full_object_release = null;
                        SendFullUpdateMsg(full_packet, phys_full_packet_data);
                        phys_full_packet_data = null;
                    }

                    if (phys_terse_packet != null && physicalOutQueue.Count == 0)
                    {
                        phys_terse_packet.Data[17] = phys_terse_packet_count;
                        SendObjectUpdateMsg(phys_terse_packet);
                        phys_terse_packet = null;
                        phys_terse_object_release = null;
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
