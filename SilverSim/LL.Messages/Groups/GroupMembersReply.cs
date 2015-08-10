// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.GroupMembersReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class GroupMembersReply : Message
    {
        public UUID AgentID = UUID.Zero;

        public UUID GroupID = UUID.Zero;
        public UUID RequestID = UUID.Zero;
        public int MemberCount = 0;

        public struct MemberDataEntry
        {
            public UUID AgentID;
            public int Contribution;
            public string OnlineStatus;
            public GroupPowers AgentPowers;
            public string Title;
            public bool IsOwner;

            public int SizeInMessage
            {
                get
                {
                    return 33 + UTF8NoBOM.GetByteCount(Title) + UTF8NoBOM.GetByteCount(OnlineStatus);
                }
            }
        }

        public List<MemberDataEntry> MemberData = new List<MemberDataEntry>();

        public GroupMembersReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RequestID);
            p.WriteInt32(MemberCount);
            p.WriteUInt8((byte)MemberData.Count);
            foreach(MemberDataEntry e in MemberData)
            {
                p.WriteUUID(e.AgentID);
                p.WriteInt32(e.Contribution);
                p.WriteStringLen8(e.OnlineStatus);
                p.WriteUInt64((UInt64)e.AgentPowers);
                p.WriteStringLen8(e.Title);
                p.WriteBoolean(e.IsOwner);
            }
        }
    }
}
