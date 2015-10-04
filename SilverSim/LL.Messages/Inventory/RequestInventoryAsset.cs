// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Inventory
{
    [UDPMessage(MessageType.RequestInventoryAsset)]
    [Reliable]
    [NotTrusted]
    public class RequestInventoryAsset : Message
    {
        public UUID QueryID;
        public UUID AgentID;
        public UUID OwnerID;
        public UUID ItemID;

        public RequestInventoryAsset()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RequestInventoryAsset m = new RequestInventoryAsset();
            m.QueryID = p.ReadUUID();
            m.AgentID = p.ReadUUID();
            m.OwnerID = p.ReadUUID();
            m.ItemID = p.ReadUUID();

            return m;
        }
    }
}
