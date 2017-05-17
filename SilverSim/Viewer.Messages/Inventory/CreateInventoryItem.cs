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
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.CreateInventoryItem)]
    [Reliable]
    [NotTrusted]
    public class CreateInventoryItem : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UInt32 CallbackID;
        public UUID FolderID;
        public UUID TransactionID;
        public InventoryPermissionsMask NextOwnerMask;
        public AssetType AssetType;
        public InventoryType InvType;
        public WearableType WearableType;
        public string Name;
        public string Description;

        public static Message Decode(UDPPacket p)
        {
            return new CreateInventoryItem()
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),

                CallbackID = p.ReadUInt32(),
                FolderID = p.ReadUUID(),
                TransactionID = p.ReadUUID(),
                NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32(),
                AssetType = (AssetType)p.ReadInt8(),
                InvType = (InventoryType)p.ReadInt8(),
                WearableType = (WearableType)p.ReadUInt8(),
                Name = p.ReadStringLen8(),
                Description = p.ReadStringLen8()
            };
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32(CallbackID);
            p.WriteUUID(FolderID);
            p.WriteUUID(TransactionID);
            p.WriteUInt32((uint)NextOwnerMask);
            p.WriteInt8((sbyte)AssetType);
            p.WriteInt8((sbyte)InvType);
            p.WriteUInt8((byte)WearableType);
            p.WriteStringLen8(Name);
            p.WriteStringLen8(Description);
        }
    }
}
