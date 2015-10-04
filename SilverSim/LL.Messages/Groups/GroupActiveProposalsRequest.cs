// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupActiveProposalsRequest)]
    [Reliable]
    [NotTrusted]
    public class GroupActiveProposalsRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID TransactionID = UUID.Zero;

        public GroupActiveProposalsRequest()
        {

        }

        public static GroupActiveProposalsRequest Decode(UDPPacket p)
        {
            GroupActiveProposalsRequest m = new GroupActiveProposalsRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();

            return m;
        }
    }
}
