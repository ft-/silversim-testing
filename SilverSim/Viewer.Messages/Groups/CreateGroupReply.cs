// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.CreateGroupReply)]
    [Reliable]
    [Trusted]
    public class CreateGroupReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public bool Success;
        public string Message = string.Empty;

        public CreateGroupReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteBoolean(Success);
            p.WriteStringLen8(Message);
        }

        public static Message Decode(UDPPacket p)
        {
            CreateGroupReply m = new CreateGroupReply();
            m.AgentID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.Success = p.ReadBoolean();
            m.Message = p.ReadStringLen8();
            return m;
        }
    }
}
