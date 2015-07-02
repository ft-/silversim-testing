/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Inventory
{
    [UDPMessage(MessageType.InventoryDescendents)]
    [Reliable]
    [Zerocoded]
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
            public UInt32 Flags;
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
            p.WriteMessageType(Number);
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
                p.WriteUInt32(d.Flags);
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

                checksum += d.Flags; // Flags
                checksum += (uint)d.InvType; // InvType
                checksum += (uint)d.Type; // Type 
                checksum += (uint)d.CreationDate; // CreationDate
                checksum += (uint)d.SalePrice;    // SalePrice
                checksum += (uint)((uint)d.SaleType * 0x07073096); // SaleType

                p.WriteUInt32(checksum);
            }
        }
    }
}
