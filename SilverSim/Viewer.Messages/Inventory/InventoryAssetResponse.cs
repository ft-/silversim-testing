// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.InventoryAssetResponse)]
    [Reliable]
    [Trusted]
    public class InventoryAssetResponse : Message
    {
        public UUID QueryID;
        public UUID AssetID;
        public bool IsReadable;

        public InventoryAssetResponse()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(QueryID);
            p.WriteUUID(AssetID);
            p.WriteBoolean(IsReadable);
        }

        public static Message Decode(UDPPacket p)
        {
            InventoryAssetResponse m = new InventoryAssetResponse();
            m.QueryID = p.ReadUUID();
            m.AssetID = p.ReadUUID();
            m.IsReadable = p.ReadBoolean();
            return m;
        }
    }
}
