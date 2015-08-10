// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Profile
{
    [UDPMessage(MessageType.UserInfoRequest)]
    [Reliable]
    [NotTrusted]
    public class UserInfoRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UserInfoRequest()
        {

        }

        public static UserInfoRequest Decode(UDPPacket p)
        {
            UserInfoRequest m = new UserInfoRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            return m;
        }
    }
}
