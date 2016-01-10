// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectPropertiesFamily)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class ObjectPropertiesFamily : Message
    {
        public UInt32 RequestFlags;
        public UUID ObjectID = UUID.Zero;
        public UUID OwnerID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public InventoryPermissionsMask BaseMask;
        public InventoryPermissionsMask OwnerMask;
        public InventoryPermissionsMask GroupMask;
        public InventoryPermissionsMask EveryoneMask;
        public InventoryPermissionsMask NextOwnerMask;
        public Int32 OwnershipCost;
        public InventoryItem.SaleInfoData.SaleType SaleType;
        public Int32 SalePrice;
        public UInt32 Category;
        public UUID LastOwnerID = UUID.Zero;
        public string Name;
        public string Description;

        public ObjectPropertiesFamily()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt32(RequestFlags);
            p.WriteUUID(ObjectID);
            p.WriteUUID(OwnerID);
            p.WriteUUID(GroupID);
            p.WriteUInt32((UInt32)BaseMask);
            p.WriteUInt32((UInt32)OwnerMask);
            p.WriteUInt32((UInt32)GroupMask);
            p.WriteUInt32((UInt32)EveryoneMask);
            p.WriteUInt32((UInt32)NextOwnerMask);
            p.WriteInt32(OwnershipCost);
            p.WriteUInt8((byte)SaleType);
            p.WriteInt32(SalePrice);
            p.WriteUInt32(Category);
            p.WriteUUID(LastOwnerID);
            p.WriteStringLen8(Name);
            p.WriteStringLen8(Description);
        }

        public static Message Decode(UDPPacket p)
        {
            ObjectPropertiesFamily m = new ObjectPropertiesFamily();
            m.RequestFlags = p.ReadUInt32();
            m.ObjectID = p.ReadUUID();
            m.OwnerID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.BaseMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.OwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.GroupMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.OwnershipCost = p.ReadInt32();
            m.SaleType = (InventoryItem.SaleInfoData.SaleType)p.ReadUInt8();
            m.SalePrice = p.ReadInt32();
            m.Category = p.ReadUInt32();
            m.LastOwnerID = p.ReadUUID();
            m.Name = p.ReadStringLen8();
            m.Description = p.ReadStringLen8();
            return m;
        }
    }
}
