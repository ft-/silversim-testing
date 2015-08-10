// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Agent
{
    [UDPMessage(MessageType.AgentDropGroup)]
    [Reliable]
    [Trusted]
    public class AgentDropGroup : Message
    {
        public UUID AgentID;
        public UUID GroupID;

        public AgentDropGroup()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
        }
    }
}
