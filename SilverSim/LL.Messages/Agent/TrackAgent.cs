// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Agent
{
    [UDPMessage(MessageType.TrackAgent)]
    [NotTrusted]
    public class TrackAgent : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public UUID PreyID = UUID.Zero;

        public TrackAgent()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            TrackAgent m = new TrackAgent();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.PreyID = p.ReadUUID();
            return m;
        }
    }
}
