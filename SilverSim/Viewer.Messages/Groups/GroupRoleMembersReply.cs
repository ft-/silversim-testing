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

            public int SizeInMessage => 32;
        }

        public List<MemberDataEntry> MemberData = new List<MemberDataEntry>();

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
            var m = new GroupRoleMembersReply()
            {
                AgentID = p.ReadUUID(),
                GroupID = p.ReadUUID(),
                RequestID = p.ReadUUID(),
                TotalPairs = p.ReadUInt32()
            };
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                m.MemberData.Add(new MemberDataEntry()
                {
                    RoleID = p.ReadUUID(),
                    MemberID = p.ReadUUID()
                });
            }
            return m;
        }
    }
}
