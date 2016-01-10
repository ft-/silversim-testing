// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Viewer.Messages.Groups
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
        public int MemberCount;

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
                    return 33 + Title.ToUTF8ByteCount() + OnlineStatus.ToUTF8ByteCount();
                }
            }
        }

        public List<MemberDataEntry> MemberData = new List<MemberDataEntry>();

        public GroupMembersReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
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

        public static Message Decode(UDPPacket p)
        {
            GroupMembersReply m = new GroupMembersReply();
            m.AgentID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RequestID = p.ReadUUID();
            m.MemberCount = p.ReadInt32();
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                MemberDataEntry d = new MemberDataEntry();
                d.AgentID = p.ReadUUID();
                d.Contribution = p.ReadInt32();
                d.OnlineStatus = p.ReadStringLen8();
                d.AgentPowers = (GroupPowers)p.ReadUInt64();
                d.Title = p.ReadStringLen8();
                d.IsOwner = p.ReadBoolean();
                m.MemberData.Add(d);
            }
            return m;
        }
    }
}
