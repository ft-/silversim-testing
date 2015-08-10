// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Teleport
{
    [UDPMessage(MessageType.TeleportLandmarkRequest)]
    [Reliable]
    [NotTrusted]
    public class TeleportLandmarkRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID LandmarkID;

        public TeleportLandmarkRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            TeleportLandmarkRequest m = new TeleportLandmarkRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LandmarkID = p.ReadUUID();

            return m;
        }
    }
}
