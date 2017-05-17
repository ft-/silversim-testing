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
            return new ObjectPropertiesFamily()
            {
                RequestFlags = p.ReadUInt32(),
                ObjectID = p.ReadUUID(),
                OwnerID = p.ReadUUID(),
                GroupID = p.ReadUUID(),
                BaseMask = (InventoryPermissionsMask)p.ReadUInt32(),
                OwnerMask = (InventoryPermissionsMask)p.ReadUInt32(),
                GroupMask = (InventoryPermissionsMask)p.ReadUInt32(),
                EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32(),
                NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32(),
                OwnershipCost = p.ReadInt32(),
                SaleType = (InventoryItem.SaleInfoData.SaleType)p.ReadUInt8(),
                SalePrice = p.ReadInt32(),
                Category = p.ReadUInt32(),
                LastOwnerID = p.ReadUUID(),
                Name = p.ReadStringLen8(),
                Description = p.ReadStringLen8()
            };
        }
    }
}
