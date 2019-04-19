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
using SilverSim.Scene.Types.Object.Parameters;
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
        private abstract class ChainMessage : Message
        {
            public Message ChainedMessage { get; set; }
        }

        private sealed class AvatarAnimationTriggerMessage : ChainMessage
        {
            private readonly AgentCircuit m_Circuit;

            public AvatarAnimationTriggerMessage(AgentCircuit circuit)
            {
                m_Circuit = circuit;
                OnSendCompletion += HandleCompletion;
            }

            public List<IAgent> Agents = new List<IAgent>();

            private void HandleCompletion(bool f)
            {
                ViewerAgent agent = m_Circuit.Agent;
                if (agent == null)
                {
                    return;
                }
                foreach (IAgent otheragent in Agents)
                {
                    m_Circuit.SendMessage(otheragent.GetAvatarAnimation());
                }
                ChainedMessage?.OnSendComplete(f);
            }
        }

        private sealed class ObjectAnimationTriggerMessage : ChainMessage
        {
            private readonly AgentCircuit m_Circuit;

            public ObjectAnimationTriggerMessage(AgentCircuit circuit)
            {
                m_Circuit = circuit;
                OnSendCompletion += HandleCompletion;
            }

            public List<ObjectPart> ObjectParts = new List<ObjectPart>();

            private void HandleCompletion(bool f)
            {
                ViewerAgent agent = m_Circuit.Agent;
                if (agent == null)
                {
                    return;
                }
                foreach(ObjectPart part in ObjectParts)
                {
                    part.AnimationController.SendAnimationsToAgent(agent);
                }
                ChainedMessage?.OnSendComplete(f);
            }
        }
     
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
                        m_Log.DebugFormat("Invalid object properties (ID {0})", part.ID);
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

        public void ScheduleUpdate(ObjectUpdateInfo info) => ScheduleObjUpdate(info);

        public void ScheduleUpdate(AgentUpdateInfo info) => ScheduleObjUpdate(info);

        private void ScheduleObjUpdate(IObjUpdateInfo info)
        {
            if ((info.IsPhysics || info.IsMoving) && !info.IsKilled && !info.IsAttached && !EnablePhysicalOutQueue)
            {
                return;
            }
            m_TxObjectQueue.Enqueue(info);
        }

        public void ScheduleFirstUpdate()
        {
            m_EnableObjectUpdates = true;
            EnablePhysicalOutQueue = true;
            m_TriggerFirstUpdate = true;
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
                    /* intentionally ignoring exceptions */
                }
            }
            return null;
        }

        private ushort GetPhysicsDilation()
        {
            double normfps = Scene.PhysicsScene?.PhysicsFPSNormalized ?? 0.0;
            return normfps > 1.0 ? (ushort)65535 : (ushort)(normfps * 65535);
        }

        private void SendFullUpdateMsg(UDPPacket full_packet, List<KeyValuePair<IObjUpdateInfo, byte[]>> full_packet_data)
        {
            ulong regionHandle;
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
            full_packet.WriteUInt16(GetPhysicsDilation()); /* dilation */
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

        private void SendCompressedUpdateMsg(UDPPacket compressed_packet, List<KeyValuePair<IObjUpdateInfo, byte[]>> compressed_packet_data)
        {
            ulong regionHandle;
            try
            {
                regionHandle = Scene.GridPosition.RegionHandle;
            }
            catch
            {
                return;
            }
            compressed_packet.WriteMessageNumber(MessageType.ObjectUpdateCompressed);
            compressed_packet.WriteUInt64(regionHandle);
            compressed_packet.WriteUInt16(GetPhysicsDilation()); /* dilation */
            compressed_packet.WriteUInt8((byte)compressed_packet_data.Count);

            foreach (var kvp in compressed_packet_data)
            {
                int offset = compressed_packet.DataPos;
                compressed_packet.WriteBytes(kvp.Value);

                if (kvp.Key.GetType() == typeof(ObjectUpdateInfo))
                {
                    var oui = kvp.Key as ObjectUpdateInfo;
                    ObjectPart part = oui.Part;

                    uint parentID = oui.ParentID;
                    uint localID = oui.LocalID;

                    compressed_packet.Data[offset + (int)ObjectPartLocalizedInfo.CompressedUpdateFixedOffset.LocalID] = (byte)(localID & 0xFF);
                    compressed_packet.Data[offset + (int)ObjectPartLocalizedInfo.CompressedUpdateFixedOffset.LocalID + 1] = (byte)((localID >> 8) & 0xFF);
                    compressed_packet.Data[offset + (int)ObjectPartLocalizedInfo.CompressedUpdateFixedOffset.LocalID + 2] = (byte)((localID >> 16) & 0xFF);
                    compressed_packet.Data[offset + (int)ObjectPartLocalizedInfo.CompressedUpdateFixedOffset.LocalID + 3] = (byte)((localID >> 24) & 0xFF);

                    byte dynflags = kvp.Value[(int)ObjectPartLocalizedInfo.CompressedUpdateFixedOffset.CompressedFlags];
                    if ((dynflags & 0x20) != 0)
                    {
                        int parentidoffset = (dynflags & 0x80) != 0 ? (int)ObjectPartLocalizedInfo.CompressedUpdateFixedOffset.ParentID_WithAngularVelocity : (int)ObjectPartLocalizedInfo.CompressedUpdateFixedOffset.ParentID_NoAngularVelocity;
                        parentidoffset += offset;
                        compressed_packet.Data[parentidoffset] = (byte)(parentID & 0xFF);
                        compressed_packet.Data[parentidoffset + 1] = (byte)((parentID >> 8) & 0xFF);
                        compressed_packet.Data[parentidoffset + 2] = (byte)((parentID >> 16) & 0xFF);
                        compressed_packet.Data[parentidoffset + 3] = (byte)((parentID >> 24) & 0xFF);
                    }

                    var b = new byte[4];
                    Buffer.BlockCopy(compressed_packet.Data, offset + (int)ObjectPartLocalizedInfo.CompressedUpdateFixedOffset.UpdateFlags, b, 0, 4);
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

                    Buffer.BlockCopy(b, 0, compressed_packet.Data, offset + (int)ObjectPartLocalizedInfo.CompressedUpdateFixedOffset.UpdateFlags, 4);
                }
            }

            SendObjectUpdateMsg(compressed_packet);
        }

        private readonly object m_PhysQueueThrottleLock = new object();
        private C5.TreeDictionary<uint, uint> m_PhysFullQueueThrottleInflight = new C5.TreeDictionary<uint, uint>();
        private C5.TreeDictionary<uint, uint> m_PhysCompressedQueueThrottleInflight = new C5.TreeDictionary<uint, uint>();

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

        internal sealed class PhysCompressedObjectReleaseThrottle : Message
        {
            private readonly AgentCircuit m_Circuit;
            private readonly List<uint> m_ObjectsInFlight = new List<uint>();

            public PhysCompressedObjectReleaseThrottle(AgentCircuit circ)
            {
                m_Circuit = circ;
                OnSendCompletion += Acked;
            }

            ~PhysCompressedObjectReleaseThrottle()
            {
                OnSendCompletion -= Acked;
            }

            public void Add(uint localid)
            {
                m_ObjectsInFlight.Add(localid);
                lock (m_Circuit.m_PhysQueueThrottleLock)
                {
                    uint val = 0;
                    if (m_Circuit.m_PhysCompressedQueueThrottleInflight.Contains(localid))
                    {
                        val = m_Circuit.m_PhysCompressedQueueThrottleInflight[localid];
                    }
                    m_Circuit.m_PhysCompressedQueueThrottleInflight[localid] = val + 1;
                }
            }

            private void Acked(bool success)
            {
                foreach (uint localid in m_ObjectsInFlight)
                {
                    lock (m_Circuit.m_PhysQueueThrottleLock)
                    {
                        if (--m_Circuit.m_PhysCompressedQueueThrottleInflight[localid] == 0)
                        {
                            m_Circuit.m_PhysCompressedQueueThrottleInflight.Remove(localid);
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

        private bool CanPhysCompressedBeSent(uint localid)
        {
            lock (m_PhysQueueThrottleLock)
            {
                return !m_PhysCompressedQueueThrottleInflight.Contains(localid) || m_PhysCompressedQueueThrottleInflight[localid] < 10;
            }
        }

        private bool IsLightLimited(IObjUpdateInfo ui) => ui.Owner != Agent?.Owner;

        private void HandleObjectUpdates()
        {
            Thread.CurrentThread.Name = $"Agent:ObjectUpdateHandler:Agent={AgentID}:Scene={m_Scene.ID}";
            ulong regionHandle;
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

                List<KeyValuePair<IObjUpdateInfo, byte[]>> phys_full_packet_data = null;
                int phys_full_packet_data_length = 0;

                List<KeyValuePair<IObjUpdateInfo, byte[]>> phys_compressed_packet_data = null;
                int phys_compressed_packet_data_length = 0;

                PhysFullObjectReleaseThrottle phys_full_object_release = null;
                PhysCompressedObjectReleaseThrottle phys_compressed_object_release = null;

                List<KeyValuePair<IObjUpdateInfo, byte[]>> nonphys_compressed_packet_data = null;
                int nonphys_compressed_packet_data_length = 0;

                List<KeyValuePair<IObjUpdateInfo, byte[]>> nonphys_full_packet_data = null;
                int nonphys_full_packet_data_length = 0;

                ObjectPropertiesTriggerMessage full_packet_objprop = null;
                ObjectAnimationTriggerMessage full_packet_objanim = null;
                AvatarAnimationTriggerMessage full_packet_agentanim = null;

                ObjectPropertiesTriggerMessage compressed_packet_objprop = null;
                ObjectAnimationTriggerMessage compressed_packet_objanim = null;
                AvatarAnimationTriggerMessage compressed_packet_agentanim = null;

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
                            if (LastObjSerialNo.Remove(ui.LocalID))
                            {
                                if (ko == null)
                                {
                                    ko = new KillObject();
                                }

                                RemoveObjectImageUpdateLock(ui.ID);
                                ko.LocalIDs.Add(ui.LocalID);
                                if (ko.LocalIDs.Count > 250)
                                {
                                    SendMessage(ko);
                                    ko = null;
                                }
                            }
                        }
                        else
                        {
                            bool dofull = ui.IsAlwaysFull;
                            bool mustBeNonPhys = false;
                            bool objknown = false;
                            bool isSelected = false;
                            bool wasSelected = false;
                            ObjectUpdateInfo oui = null;
                            AgentUpdateInfo aui = null;

                            if (LastObjSerialNo.Contains(ui.LocalID))
                            {
                                int serialno = LastObjSerialNo[ui.LocalID];
                                dofull |= ui.IsPhysics;// serialno != ui.SerialNumber;
                                objknown = true;
                            }
                            else
                            {
                                LastObjSerialNo[ui.LocalID] = ui.SerialNumber;
                            }

                            byte[] propUpdate = ui.GetPropertiesUpdate(Agent.CurrentCulture);
                            if (propUpdate != null)
                            {
                                oui = ui as ObjectUpdateInfo;
                                if (oui != null)
                                {
                                    isSelected = SelectedObjects.Contains(ui.ID);
                                    wasSelected = SendSelectedObjects.Contains(ui.ID);
                                }
                                else
                                {
                                    aui = ui as AgentUpdateInfo;
                                    if(aui != null && !objknown)
                                    {
                                        mustBeNonPhys = true;
                                    }
                                }
                            }

                            byte[] compressedUpdate = null;
                            if(!dofull)
                            {
                                compressedUpdate = IsLightLimited(ui) ? ui.GetCompressedUpdateLimited(Agent.CurrentCulture) : ui.GetCompressedUpdate(Agent.CurrentCulture);
                                if(compressedUpdate == null)
                                {
                                    dofull = true;
                                }
                            }

                            if (dofull)
                            {
                                byte[] fullUpdate = IsLightLimited(ui) ? ui.GetFullUpdateLimited(Agent.CurrentCulture) : ui.GetFullUpdate(Agent.CurrentCulture);

                                if (fullUpdate != null)
                                {
                                    if (ui.IsPhysics && !mustBeNonPhys)
                                    {
                                        if (CanPhysFullBeSent(ui.LocalID))
                                        {
                                            bool foundobject = false;
                                            send_phys_packet:
                                            if (phys_full_packet_data != null && (fullUpdate.Length + phys_full_packet_data_length > 1400 || phys_full_packet_data.Count == 255 || foundobject))
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
                                        if (nonphys_full_packet_data != null && (fullUpdate.Length + nonphys_full_packet_data_length > 1400 || nonphys_full_packet_data.Count == 255 || foundobject))
                                        {
                                            var full_packet = GetTxObjectPoolPacket();
                                            if (full_packet == null)
                                            {
                                                break;
                                            }
                                            foundobject = true;
                                            full_packet.IsReliable = true;

                                            ChainMessage chain = null;

                                            if(full_packet_agentanim != null)
                                            {
                                                chain = full_packet_agentanim;
                                                full_packet.AckMessage = full_packet_agentanim;
                                                full_packet_agentanim = null;
                                            }

                                            if(full_packet_objanim != null)
                                            {
                                                if(chain != null)
                                                {
                                                    chain.ChainedMessage = full_packet_objanim;
                                                }
                                                else
                                                {
                                                    full_packet.AckMessage = full_packet_objanim;
                                                }
                                                chain = full_packet_objanim;
                                                full_packet_objanim = null;
                                            }

                                            if(chain != null)
                                            {
                                                chain.ChainedMessage = full_packet_objprop;
                                            }
                                            else
                                            {
                                                full_packet.AckMessage = full_packet_objprop;
                                            }

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

                                        if (oui != null)
                                        {
                                            if (wasSelected && !isSelected)
                                            {
                                                SendSelectedObjects.Remove(ui.ID);
                                            }
                                            else if (isSelected && !wasSelected)
                                            {
                                                SendSelectedObjects.Add(ui.ID);
                                                if (full_packet_objprop == null)
                                                {
                                                    full_packet_objprop = new ObjectPropertiesTriggerMessage(this);
                                                }
                                                full_packet_objprop.ObjectParts.Add(oui.Part);
                                            }
                                            else if (!objknown && (oui.Part.ExtendedMesh.Flags & ExtendedMeshParams.MeshFlags.AnimatedMeshEnabled) != 0 && m_EnableObjectAnimation)
                                            {
                                                mustBeNonPhys = true;
                                                if (full_packet_objanim == null)
                                                {
                                                    full_packet_objanim = new ObjectAnimationTriggerMessage(this);
                                                }
                                                full_packet_objanim.ObjectParts.Add(oui.Part);
                                            }
                                        }
                                        else if (aui != null && !objknown)
                                        {
                                            if (full_packet_agentanim == null)
                                            {
                                                full_packet_agentanim = new AvatarAnimationTriggerMessage(this);
                                            }
                                            full_packet_agentanim.Agents.Add(aui.Agent);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (ui.IsPhysics && !mustBeNonPhys)
                                {
                                    if (CanPhysCompressedBeSent(ui.LocalID))
                                    {
                                        bool foundobject = false;
                                        send_phys_packet:
                                        if (phys_compressed_packet_data != null && (compressedUpdate.Length + phys_compressed_packet_data_length > 1400 || phys_compressed_packet_data.Count == 255 || foundobject))
                                        {
                                            var compressed_packet = new UDPPacket();
                                            if (compressed_packet == null)
                                            {
                                                break;
                                            }
                                            foundobject = false;
                                            compressed_packet.IsReliable = true;
                                            compressed_packet.AckMessage = phys_compressed_object_release;
                                            phys_compressed_object_release = null;
                                            SendCompressedUpdateMsg(compressed_packet, phys_compressed_packet_data);
                                            phys_compressed_packet_data = null;
                                        }

                                        if (phys_compressed_packet_data == null)
                                        {
                                            phys_compressed_packet_data = new List<KeyValuePair<IObjUpdateInfo, byte[]>>();
                                            phys_compressed_packet_data_length = 0;
                                            phys_compressed_object_release = new PhysCompressedObjectReleaseThrottle(this);
                                        }
                                        else
                                        {
                                            foreach (KeyValuePair<IObjUpdateInfo, byte[]> kvp in phys_compressed_packet_data)
                                            {
                                                if (kvp.Key.LocalID == ui.LocalID)
                                                {
                                                    foundobject = true;
                                                    goto send_phys_packet;
                                                }
                                            }
                                        }

                                        phys_compressed_object_release.Add(ui.LocalID);
                                        phys_compressed_packet_data.Add(new KeyValuePair<IObjUpdateInfo, byte[]>(ui, compressedUpdate));
                                        phys_compressed_packet_data_length += compressedUpdate.Length;
                                    }
                                }
                                else
                                {
                                    bool foundobject = false;
                                    send_nonphys_packet:
                                    if (nonphys_compressed_packet_data != null && (compressedUpdate.Length + nonphys_compressed_packet_data_length > 1400 || nonphys_compressed_packet_data.Count == 255 || foundobject))
                                    {
                                        var compressed_packet = GetTxObjectPoolPacket();
                                        if (compressed_packet == null)
                                        {
                                            break;
                                        }
                                        foundobject = false;
                                        compressed_packet.IsReliable = true;

                                        ChainMessage chain = null;

                                        if (compressed_packet_agentanim != null)
                                        {
                                            chain = compressed_packet_agentanim;
                                            compressed_packet.AckMessage = compressed_packet_agentanim;
                                            compressed_packet_agentanim = null;
                                        }

                                        if (compressed_packet_objanim != null)
                                        {
                                            if (chain != null)
                                            {
                                                chain.ChainedMessage = compressed_packet_objanim;
                                            }
                                            else
                                            {
                                                compressed_packet.AckMessage = compressed_packet_objanim;
                                            }
                                            chain = compressed_packet_objanim;
                                            compressed_packet_objanim = null;
                                        }

                                        if (chain != null)
                                        {
                                            chain.ChainedMessage = compressed_packet_objprop;
                                        }
                                        else
                                        {
                                            compressed_packet.AckMessage = compressed_packet_objprop;
                                        }

                                        compressed_packet_objprop = null;
                                        SendCompressedUpdateMsg(compressed_packet, nonphys_compressed_packet_data);
                                        nonphys_compressed_packet_data = null;
                                    }

                                    if (nonphys_compressed_packet_data == null)
                                    {
                                        nonphys_compressed_packet_data = new List<KeyValuePair<IObjUpdateInfo, byte[]>>();
                                        nonphys_compressed_packet_data_length = 0;
                                    }
                                    else
                                    {
                                        foreach (KeyValuePair<IObjUpdateInfo, byte[]> kvp in nonphys_compressed_packet_data)
                                        {
                                            if (kvp.Key.LocalID == ui.LocalID)
                                            {
                                                foundobject = true;
                                                goto send_nonphys_packet;
                                            }
                                        }
                                    }

                                    nonphys_compressed_packet_data.Add(new KeyValuePair<IObjUpdateInfo, byte[]>(ui, compressedUpdate));
                                    nonphys_compressed_packet_data_length += compressedUpdate.Length;

                                    if (oui != null)
                                    {
                                        if (wasSelected && !isSelected)
                                        {
                                            SendSelectedObjects.Remove(ui.ID);
                                        }
                                        else if (isSelected && !wasSelected)
                                        {
                                            SendSelectedObjects.Add(ui.ID);
                                            if (compressed_packet_objprop == null)
                                            {
                                                compressed_packet_objprop = new ObjectPropertiesTriggerMessage(this);
                                            }
                                            compressed_packet_objprop.ObjectParts.Add(oui.Part);
                                        }
                                        else if (!objknown && (oui.Part.ExtendedMesh.Flags & ExtendedMeshParams.MeshFlags.AnimatedMeshEnabled) != 0 && m_EnableObjectAnimation)
                                        {
                                            mustBeNonPhys = true;
                                            if (compressed_packet_objanim == null)
                                            {
                                                compressed_packet_objanim = new ObjectAnimationTriggerMessage(this);
                                            }
                                            compressed_packet_objanim.ObjectParts.Add(oui.Part);
                                        }
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


                    if (phys_compressed_packet_data != null && physicalOutQueue.Count == 0)
                    {
                        var compressed_packet = new UDPPacket();
                        if (compressed_packet == null)
                        {
                            break;
                        }
                        compressed_packet.IsReliable = true;
                        compressed_packet.AckMessage = phys_compressed_object_release;
                        phys_compressed_object_release = null;
                        SendCompressedUpdateMsg(compressed_packet, phys_compressed_packet_data);
                        phys_compressed_packet_data = null;
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

                    ChainMessage chain = null;

                    if (full_packet_agentanim != null)
                    {
                        chain = full_packet_agentanim;
                        full_packet.AckMessage = full_packet_agentanim;
                    }

                    if (full_packet_objanim != null)
                    {
                        if (chain != null)
                        {
                            chain.ChainedMessage = full_packet_objanim;
                        }
                        else
                        {
                            full_packet.AckMessage = full_packet_objanim;
                        }
                        chain = full_packet_objanim;
                    }

                    if (chain != null)
                    {
                        chain.ChainedMessage = full_packet_objprop;
                    }
                    else
                    {
                        full_packet.AckMessage = full_packet_objprop;
                    }

                    SendFullUpdateMsg(full_packet, nonphys_full_packet_data);
                }

                if (nonphys_compressed_packet_data != null)
                {
                    var compressed_packet = GetTxObjectPoolPacket();
                    if (compressed_packet == null)
                    {
                        break;
                    }
                    compressed_packet.IsReliable = true;

                    ChainMessage chain = null;

                    if (compressed_packet_agentanim != null)
                    {
                        chain = compressed_packet_agentanim;
                        compressed_packet.AckMessage = compressed_packet_agentanim;
                    }

                    if (compressed_packet_objanim != null)
                    {
                        if (chain != null)
                        {
                            chain.ChainedMessage = compressed_packet_objanim;
                        }
                        else
                        {
                            compressed_packet.AckMessage = compressed_packet_objanim;
                        }
                        chain = compressed_packet_objanim;
                    }

                    if (chain != null)
                    {
                        chain.ChainedMessage = compressed_packet_objprop;
                    }
                    else
                    {
                        compressed_packet.AckMessage = compressed_packet_objprop;
                    }

                    SendCompressedUpdateMsg(compressed_packet, nonphys_compressed_packet_data);
                }


                if(ko != null)
                {
                    SendMessage(ko);
                }
            }
        }
    }
}
