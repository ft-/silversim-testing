// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

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
            var m = new GroupVoteHistoryItemReply()
            {
                AgentID = p.ReadUUID(),
                GroupID = p.ReadUUID(),
                TransactionID = p.ReadUUID(),
                TotalNumItems = p.ReadUInt32(),
                VoteID = p.ReadUUID(),
                TerseDataID = p.ReadStringLen8(),
                StartDateTime = p.ReadStringLen8(),
                EndDateTime = p.ReadStringLen8(),
                VoteInitiatorID = p.ReadUUID(),
                VoteType = p.ReadStringLen8(),
                VoteResult = p.ReadStringLen8(),
                Majority = p.ReadFloat(),
                Quorum = p.ReadInt32(),
                ProposalText = p.ReadStringLen16()
            };
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                m.VoteItem.Add(new VoteItemData()
                {
                    CandidateID = p.ReadUUID(),
                    VoteCast = p.ReadStringLen8(),
                    NumVotes = p.ReadInt32()
                });
            }
            return m;
        }
    }
}
