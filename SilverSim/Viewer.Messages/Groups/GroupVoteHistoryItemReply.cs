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

        public static Message Decode(UDPPacket p)
        {
            GroupVoteHistoryItemReply m = new GroupVoteHistoryItemReply();
            m.AgentID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();
            m.TotalNumItems = p.ReadUInt32();
            m.VoteID = p.ReadUUID();
            m.TerseDataID = p.ReadStringLen8();
            m.StartDateTime = p.ReadStringLen8();
            m.EndDateTime = p.ReadStringLen8();
            m.VoteInitiatorID = p.ReadUUID();
            m.VoteType = p.ReadStringLen8();
            m.VoteResult = p.ReadStringLen8();
            m.Majority = p.ReadFloat();
            m.Quorum = p.ReadInt32();
            m.ProposalText = p.ReadStringLen16();

            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                VoteItemData d = new VoteItemData();
                d.CandidateID = p.ReadUUID();
                d.VoteCast = p.ReadStringLen8();
                d.NumVotes = p.ReadInt32();
                m.VoteItem.Add(d);
            }
            return m;
        }
    }
}
