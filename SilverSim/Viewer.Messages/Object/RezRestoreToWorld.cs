// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.RezRestoreToWorld)]
    [Reliable]
    [NotTrusted]
    public class RezRestoreToWorld : Message
    {
        public UUID AgentID;
        public UUID SessionID;

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

        public RezRestoreToWorld()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RezRestoreToWorld m = new RezRestoreToWorld();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

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
            m.InvType = (InventoryType)p.ReadInt8();
            m.Flags = p.ReadUInt32();
            m.SaleType = (InventoryItem.SaleInfoData.SaleType)p.ReadUInt8();
            m.SalePrice = p.ReadInt32();
            m.Name = p.ReadStringLen8();
            m.Description = p.ReadStringLen8();
            m.CreationDate = p.ReadUInt32();
            m.CRC = p.ReadUInt32();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(ItemID);
            p.WriteUUID(FolderID);
            p.WriteUUID(CreatorID);
            p.WriteUUID(OwnerID);
            p.WriteUUID(GroupID);
            p.WriteUInt32((uint)BaseMask);
            p.WriteUInt32((uint)OwnerMask);
            p.WriteUInt32((uint)GroupMask);
            p.WriteUInt32((uint)EveryoneMask);
            p.WriteUInt32((uint)NextOwnerMask);
            p.WriteBoolean(IsGroupOwned);
            p.WriteUUID(TransactionID);
            p.WriteInt8((sbyte)AssetType);
            p.WriteInt8((sbyte)InvType);
            p.WriteUInt32(Flags);
            p.WriteUInt8((byte)SaleType);
            p.WriteInt32(SalePrice);
            p.WriteStringLen8(Name);
            p.WriteStringLen8(Description);
            p.WriteUInt32(CreationDate);
            p.WriteUInt32(CRC);
        }
    }
}
