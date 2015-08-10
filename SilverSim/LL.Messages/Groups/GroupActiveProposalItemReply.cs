// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.GroupActiveProposalItemReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class GroupActiveProposalItemReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;

        public UUID TransactionID = UUID.Zero;
        public UInt32 TotalNumItems = 0;

        public struct ProposalDataEntry
        {
            public UUID VoteID;
            public UUID VoteInitiatorID;
            public string TerseDateID;
            public string StartDateTime;
            public string EndDateTime;
            public bool AlreadyVoted;
            public string VoteCast;
            public float Majority;
            public int Quorum;
            public string ProposalText;
        }

        public List<ProposalDataEntry> ProposalData = new List<ProposalDataEntry>();

        public GroupActiveProposalItemReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteUUID(TransactionID);
            p.WriteUInt32(TotalNumItems);
            p.WriteUInt8((byte)ProposalData.Count);
            foreach(ProposalDataEntry e in ProposalData)
            {
                p.WriteUUID(e.VoteID);
                p.WriteUUID(e.VoteInitiatorID);
                p.WriteStringLen8(e.TerseDateID);
                p.WriteStringLen8(e.StartDateTime);
                p.WriteStringLen8(e.EndDateTime);
                p.WriteBoolean(e.AlreadyVoted);
                p.WriteStringLen8(e.VoteCast);
                p.WriteFloat(e.Majority);
                p.WriteInt32(e.Quorum);
                p.WriteStringLen8(e.ProposalText);
            }
        }
    }
}
