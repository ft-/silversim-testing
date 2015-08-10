// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.GroupAccountSummaryRequest)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class GroupAccountSummaryRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;

        public UUID RequestID = UUID.Zero;
        public int IntervalDays = 0;
        public int CurrentInterval = 0;

        public GroupAccountSummaryRequest()
        {

        }

        public static GroupAccountSummaryRequest Decode(UDPPacket p)
        {
            GroupAccountSummaryRequest m = new GroupAccountSummaryRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RequestID = p.ReadUUID();
            m.IntervalDays = p.ReadInt32();
            m.CurrentInterval = p.ReadInt32();
            return m;
        }
    }
}
