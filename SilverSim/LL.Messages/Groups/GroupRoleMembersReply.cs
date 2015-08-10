// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.GroupRoleMembersReply)]
    [Reliable]
    [Trusted]
    public class GroupRoleMembersReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID RequestID = UUID.Zero;
        public UInt32 TotalPairs = 0;

        public struct MemberDataEntry
        {
            public UUID RoleID;
            public UUID MemberID;

            public int SizeInMessage
            {
                get
                {
                    return 32;
                }
            }
        }

        public List<MemberDataEntry> MemberData = new List<MemberDataEntry>();

        public GroupRoleMembersReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RequestID);
            p.WriteUInt32(TotalPairs);
            p.WriteUInt8((byte)MemberData.Count);
            foreach(MemberDataEntry e in MemberData)
            {
                p.WriteUUID(e.RoleID);
                p.WriteUUID(e.MemberID);
            }
        }
    }
}
