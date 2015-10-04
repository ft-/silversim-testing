// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.RezObjectFromNotecard)]
    [Reliable]
    [NotTrusted]
    public class RezObjectFromNotecard : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID GroupID;

        public struct RezDataS
        {
            public UUID FromTaskID;
            public byte BypassRaycast;
            public Vector3 RayStart;
            public Vector3 RayEnd;
            public UUID RayTargetID;
            public bool RayEndIsIntersection;
            public bool RezSelected;
            public bool RemoveItem;
            public UInt32 ItemFlags;
            public InventoryPermissionsMask GroupMask;
            public InventoryPermissionsMask EveryoneMask;
            public InventoryPermissionsMask NextOwnerMask;
        }

        public RezDataS RezData;

        public struct NotecardDataS
        {
            public UUID NotecardItemID;
            public UUID ObjectID;
        }

        public NotecardDataS NotecardData;

        public List<UUID> InventoryData = new List<UUID>();

        public RezObjectFromNotecard()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RezObjectFromNotecard m = new RezObjectFromNotecard();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();

            m.RezData.FromTaskID = p.ReadUUID();
            m.RezData.BypassRaycast = p.ReadUInt8();
            m.RezData.RayStart = p.ReadVector3f();
            m.RezData.RayEnd = p.ReadVector3f();
            m.RezData.RayTargetID = p.ReadUUID();
            m.RezData.RayEndIsIntersection = p.ReadBoolean();
            m.RezData.RezSelected = p.ReadBoolean();
            m.RezData.RemoveItem = p.ReadBoolean();
            m.RezData.ItemFlags = p.ReadUInt32();
            m.RezData.GroupMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.RezData.EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.RezData.NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32();

            m.NotecardData.NotecardItemID = p.ReadUUID();
            m.NotecardData.ObjectID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i )
            {
                m.InventoryData.Add(p.ReadUUID());
            }

            return m;
        }
    }
}
