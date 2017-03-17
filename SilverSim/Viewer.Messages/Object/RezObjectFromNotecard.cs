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

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RezData.FromTaskID);
            p.WriteUInt8(RezData.BypassRaycast);
            p.WriteVector3f(RezData.RayStart);
            p.WriteVector3f(RezData.RayEnd);
            p.WriteUUID(RezData.RayTargetID);
            p.WriteBoolean(RezData.RayEndIsIntersection);
            p.WriteBoolean(RezData.RezSelected);
            p.WriteBoolean(RezData.RemoveItem);
            p.WriteUInt32(RezData.ItemFlags);
            p.WriteUInt32((uint)RezData.GroupMask);
            p.WriteUInt32((uint)RezData.EveryoneMask);
            p.WriteUInt32((uint)RezData.NextOwnerMask);

            p.WriteUUID(NotecardData.NotecardItemID);
            p.WriteUUID(NotecardData.ObjectID);

            p.WriteUInt8((byte)InventoryData.Count);
            foreach(UUID id in InventoryData)
            {
                p.WriteUUID(id);
            }
        }
    }
}
