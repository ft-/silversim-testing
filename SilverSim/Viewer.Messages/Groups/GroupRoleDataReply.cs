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
    [UDPMessage(MessageType.GroupRoleDataReply)]
    [Reliable]
    [Trusted]
    public class GroupRoleDataReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID RequestID = UUID.Zero;
        public int RoleCount;

        public struct RoleDataEntry
        {
            public UUID RoleID;
            public string Name;
            public string Title;
            public string Description;
            public GroupPowers Powers;
            public UInt32 Members;

            public int SizeInMessage => 34 + Title.ToUTF8ByteCount() + Description.ToUTF8ByteCount() + Name.ToUTF8ByteCount();
        }

        public List<RoleDataEntry> RoleData = new List<RoleDataEntry>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RequestID);
            p.WriteInt32(RoleCount);
            p.WriteUInt8((byte)RoleData.Count);
            foreach(RoleDataEntry e in RoleData)
            {
                p.WriteUUID(e.RoleID);
                p.WriteStringLen8(e.Name);
                p.WriteStringLen8(e.Title);
                p.WriteStringLen8(e.Description);
                p.WriteUInt64((UInt64)e.Powers);
                p.WriteUInt32(e.Members);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            var m = new GroupRoleDataReply()
            {
                AgentID = p.ReadUUID(),
                GroupID = p.ReadUUID(),
                RequestID = p.ReadUUID(),
                RoleCount = p.ReadInt32()
            };
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                m.RoleData.Add(new RoleDataEntry()
                {
                    RoleID = p.ReadUUID(),
                    Name = p.ReadStringLen8(),
                    Title = p.ReadStringLen8(),
                    Description = p.ReadStringLen8(),
                    Powers = (GroupPowers)p.ReadUInt64(),
                    Members = p.ReadUInt32()
                });
            }
            return m;
        }
    }
}
