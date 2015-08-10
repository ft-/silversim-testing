// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.InviteGroupRequest)]
    [Reliable]
    [NotTrusted]
    public class InviteGroupRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public UUID GroupID = UUID.Zero;
        public UUID InviteeID = UUID.Zero;
        public UUID RoleID = UUID.Zero;

        public InviteGroupRequest()
        {

        }

        public static InviteGroupRequest Decode(UDPPacket p)
        {
            InviteGroupRequest m = new InviteGroupRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.InviteeID = p.ReadUUID();
            m.RoleID = p.ReadUUID();
            return m;
        }
    }
}
