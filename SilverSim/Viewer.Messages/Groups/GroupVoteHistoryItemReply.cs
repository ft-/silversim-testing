// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupVoteHistoryItemReply)]
    [Reliable]
    [Trusted]
    public class GroupVoteHistoryItemReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;

        public UUID TransactionID = UUID.Zero;
        public UInt32 TotalNumItems;

        public UUID VoteID = UUID.Zero;
        public string TerseDataID = string.Empty;
        public string StartDateTime = string.Empty;
        public string EndDateTime = string.Empty;
        public UUID VoteInitiatorID = UUID.Zero;
        public string VoteType = string.Empty;
        public string VoteResult = string.Empty;
        public float Majority;
        public int Quorum;
        public string ProposalText = string.Empty;

        public struct VoteItemData
        {
            public UUID CandidateID;
            public string VoteCast;
            public int NumVotes;
        }

        public List<VoteItemData> VoteItem = new List<VoteItemData>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteUUID(TransactionID);
            p.WriteUInt32(TotalNumItems);
            p.WriteUUID(VoteID);
            p.WriteStringLen8(TerseDataID);
            p.WriteStringLen8(StartDateTime);
            p.WriteStringLen8(EndDateTime);
            p.WriteUUID(VoteInitiatorID);
            p.WriteStringLen8(VoteType);
            p.WriteStringLen8(VoteResult);
            p.WriteFloat(Majority);
            p.WriteInt32(Quorum);
            p.WriteStringLen16(ProposalText);

            p.WriteUInt8((byte)VoteItem.Count);
            foreach(VoteItemData e in VoteItem)
            {
                p.WriteUUID(e.CandidateID);
                p.WriteStringLen8(e.VoteCast);
                p.WriteInt32(e.NumVotes);
            }
        }
    }
}
