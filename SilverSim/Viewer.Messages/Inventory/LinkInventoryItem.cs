// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.LinkInventoryItem)]
    [Reliable]
    [NotTrusted]
    public class LinkInventoryItem : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 CallbackID;
        public UUID FolderID;
        public UUID TransactionID;
        public UUID OldItemID;
        public AssetType AssetType;
        public InventoryType InvType;
        public string Name;
        public string Description;

        public LinkInventoryItem()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            LinkInventoryItem m = new LinkInventoryItem();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.CallbackID = p.ReadUInt32();
            m.FolderID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();
            m.OldItemID = p.ReadUUID();
            m.AssetType = (AssetType)p.ReadInt8();
            m.InvType = (InventoryType)p.ReadInt8();
            m.Name = p.ReadStringLen8();
            m.Description = p.ReadStringLen8();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32(CallbackID);
            p.WriteUUID(FolderID);
            p.WriteUUID(TransactionID);
            p.WriteUUID(OldItemID);
            p.WriteInt8((sbyte)AssetType);
            p.WriteInt8((sbyte)InvType);
            p.WriteStringLen8(Name);
            p.WriteStringLen8(Description);
        }
    }
}
