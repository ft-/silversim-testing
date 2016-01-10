// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.LeaveGroupReply)]
    [Reliable]
    [Trusted]
    public class LeaveGroupReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public bool Success;

        public LeaveGroupReply()
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
            LeaveGroupReply m = new LeaveGroupReply();
            m.AgentID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.Success = p.ReadBoolean();
            return m;
        }
    }
}
