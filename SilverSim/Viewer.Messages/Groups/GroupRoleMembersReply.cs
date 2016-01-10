// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupRoleMembersReply)]
    [Reliable]
    [Trusted]
    public class GroupRoleMembersReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID RequestID = UUID.Zero;
        public UInt32 TotalPairs;

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

        public static Message Decode(UDPPacket p)
        {
            GroupRoleMembersReply m = new GroupRoleMembersReply();
            m.AgentID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RequestID = p.ReadUUID();
            m.TotalPairs = p.ReadUInt32();
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                MemberDataEntry d = new MemberDataEntry();
                d.RoleID = p.ReadUUID();
                d.MemberID = p.ReadUUID();
                m.MemberData.Add(d);
            }
            return m;
        }
    }
}
