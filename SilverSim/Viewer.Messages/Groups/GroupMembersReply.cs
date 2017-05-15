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
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

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
            var m = new GroupMembersReply()
            {
                AgentID = p.ReadUUID(),
                GroupID = p.ReadUUID(),
                RequestID = p.ReadUUID(),
                MemberCount = p.ReadInt32()
            };
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                m.MemberData.Add(new MemberDataEntry()
                {
                    AgentID = p.ReadUUID(),
                    Contribution = p.ReadInt32(),
                    OnlineStatus = p.ReadStringLen8(),
                    AgentPowers = (GroupPowers)p.ReadUInt64(),
                    Title = p.ReadStringLen8(),
                    IsOwner = p.ReadBoolean()
                });
            }
            return m;
        }
    }
}
