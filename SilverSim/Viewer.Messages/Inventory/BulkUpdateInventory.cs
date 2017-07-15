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
            FolderData.Add(new FolderDataEntry()
            {
                FolderID = folder.ID,
                ParentID = folder.ParentFolderID,
                Type = folder.InventoryType,
                Name = folder.Name
            });
        }

        public void AddInventoryItem(InventoryItem item, UInt32 callbackID)
        {
            ItemData.Add(new ItemDataEntry()
            {
                ItemID = item.ID,
                CallbackID = callbackID,
                FolderID = item.ParentFolderID,
                CreatorID = item.Creator.ID,
                OwnerID = item.Owner.ID,
                GroupID = item.Group.ID,
                BaseMask = item.Permissions.Base | item.Permissions.Current,
                OwnerMask = item.Permissions.Current,
                GroupMask = item.Permissions.Group,
                EveryoneMask = item.Permissions.EveryOne,
                NextOwnerMask = item.Permissions.NextOwner,
                IsGroupOwned = item.IsGroupOwned,
                AssetID = item.AssetID,
                Type = item.AssetType,
                InvType = item.InventoryType,
                Flags = item.Flags,
                SalePrice = item.SaleInfo.Price,
                SaleType = item.SaleInfo.Type,
                Name = item.Name,
                Description = item.Description,
                CreationDate = (uint)item.CreationDate.DateTimeToUnixTime()
            });
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
            foreach (var item in items)
            {
                AddInventoryItem(item, callbackID);
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(TransactionID);

            p.WriteUInt8((byte)FolderData.Count);
            foreach (var d in FolderData)
            {
                p.WriteUUID(d.FolderID);
                p.WriteUUID(d.ParentID);
                p.WriteInt8((sbyte)d.Type);
                p.WriteStringLen8(d.Name);
            }

            p.WriteUInt8((byte)ItemData.Count);
            foreach (var d in ItemData)
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
            var m = new BulkUpdateInventory()
            {
                AgentID = p.ReadUUID(),
                TransactionID = p.ReadUUID()
            };
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                m.FolderData.Add(new FolderDataEntry()
                {
                    FolderID = p.ReadUUID(),
                    ParentID = p.ReadUUID(),
                    Type = (InventoryType)p.ReadInt8(),
                    Name = p.ReadStringLen8()
                });
            }

            n = p.ReadUInt8();
            while(n-- != 0)
            {
                m.ItemData.Add(new ItemDataEntry()
                {
                    ItemID = p.ReadUUID(),
                    CallbackID = p.ReadUInt32(),
                    FolderID = p.ReadUUID(),
                    CreatorID = p.ReadUUID(),
                    OwnerID = p.ReadUUID(),
                    GroupID = p.ReadUUID(),
                    BaseMask = (InventoryPermissionsMask)p.ReadUInt32(),
                    OwnerMask = (InventoryPermissionsMask)p.ReadUInt32(),
                    GroupMask = (InventoryPermissionsMask)p.ReadUInt32(),
                    EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32(),
                    NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32(),
                    IsGroupOwned = p.ReadBoolean(),
                    AssetID = p.ReadUUID(),
                    Type = (AssetType)p.ReadInt8(),
                    InvType = (InventoryType)p.ReadInt8(),
                    Flags = (InventoryFlags)p.ReadUInt32(),
                    SaleType = (InventoryItem.SaleInfoData.SaleType)p.ReadUInt8(),
                    SalePrice = p.ReadInt32(),
                    Name = p.ReadStringLen8(),
                    Description = p.ReadStringLen8(),
                    CreationDate = p.ReadUInt32()
                });
                p.ReadUInt32(); /* checksum */
            }
            return m;
        }

        private BinaryData EncodeU32ToBinary(uint val)
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
            var agentDataArray = new AnArray
            {
                new MapType
                {
                    ["AgentID"] = AgentID,
                    ["TransactionID"] = TransactionID
                }
            };
            var llsd = new MapType
            {
                ["AgentData"] = agentDataArray
            };
            var folderDataArray = new AnArray();

            foreach(var folder in FolderData)
            {
                folderDataArray.Add(new MapType
                {
                    { "FolderID", folder.FolderID },
                    { "AgentID", AgentID },
                    { "ParentID", folder.ParentID },
                    { "Type", (int)folder.Type },
                    { "Name", folder.Name }
                });
            }
            llsd.Add("FolderData", folderDataArray);

            var itemDataArray = new AnArray();
            foreach(var item in ItemData)
            {
                itemDataArray.Add(new MapType
                {
                    { "ItemID", item.ItemID },
                    { "FolderID", item.FolderID },
                    { "CreatorID", item.CreatorID },
                    { "OwnerID", item.OwnerID },
                    { "GroupID", item.GroupID },
                    { "BaseMask", EncodeU32ToBinary((uint)item.BaseMask) },
                    { "OwnerMask", EncodeU32ToBinary((uint)item.OwnerMask) },
                    { "GroupMask", EncodeU32ToBinary((uint)item.GroupMask) },
                    { "EveryoneMask", EncodeU32ToBinary((uint)item.EveryoneMask) },
                    { "NextOwnerMask", EncodeU32ToBinary((uint)item.NextOwnerMask) },
                    { "GroupOwned", item.IsGroupOwned },
                    { "AssetID", item.AssetID },
                    { "Type", (int)item.Type },
                    { "InvType", (int)item.InvType },
                    { "Flags", EncodeU32ToBinary((uint)item.Flags) },
                    { "SaleType", (int)item.SaleType },
                    { "SalePrice", item.SalePrice },
                    { "Name", item.Name },
                    { "Description", item.Description },
                    { "CreationDate", (int)item.CreationDate },
                    { "CRC", EncodeU32ToBinary(item.Checksum) },
                    { "CallbackID", EncodeU32ToBinary(item.CallbackID) }
                });
            }
            llsd.Add("ItemData", itemDataArray);

            return llsd;
        }
    }
}
