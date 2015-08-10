// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.CreateGroupReply)]
    [Reliable]
    [Trusted]
    public class CreateGroupReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public bool Success = false;
        public string Message = "";

        public CreateGroupReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteBoolean(Success);
            p.WriteStringLen8(Message);
        }
    }
}
