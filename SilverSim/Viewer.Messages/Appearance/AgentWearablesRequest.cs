// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Appearance
{
    [UDPMessage(MessageType.AgentWearablesRequest)]
    [Reliable]
    [NotTrusted]
    public class AgentWearablesRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public AgentWearablesRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            AgentWearablesRequest m = new AgentWearablesRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            return m;
        }
    }
}
