// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupTitleUpdate)]
    [Reliable]
    [NotTrusted]
    public class GroupTitleUpdate : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID TitleRoleID = UUID.Zero;

        public GroupTitleUpdate()
        {

        }

        public static GroupTitleUpdate Decode(UDPPacket p)
        {
            GroupTitleUpdate m = new GroupTitleUpdate();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.TitleRoleID = p.ReadUUID();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteUUID(TitleRoleID);
        }
    }
}
