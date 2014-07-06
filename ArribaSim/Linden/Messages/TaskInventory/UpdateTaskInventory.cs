﻿/*

ArribaSim is distributed under the terms of the
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

using ArribaSim.Types;
using ArribaSim.Types.Asset;
using ArribaSim.Types.Inventory;
using System;

namespace ArribaSim.Linden.Messages.TaskInventory
{
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
        public UInt32 BaseMask;
        public UInt32 OwnerMask;
        public UInt32 GroupMask;
        public UInt32 EveryoneMask;
        public UInt32 NextOwnerMask;
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

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.UpdateTaskInventory;
            }
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
            m.BaseMask = p.ReadUInt32();
            m.OwnerMask = p.ReadUInt32();
            m.GroupMask = p.ReadUInt32();
            m.EveryoneMask = p.ReadUInt32();
            m.NextOwnerMask = p.ReadUInt32();
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
