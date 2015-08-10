// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.MuteList
{
    [UDPMessage(MessageType.MuteListRequest)]
    [Reliable]
    [NotTrusted]
    public class MuteListRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public MuteListRequest()
        {

        }

        public static MuteListRequest Decode(UDPPacket p)
        {
            MuteListRequest m = new MuteListRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            return m;
        }
    }
}
