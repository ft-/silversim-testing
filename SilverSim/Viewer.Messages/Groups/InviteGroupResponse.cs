// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.InviteGroupResponse)]
    [Reliable]
    [Trusted]
    public class InviteGroupResponse : Message
    {
        public UUID AgentID;
        public UUID InviteeID;
        public UUID GroupID;
        public UUID RoleID;
        public int MembershipFee;

        public InviteGroupResponse()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(InviteeID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RoleID);
            p.WriteInt32(MembershipFee);
        }

        public static Message Decode(UDPPacket p)
        {
            InviteGroupResponse m = new InviteGroupResponse();
            m.AgentID = p.ReadUUID();
            m.InviteeID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RoleID = p.ReadUUID();
            m.MembershipFee = p.ReadInt32();
            return m;
        }
    }
}
