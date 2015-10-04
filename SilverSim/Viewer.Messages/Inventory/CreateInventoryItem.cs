// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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


        public CreateInventoryItem()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            CreateInventoryItem m = new CreateInventoryItem();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            m.CallbackID = p.ReadUInt32();
            m.FolderID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();
            m.NextOwnerMask = (InventoryPermissionsMask)p.ReadUInt32();
            m.AssetType = (AssetType)p.ReadUInt8();
            m.InvType = (InventoryType)p.ReadUInt8();
            m.WearableType = (WearableType)p.ReadUInt8();
            m.Name = p.ReadStringLen8();
            m.Description = p.ReadStringLen8();

            return m;
        }
    }
}
