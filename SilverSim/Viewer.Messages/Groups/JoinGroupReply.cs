// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.JoinGroupReply)]
    [Reliable]
    [Trusted]
    public class JoinGroupReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public bool Success;

        public JoinGroupReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteBoolean(Success);
        }

        public static Message Decode(UDPPacket p)
        {
            JoinGroupReply m = new JoinGroupReply();
            m.AgentID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.Success = p.ReadBoolean();
            return m;
        }
    }
}
