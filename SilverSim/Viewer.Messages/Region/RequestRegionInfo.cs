// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Region
{
    [UDPMessage(MessageType.RequestRegionInfo)]
    [Reliable]
    [NotTrusted]
    public class RequestRegionInfo : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public RequestRegionInfo()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RequestRegionInfo m = new RequestRegionInfo();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            return m;
        }
    }
}
