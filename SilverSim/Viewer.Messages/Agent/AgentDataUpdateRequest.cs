// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.AgentDataUpdateRequest)]
    [Reliable]
    [NotTrusted]
    public class AgentDataUpdateRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public AgentDataUpdateRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AgentDataUpdateRequest m = new AgentDataUpdateRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            return m;
        }
    }
}
