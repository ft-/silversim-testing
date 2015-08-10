// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Inventory
{
    [UDPMessage(MessageType.UpdateInventoryItem)]
    [Reliable]
    [NotTrusted]
    public class UpdateInventoryItem : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID TransactionID;

        public struct InventoryDataEntry
        {
            public UUID ItemID;
            public UUID FolderID;
            public UInt32 CallbackID;
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
            public AssetType Type;
            public InventoryType InvType;
            public UInt32 Flags;
            public InventoryItem.SaleInfoData.SaleType SaleType;
            public Int32 SalePrice;
            public string Name;
            public string Description;
            public UInt32 CreationDate;
            public UInt32 CRC;
        }

        public List<InventoryDataEntry> InventoryData = new List<InventoryDataEntry>();

        public UpdateInventoryItem()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            UpdateInventoryItem m = new UpdateInventoryItem();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                InventoryDataEntry d = new InventoryDataEntry();
                d.ItemID = p.ReadUUID();
                d.FolderID = p.ReadUUID();
                d.CallbackID = p.ReadUInt32();
                d.CreatorID = p.ReadUUID();
                d.OwnerID = p.ReadUUID();
                d.GroupID = p.ReadUUID();
                d.BaseMask = (InventoryPermissionsMask)p.ReadUInt32();
                d.OwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
                d.GroupMask = (InventoryPermissionsMask)p.ReadUInt32();
                d.EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32();
                d.NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
                d.IsGroupOwned = p.ReadBoolean();
                d.TransactionID = p.ReadUUID();
                d.Type = (AssetType)p.ReadInt8();
                d.InvType = (InventoryType)p.ReadInt8();
                d.Flags = p.ReadUInt32();
                d.SaleType = (InventoryItem.SaleInfoData.SaleType)p.ReadUInt8();
                d.SalePrice = p.ReadInt32();
                d.Name = p.ReadStringLen8();
                d.Description = p.ReadStringLen8();
                d.CreationDate = p.ReadUInt32();
                d.CRC = p.ReadUInt32();
                m.InventoryData.Add(d);
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(TransactionID);
            p.WriteUInt8((byte)InventoryData.Count);
            foreach (InventoryDataEntry d in InventoryData)
            {
                p.WriteUUID(d.ItemID);
                p.WriteUUID(d.FolderID);
                p.WriteUInt32(d.CallbackID);
                p.WriteUUID(d.CreatorID);
                p.WriteUUID(d.OwnerID);
                p.WriteUUID(d.GroupID);
                p.WriteUInt32((UInt32)d.BaseMask);
                p.WriteUInt32((UInt32)d.OwnerMask);
                p.WriteUInt32((UInt32)d.GroupMask);
                p.WriteUInt32((UInt32)d.EveryoneMask);
                p.WriteUInt32((UInt32)d.NextOwnerMask);
                p.WriteBoolean(d.IsGroupOwned);
                p.WriteUUID(d.TransactionID);
                p.WriteInt8((sbyte)d.Type);
                p.WriteInt8((sbyte)d.InvType);
                p.WriteUInt32(d.Flags);
                p.WriteUInt8((byte)d.SaleType);
                p.WriteInt32(d.SalePrice);
                p.WriteStringLen8(d.Name);
                p.WriteStringLen8(d.Description);
                p.WriteUInt32(d.CreationDate);
                p.WriteUInt32(d.CRC);
            }
        }
    }
}
