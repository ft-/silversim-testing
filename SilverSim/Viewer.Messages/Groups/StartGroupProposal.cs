// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.StartGroupProposal)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    [UDPDeprecated]
    public class StartGroupProposal : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID GroupID;
        public int Quorum;
        public double Majority;
        public int Duration;
        public string ProposalText;

        public StartGroupProposal()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            StartGroupProposal m = new StartGroupProposal();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.Quorum = p.ReadInt32();
            m.Majority = p.ReadFloat();
            m.Duration = p.ReadInt32();
            m.ProposalText = p.ReadStringLen8();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteInt32(Quorum);
            p.WriteFloat((float)Majority);
            p.WriteInt32(Duration);
            p.WriteStringLen8(ProposalText);
        }
    }
}
