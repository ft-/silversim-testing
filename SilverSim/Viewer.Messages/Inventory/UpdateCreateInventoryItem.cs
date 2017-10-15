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

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.UpdateCreateInventoryItem)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class UpdateCreateInventoryItem : Message
    {
        public UUID AgentID;
        public bool SimApproved;
        public UUID TransactionID;

        public struct ItemDataEntry
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

        public UpdateCreateInventoryItem()
        {
        }

        public void AddItem(InventoryItem item, UInt32 callbackID)
        {
            ItemDataEntry id;
            id.ItemID = item.ID;
            id.FolderID = item.ParentFolderID;
            id.CallbackID = callbackID;
            id.CreatorID = item.Creator.ID;
            id.OwnerID = item.Owner.ID;
            id.GroupID = item.Group.ID;
            id.BaseMask = item.Permissions.Current;
            id.OwnerMask = item.Permissions.Current;
            id.GroupMask = item.Permissions.Group;
            id.EveryoneMask = item.Permissions.EveryOne;
            id.NextOwnerMask = item.Permissions.NextOwner;
            id.IsGroupOwned = item.IsGroupOwned;
            id.AssetID = item.AssetID;
            id.Type = item.AssetType;
            id.InvType = item.InventoryType;
            id.Flags = item.Flags;
            id.SaleType = item.SaleInfo.Type;
            id.SalePrice = item.SaleInfo.Price;
            id.Name = item.Name;
            id.Description = item.Description;
            id.CreationDate = (uint)item.CreationDate.DateTimeToUnixTime();
            ItemData.Add(id);
        }

        public UpdateCreateInventoryItem(UUID agentID, bool simApproved, UUID transactionID, InventoryItem item, UInt32 callbackID)
        {
            AgentID = agentID;
            SimApproved = simApproved;
            TransactionID = transactionID;
            AddItem(item, callbackID);
        }

        public UpdateCreateInventoryItem(UUID agentID, bool simApproved, UUID transactionID, List<InventoryItem> items, UInt32 callbackID)
        {
            AgentID = agentID;
            SimApproved = simApproved;
            TransactionID = transactionID;

            foreach (var item in items)
            {
                AddItem(item, callbackID);
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteBoolean(SimApproved);
            p.WriteUUID(TransactionID);

            p.WriteUInt8((byte)ItemData.Count);
            foreach (var d in ItemData)
            {
                p.WriteUUID(d.ItemID);
                p.WriteUUID(d.FolderID);
                p.WriteUInt32(d.CallbackID);
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
            var m = new UpdateCreateInventoryItem
            {
                AgentID = p.ReadUUID(),
                SimApproved = p.ReadBoolean(),
                TransactionID = p.ReadUUID()
            };
            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ItemData.Add(new ItemDataEntry
                {
                    ItemID = p.ReadUUID(),
                    FolderID = p.ReadUUID(),
                    CallbackID = p.ReadUInt32(),
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
    }
}
