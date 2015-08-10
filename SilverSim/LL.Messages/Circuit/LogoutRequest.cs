// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Circuit
{
    [UDPMessage(MessageType.LogoutRequest)]
    [Reliable]
    [NotTrusted]
    public class LogoutRequest : Message
    {
        public UUID SessionID = UUID.Zero;
        public UUID AgentID = UUID.Zero;

        public LogoutRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            LogoutRequest m = new LogoutRequest();
            m.SessionID = p.ReadUUID();
            m.AgentID = p.ReadUUID();
            return m;
        }
    }
}
