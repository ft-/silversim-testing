// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.AgentSit)]
    [Reliable]
    [NotTrusted]
    public class AgentSit : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public static Message Decode(UDPPacket p)
        {
            AgentSit m = new AgentSit();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            return m;
        }
    }
}
