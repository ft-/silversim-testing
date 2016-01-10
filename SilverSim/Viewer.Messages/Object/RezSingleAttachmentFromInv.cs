// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.RezSingleAttachmentFromInv)]
    [Reliable]
    [NotTrusted]
    public class RezSingleAttachmentFromInv : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID ItemID;
        public UUID OwnerID;
        public AttachmentPoint AttachmentPoint;
        public UInt32 ItemFlags;
        public InventoryPermissionsMask GroupMask;
        public InventoryPermissionsMask EveryoneMask;
        public InventoryPermissionsMask NextOwnerMask;
        public string Name;
        public string Description;

        public RezSingleAttachmentFromInv()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RezSingleAttachmentFromInv m = new RezSingleAttachmentFromInv();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ItemID = p.ReadUUID();
            m.OwnerID = p.ReadUUID();
            m.AttachmentPoint = (AttachmentPoint)p.ReadUInt8();
            m.ItemFlags = p.ReadUInt32();
            m.GroupMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.Name = p.ReadStringLen8();
            m.Description = p.ReadStringLen8();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(ItemID);
            p.WriteUUID(OwnerID);
            p.WriteUInt8((byte)AttachmentPoint);
            p.WriteUInt32(ItemFlags);
            p.WriteUInt32((uint)GroupMask);
            p.WriteUInt32((uint)EveryoneMask);
            p.WriteUInt32((uint)NextOwnerMask);
            p.WriteStringLen8(Name);
            p.WriteStringLen8(Description);
        }
    }
}
