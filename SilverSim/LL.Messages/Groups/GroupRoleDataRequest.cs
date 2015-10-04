// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupRoleDataRequest)]
    [Reliable]
    [NotTrusted]
    public class GroupRoleDataRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public UUID GroupID = UUID.Zero;
        public UUID RequestID = UUID.Zero;

        public GroupRoleDataRequest()
        {

        }

        public static GroupRoleDataRequest Decode(UDPPacket p)
        {
            GroupRoleDataRequest m = new GroupRoleDataRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RequestID = p.ReadUUID();

            return m;
        }
    }
}
