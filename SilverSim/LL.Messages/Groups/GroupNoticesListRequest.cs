// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupNoticesListRequest)]
    [Reliable]
    [NotTrusted]
    public class GroupNoticesListRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID GroupID;

        public GroupNoticesListRequest()
        {

        }

        public static GroupNoticesListRequest Decode(UDPPacket p)
        {
            GroupNoticesListRequest m = new GroupNoticesListRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            return m;
        }
    }
}
