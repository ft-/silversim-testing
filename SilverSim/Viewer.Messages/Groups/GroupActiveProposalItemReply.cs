﻿// SilverSim is distributed under the terms of the
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
    [UDPMessage(MessageType.GroupActiveProposalItemReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class GroupActiveProposalItemReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;

        public UUID TransactionID = UUID.Zero;
        public UInt32 TotalNumItems;

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

        public override void Serialize(UDPPacket p)
        {
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

        public static Message Decode(UDPPacket p)
        {
            var m = new GroupActiveProposalItemReply
            {
                AgentID = p.ReadUUID(),
                GroupID = p.ReadUUID(),
                TransactionID = p.ReadUUID(),
                TotalNumItems = p.ReadUInt32()
            };
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                m.ProposalData.Add(new ProposalDataEntry
                {
                    VoteID = p.ReadUUID(),
                    VoteInitiatorID = p.ReadUUID(),
                    TerseDateID = p.ReadStringLen8(),
                    StartDateTime = p.ReadStringLen8(),
                    EndDateTime = p.ReadStringLen8(),
                    AlreadyVoted = p.ReadBoolean(),
                    VoteCast = p.ReadStringLen8(),
                    Majority = p.ReadFloat(),
                    Quorum = p.ReadInt32(),
                    ProposalText = p.ReadStringLen8()
                });
            }
            return m;
        }
    }
}
