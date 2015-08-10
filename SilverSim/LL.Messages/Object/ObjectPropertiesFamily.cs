// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.LL.Messages.Object
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
            p.WriteMessageType(Number);
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
    }
}
