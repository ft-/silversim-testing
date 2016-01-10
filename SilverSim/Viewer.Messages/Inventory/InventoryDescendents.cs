// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.InventoryDescendents)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class InventoryDescendents : Message
    {
        public UUID AgentID;
        public UUID FolderID;
        public UUID OwnerID;
        public Int32 Version;
        public Int32 Descendents;

        public struct FolderDataEntry
        {
            public UUID FolderID;
            public UUID ParentID;
            public InventoryType Type;
            public string Name;
        }

        public List<FolderDataEntry> FolderData = new List<FolderDataEntry>();
        
        public struct ItemDataEntry
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
            public UUID AssetID;
            public AssetType Type;
            public InventoryType InvType;
            public InventoryFlags Flags;
            public InventoryItem.SaleInfoData.SaleType SaleType;
            public Int32 SalePrice;
            public string Name;
            public string Description;
            public UInt32 CreationDate;
        }

        public List<ItemDataEntry> ItemData = new List<ItemDataEntry>();

        public InventoryDescendents()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(FolderID);
            p.WriteUUID(OwnerID);
            p.WriteInt32(Version);
            p.WriteInt32(Descendents);

            p.WriteUInt8((byte)FolderData.Count);
            foreach (FolderDataEntry d in FolderData)
            {
                p.WriteUUID(d.FolderID);
                p.WriteUUID(d.ParentID);
                p.WriteInt8((sbyte)d.Type);
                p.WriteStringLen8(d.Name);
            }

            p.WriteUInt8((byte)ItemData.Count);
            foreach (ItemDataEntry d in ItemData)
            {
                p.WriteUUID(d.ItemID);
                p.WriteUUID(d.FolderID);
                p.WriteUUID(d.CreatorID);
                p.WriteUUID(d.OwnerID);
                p.WriteUUID(d.GroupID);
                p.WriteUInt32((uint)d.BaseMask);
                p.WriteUInt32((uint)d.OwnerMask);
                p.WriteUInt32((uint)d.GroupMask);
                p.WriteUInt32((uint)d.EveryoneMask);
                p.WriteUInt32((uint)d.NextOwnerMask);
                p.WriteBoolean(d.IsGroupOwned);
                p.WriteUUID(d.AssetID);
                p.WriteInt8((sbyte)d.Type);
                p.WriteInt8((sbyte)d.InvType);
                p.WriteUInt32((uint)d.Flags);
                p.WriteUInt8((byte)d.SaleType);
                p.WriteInt32(d.SalePrice);
                p.WriteStringLen8(d.Name);
                p.WriteStringLen8(d.Description);
                p.WriteUInt32(d.CreationDate);

                uint checksum = 0;

                checksum += d.AssetID.LLChecksum; // AssetID
                checksum += d.FolderID.LLChecksum; // FolderID
                checksum += d.ItemID.LLChecksum; // ItemID

                checksum += d.CreatorID.LLChecksum; // CreatorID
                checksum += d.OwnerID.LLChecksum; // OwnerID
                checksum += d.GroupID.LLChecksum; // GroupID

                checksum += (uint)d.OwnerMask;
                checksum += (uint)d.NextOwnerMask;
                checksum += (uint)d.EveryoneMask;
                checksum += (uint)d.GroupMask;

                checksum += (uint)d.Flags; // Flags
                checksum += (uint)d.InvType; // InvType
                checksum += (uint)d.Type; // Type 
                checksum += d.CreationDate; // CreationDate
                checksum += (uint)d.SalePrice;    // SalePrice
                checksum += (uint)((uint)d.SaleType * 0x07073096); // SaleType

                p.WriteUInt32(checksum);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            InventoryDescendents m = new InventoryDescendents();
            m.AgentID = p.ReadUUID();
            m.FolderID = p.ReadUUID();
            m.OwnerID = p.ReadUUID();
            m.Version = p.ReadInt32();
            m.Descendents = p.ReadInt32();

            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                FolderDataEntry d = new FolderDataEntry();
                d.FolderID = p.ReadUUID();
                d.ParentID = p.ReadUUID();
                d.Type = (InventoryType)p.ReadInt8();
                d.Name = p.ReadStringLen8();
                m.FolderData.Add(d);
            }

            n = p.ReadUInt8();
            while(n-- != 0)
            {
                ItemDataEntry d = new ItemDataEntry();
                d.ItemID = p.ReadUUID();
                d.FolderID = p.ReadUUID();
                d.CreatorID = p.ReadUUID();
                d.OwnerID = p.ReadUUID();
                d.GroupID = p.ReadUUID();
                d.BaseMask = (InventoryPermissionsMask)p.ReadUInt32();
                d.OwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
                d.GroupMask = (InventoryPermissionsMask)p.ReadUInt32();
                d.EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32();
                d.NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
                d.IsGroupOwned = p.ReadBoolean();
                d.Type = (AssetType)p.ReadInt8();
                d.InvType = (InventoryType)p.ReadInt8();
                d.Flags = (InventoryFlags)p.ReadUInt32();
                d.SaleType = (InventoryItem.SaleInfoData.SaleType)p.ReadUInt8();
                d.SalePrice = p.ReadInt32();
                d.Name = p.ReadStringLen8();
                d.Description = p.ReadStringLen8();
                d.CreationDate = p.ReadUInt32();
                p.ReadUInt32(); /* checksum */
                m.ItemData.Add(d);
            }
            return m;
        }
    }
}
