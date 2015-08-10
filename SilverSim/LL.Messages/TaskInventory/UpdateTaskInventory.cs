// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.LL.Messages.TaskInventory
{
    [UDPMessage(MessageType.UpdateTaskInventory)]
    [Reliable]
    [NotTrusted]
    public class UpdateTaskInventory : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 LocalID;
        public byte Key;
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
        public UInt32 Flags;
        public InventoryItem.SaleInfoData.SaleType SaleType;
        public Int32 SalePrice;
        public string Name;
        public string Description;
        public UInt32 CreationDate;
        public UInt32 CRC;

        public UpdateTaskInventory()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            UpdateTaskInventory m = new UpdateTaskInventory();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LocalID = p.ReadUInt32();
            m.Key = p.ReadUInt8();
            m.ItemID = p.ReadUUID();
            m.FolderID = p.ReadUUID();
            m.CreatorID = p.ReadUUID();
            m.OwnerID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.BaseMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.OwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.GroupMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.IsGroupOwned = p.ReadBoolean();
            m.TransactionID = p.ReadUUID();
            m.AssetType = (AssetType)p.ReadInt8();
            m.InvType = (InventoryType) p.ReadInt8();
            m.Flags = p.ReadUInt32();
            m.SaleType = (InventoryItem.SaleInfoData.SaleType)p.ReadUInt8();
            m.SalePrice = p.ReadInt32();
            m.Name = p.ReadStringLen8();
            m.Description = p.ReadStringLen8();
            m.CreationDate = p.ReadUInt32();
            m.CRC = p.ReadUInt32();

            return m;
        }
    }
}
