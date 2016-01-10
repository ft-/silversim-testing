// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupProposalBallot)]
    [Reliable]
    [NotTrusted]
    [UDPDeprecated]
    public class GroupProposalBallot : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID ProposalID;
        public UUID GroupID;
        public string VoteCast;

        public GroupProposalBallot()
        {

        }

        public static GroupProposalBallot Decode(UDPPacket p)
        {
            GroupProposalBallot m = new GroupProposalBallot();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ProposalID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.VoteCast = p.ReadStringLen8();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(ProposalID);
            p.WriteUUID(GroupID);
            p.WriteStringLen8(VoteCast);
        }
    }
}
