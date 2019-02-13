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

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.RezObject)]
    [Reliable]
    [NotTrusted]
    public class RezObject : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID GroupID;

        public struct RezDataS
        {
            public UUID FromTaskID;
            public bool BypassRaycast;
            public Vector3 RayStart;
            public Vector3 RayEnd;
            public UUID RayTargetID;
            public bool RayEndIsIntersection;
            public bool RezSelected;
            public bool RemoveItem;
            public UInt32 ItemFlags;
            public InventoryPermissionsMask GroupMask;
            public InventoryPermissionsMask EveryoneMask;
            public InventoryPermissionsMask NextOwnerMask;
        }

        public RezDataS RezData;

        public struct InventoryDataS
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
            public UInt32 Flags;
            public InventoryItem.SaleInfoData.SaleType SaleType;
            public Int32 SalePrice;
            public string Name;
            public string Description;
            public UInt32 CreationDate;
            public UInt32 CRC;
        }

        public InventoryDataS InventoryData;

        public static Message Decode(UDPPacket p)
        {
            var m = new RezObject
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),
                GroupID = p.ReadUUID()
            };
            m.RezData.FromTaskID = p.ReadUUID();
            m.RezData.BypassRaycast = p.ReadBoolean();
            m.RezData.RayStart = p.ReadVector3f();
            m.RezData.RayEnd = p.ReadVector3f();
            m.RezData.RayTargetID = p.ReadUUID();
            m.RezData.RayEndIsIntersection = p.ReadBoolean();
            m.RezData.RezSelected = p.ReadBoolean();
            m.RezData.RemoveItem = p.ReadBoolean();
            m.RezData.ItemFlags = p.ReadUInt32();
            m.RezData.GroupMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.RezData.EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.RezData.NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.InventoryData.ItemID = p.ReadUUID();
            m.InventoryData.FolderID = p.ReadUUID();
            m.InventoryData.CreatorID = p.ReadUUID();
            m.InventoryData.OwnerID = p.ReadUUID();
            m.InventoryData.GroupID = p.ReadUUID();
            m.InventoryData.BaseMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.InventoryData.OwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.InventoryData.GroupMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.InventoryData.EveryoneMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.InventoryData.NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.InventoryData.IsGroupOwned = p.ReadBoolean();
            m.InventoryData.TransactionID = p.ReadUUID();
            m.InventoryData.AssetType = (AssetType)p.ReadInt8();
            m.InventoryData.InvType = (InventoryType)p.ReadInt8();
            m.InventoryData.Flags = p.ReadUInt32();
            m.InventoryData.SaleType = (InventoryItem.SaleInfoData.SaleType)p.ReadUInt8();
            m.InventoryData.SalePrice = p.ReadInt32();
            m.InventoryData.Name = p.ReadStringLen8();
            m.InventoryData.Description = p.ReadStringLen8();
            m.InventoryData.CreationDate = p.ReadUInt32();
            m.InventoryData.CRC = p.ReadUInt32();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RezData.FromTaskID);
            p.WriteBoolean(RezData.BypassRaycast);
            p.WriteVector3f(RezData.RayStart);
            p.WriteVector3f(RezData.RayEnd);
            p.WriteUUID(RezData.RayTargetID);
            p.WriteBoolean(RezData.RayEndIsIntersection);
            p.WriteBoolean(RezData.RezSelected);
            p.WriteBoolean(RezData.RemoveItem);
            p.WriteUInt32(RezData.ItemFlags);
            p.WriteUInt32((uint)RezData.GroupMask);
            p.WriteUInt32((uint)RezData.EveryoneMask);
            p.WriteUInt32((uint)RezData.NextOwnerMask);
            p.WriteUUID(InventoryData.ItemID);
            p.WriteUUID(InventoryData.FolderID);
            p.WriteUUID(InventoryData.CreatorID);
            p.WriteUUID(InventoryData.OwnerID);
            p.WriteUUID(InventoryData.GroupID);
            p.WriteUInt32((uint)InventoryData.BaseMask);
            p.WriteUInt32((uint)InventoryData.OwnerMask);
            p.WriteUInt32((uint)InventoryData.GroupMask);
            p.WriteUInt32((uint)InventoryData.EveryoneMask);
            p.WriteUInt32((uint)InventoryData.NextOwnerMask);
            p.WriteBoolean(InventoryData.IsGroupOwned);
            p.WriteUUID(InventoryData.TransactionID);
            p.WriteInt8((sbyte)InventoryData.AssetType);
            p.WriteInt8((sbyte)InventoryData.InvType);
            p.WriteUInt32(InventoryData.Flags);
            p.WriteUInt8((byte)InventoryData.SaleType);
            p.WriteInt32(InventoryData.SalePrice);
            p.WriteStringLen8(InventoryData.Name);
            p.WriteStringLen8(InventoryData.Description);
            p.WriteUInt32(InventoryData.CreationDate);
            p.WriteUInt32(InventoryData.CRC);
        }
    }
}
