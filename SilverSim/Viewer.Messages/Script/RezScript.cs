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
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Viewer.Messages.Script
{
    [UDPMessage(MessageType.RezScript)]
    [Reliable]
    [NotTrusted]
    public class RezScript : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID GroupID;

        public UInt32 ObjectLocalID;
        public bool IsEnabled;

        public struct InventoryData
        {
            public UUID ItemID;
            public UUID FolderID;
            public UUID CreatorID;
            public UUID OwnerID;
            public UUID GroupID;
            public InventoryPermissionsMask BaseMask;
            public InventoryPermissionsMask OwnerMask;
            public InventoryPermissionsMask GroupMask;
            public InventoryPermissionsMask EveryoneMask;
            public InventoryPermissionsMask NextOwnerMask;
            public bool IsGroupOwned;
            public UUID TransactionID;
            public AssetType AssetType;
            public InventoryType InvType;
            public InventoryFlags Flags;
            public InventoryItem.SaleInfoData.SaleType SaleType;
            public Int32 SalePrice;
            public string Name;
            public string Description;
            public UInt32 CreationDate;
            public UInt32 CRC;
        }

        public InventoryData InventoryBlock = new InventoryData();

        public static Message Decode(UDPPacket p)
        {
            var m = new RezScript()
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),
                GroupID = p.ReadUUID(),
                ObjectLocalID = p.ReadUInt32(),
                IsEnabled = p.ReadBoolean()
            };
            m.InventoryBlock.ItemID = p.ReadUUID();
            m.InventoryBlock.FolderID = p.ReadUUID();
            m.InventoryBlock.CreatorID = p.ReadUUID();
            m.InventoryBlock.OwnerID = p.ReadUUID();
            m.InventoryBlock.GroupID = p.ReadUUID();
            m.InventoryBlock.BaseMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.InventoryBlock.OwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.InventoryBlock.GroupMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.InventoryBlock.EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.InventoryBlock.NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.InventoryBlock.IsGroupOwned = p.ReadBoolean();
            m.InventoryBlock.TransactionID = p.ReadUUID();
            m.InventoryBlock.AssetType = (AssetType)p.ReadInt8();
            m.InventoryBlock.InvType = (InventoryType)p.ReadInt8();
            m.InventoryBlock.Flags = (InventoryFlags)p.ReadUInt32();
            m.InventoryBlock.SaleType = (InventoryItem.SaleInfoData.SaleType)p.ReadUInt8();
            m.InventoryBlock.SalePrice = p.ReadInt32();
            m.InventoryBlock.Name = p.ReadStringLen8();
            m.InventoryBlock.Description = p.ReadStringLen8();
            m.InventoryBlock.CreationDate = p.ReadUInt32();
            m.InventoryBlock.CRC = p.ReadUInt32();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteUInt32(ObjectLocalID);
            p.WriteBoolean(IsEnabled);
            p.WriteUUID(InventoryBlock.ItemID);
            p.WriteUUID(InventoryBlock.FolderID);
            p.WriteUUID(InventoryBlock.CreatorID);
            p.WriteUUID(InventoryBlock.OwnerID);
            p.WriteUInt32((uint)InventoryBlock.BaseMask);
            p.WriteUInt32((uint)InventoryBlock.OwnerMask);
            p.WriteUInt32((uint)InventoryBlock.GroupMask);
            p.WriteUInt32((uint)InventoryBlock.EveryoneMask);
            p.WriteUInt32((uint)InventoryBlock.NextOwnerMask);
            p.WriteBoolean(InventoryBlock.IsGroupOwned);
            p.WriteUUID(InventoryBlock.TransactionID);
            p.WriteInt8((sbyte)InventoryBlock.AssetType);
            p.WriteInt8((sbyte)InventoryBlock.InvType);
            p.WriteUInt32((uint)InventoryBlock.Flags);
            p.WriteUInt8((byte)InventoryBlock.SaleType);
            p.WriteInt32(InventoryBlock.SalePrice);
            p.WriteStringLen8(InventoryBlock.Name);
            p.WriteStringLen8(InventoryBlock.Description);
            p.WriteUInt32(InventoryBlock.CreationDate);
            p.WriteUInt32(InventoryBlock.CRC);
        }
    }
}
