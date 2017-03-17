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
using System.Collections.Generic;
using MapType = SilverSim.Types.Map;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.BulkUpdateInventory)]
    [Reliable]
    [Trusted]
    [EventQueueGet("BulkUpdateInventory")]
    public class BulkUpdateInventory : Message
    {
        public UUID AgentID;
        public UUID SessionID; /* serialized in EQG */
        public UUID TransactionID;

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
            public UInt32 CallbackID;
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

            public uint Checksum
            {
                get
                {
                    uint checksum = 0;

                    checksum += AssetID.LLChecksum; // AssetID
                    checksum += FolderID.LLChecksum; // FolderID
                    checksum += ItemID.LLChecksum; // ItemID

                    checksum += CreatorID.LLChecksum; // CreatorID
                    checksum += OwnerID.LLChecksum; // OwnerID
                    checksum += GroupID.LLChecksum; // GroupID

                    checksum += (uint)OwnerMask;
                    checksum += (uint)NextOwnerMask;
                    checksum += (uint)EveryoneMask;
                    checksum += (uint)GroupMask;

                    checksum += (uint)Flags; // Flags
                    checksum += (uint)InvType; // InvType
                    checksum += (uint)Type; // Type 
                    checksum += CreationDate; // CreationDate
                    checksum += (uint)SalePrice;    // SalePrice
                    checksum += (uint)((uint)SaleType * 0x07073096); // SaleType
                    return checksum;
                }
            }
        }

        public List<ItemDataEntry> ItemData = new List<ItemDataEntry>();

        public BulkUpdateInventory()
        {

        }

        public void AddInventoryFolder(InventoryFolder folder)
        {
            FolderDataEntry e = new FolderDataEntry();
            e.FolderID = folder.ID;
            e.ParentID = folder.ParentFolderID;
            e.Type = folder.InventoryType;
            e.Name = folder.Name;
            FolderData.Add(e);
        }

        public void AddInventoryItem(InventoryItem item, UInt32 callbackID)
        {
            ItemDataEntry e = new ItemDataEntry();
            e.ItemID = item.ID;
            e.CallbackID = callbackID;
            e.FolderID = item.ParentFolderID;
            e.CreatorID = item.Creator.ID;
            e.OwnerID = item.Owner.ID;
            e.GroupID = item.Group.ID;
            e.BaseMask = item.Permissions.Base | item.Permissions.Current;
            e.OwnerMask = item.Permissions.Current;
            e.GroupMask = item.Permissions.Group;
            e.EveryoneMask = item.Permissions.EveryOne;
            e.NextOwnerMask = item.Permissions.NextOwner;
            e.IsGroupOwned = item.IsGroupOwned;
            e.AssetID = item.AssetID;
            e.Type = item.AssetType;
            e.InvType = item.InventoryType;
            e.Flags = item.Flags;
            e.SalePrice = item.SaleInfo.Price;
            e.SaleType = item.SaleInfo.Type;
            e.Name = item.Name;
            e.Description = item.Description;
            e.CreationDate = (uint)item.CreationDate.DateTimeToUnixTime();
            ItemData.Add(e);
        }

        public BulkUpdateInventory(
            UUID agentID, 
            UUID transactionID,
            UInt32 callbackID,
            InventoryItem item)
        {
            AgentID = agentID;
            TransactionID = transactionID;
            AddInventoryItem(item, callbackID);
        }

        public BulkUpdateInventory(
            UUID agentID,
            UUID transactionID,
            UInt32 callbackID,
            List<InventoryItem> items)
        {
            AgentID = agentID;
            TransactionID = transactionID;
            foreach (InventoryItem item in items)
            {
                AddInventoryItem(item, callbackID);
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(TransactionID);

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
                p.WriteUInt32(d.CallbackID);
                p.WriteUUID(d.FolderID);
                p.WriteUUID(d.CreatorID);
                p.WriteUUID(d.OwnerID);
                p.WriteUUID(d.GroupID);
                p.WriteUInt32((UInt32)d.BaseMask);
                p.WriteUInt32((UInt32)d.OwnerMask);
                p.WriteUInt32((UInt32)d.GroupMask);
                p.WriteUInt32((UInt32)d.EveryoneMask);
                p.WriteUInt32((UInt32)d.NextOwnerMask);
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
                p.WriteUInt32(d.Checksum);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            BulkUpdateInventory m = new BulkUpdateInventory();
            m.AgentID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();

            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                FolderDataEntry e = new FolderDataEntry();
                e.FolderID = p.ReadUUID();
                e.ParentID = p.ReadUUID();
                e.Type = (InventoryType)p.ReadInt8();
                e.Name = p.ReadStringLen8();
                m.FolderData.Add(e);
            }

            n = p.ReadUInt8();
            while(n-- != 0)
            {
                ItemDataEntry e = new ItemDataEntry();
                e.ItemID = p.ReadUUID();
                e.CallbackID = p.ReadUInt32();
                e.FolderID = p.ReadUUID();
                e.CreatorID = p.ReadUUID();
                e.OwnerID = p.ReadUUID();
                e.GroupID = p.ReadUUID();
                e.BaseMask = (InventoryPermissionsMask)p.ReadUInt32();
                e.OwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
                e.GroupMask = (InventoryPermissionsMask)p.ReadUInt32();
                e.EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32();
                e.NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
                e.IsGroupOwned = p.ReadBoolean();
                e.AssetID = p.ReadUUID();
                e.Type = (AssetType)p.ReadInt8();
                e.InvType = (InventoryType)p.ReadInt8();
                e.Flags = (InventoryFlags)p.ReadUInt32();
                e.SaleType = (InventoryItem.SaleInfoData.SaleType)p.ReadUInt8();
                e.SalePrice = p.ReadInt32();
                e.Name = p.ReadStringLen8();
                e.Description = p.ReadStringLen8();
                e.CreationDate = p.ReadUInt32();
                p.ReadUInt32(); /* checksum */
                m.ItemData.Add(e);
            }
            return m;
        }

        BinaryData EncodeU32ToBinary(uint val)
        {
            byte[] ret = BitConverter.GetBytes(val);
            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(ret);
            }
            return new BinaryData(ret);
        }

        public override IValue SerializeEQG()
        {
            MapType llsd = new MapType();

            AnArray agentDataArray = new AnArray();
            MapType agentData = new MapType();
            agentData.Add("AgentID", AgentID);
            agentData.Add("SessionID", SessionID);
            agentData.Add("TransactionID", TransactionID);
            agentDataArray.Add(agentData);
            llsd.Add("AgentData", agentDataArray);

            AnArray folderDataArray = new AnArray();

            foreach(FolderDataEntry folder in FolderData)
            {
                MapType folderData = new MapType();
                folderData.Add("FolderID", folder.FolderID);
                folderData.Add("AgentID", AgentID);
                folderData.Add("ParentID", folder.ParentID);
                folderData.Add("Type", (int)folder.Type);
                folderData.Add("Name", folder.Name);
                folderDataArray.Add(folderData);
            }
            llsd.Add("FolderData", folderDataArray);

            AnArray itemDataArray = new AnArray();
            foreach(ItemDataEntry item in ItemData)
            {
                MapType itemData = new MapType();
                itemData.Add("ItemID", item.ItemID);
                itemData.Add("FolderID", item.FolderID);
                itemData.Add("CreatorID", item.CreatorID);
                itemData.Add("OwnerID", item.OwnerID);
                itemData.Add("GroupID", item.GroupID);
                itemData.Add("BaseMask", EncodeU32ToBinary((uint)item.BaseMask));
                itemData.Add("OwnerMask", EncodeU32ToBinary((uint)item.OwnerMask));
                itemData.Add("GroupMask", EncodeU32ToBinary((uint)item.GroupMask));
                itemData.Add("EveryoneMask", EncodeU32ToBinary((uint)item.EveryoneMask));
                itemData.Add("NextOwnerMask", EncodeU32ToBinary((uint)item.NextOwnerMask));
                itemData.Add("GroupOwned", item.IsGroupOwned);
                itemData.Add("AssetID", item.AssetID);
                itemData.Add("Type", (int)item.Type);
                itemData.Add("InvType", (int)item.InvType);
                itemData.Add("Flags", EncodeU32ToBinary((uint)item.Flags));
                itemData.Add("SaleType", (int)item.SaleType);
                itemData.Add("SalePrice", item.SalePrice);
                itemData.Add("Name", item.Name);
                itemData.Add("Description", item.Description);
                itemData.Add("CreationDate", (int)item.CreationDate);
                itemData.Add("CRC", EncodeU32ToBinary(item.Checksum));
                itemData.Add("CallbackID", 0);
                itemDataArray.Add(itemData);
            }
            llsd.Add("ItemData", itemDataArray);

            return llsd;
        }
    }
}
