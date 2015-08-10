// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.GroupNoticeRequest)]
    [Reliable]
    [NotTrusted]
    public class GroupNoticeRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID GroupNoticeID;

        public GroupNoticeRequest()
        {
        }

        public static GroupNoticeRequest Decode(UDPPacket p)
        {
            GroupNoticeRequest m = new GroupNoticeRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupNoticeID = p.ReadUUID();
            return m;
        }
    }
}
