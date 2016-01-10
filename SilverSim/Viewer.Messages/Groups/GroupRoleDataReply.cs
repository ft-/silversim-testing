// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

            public int SizeInMessage
            {
                get
                {
                    return 34 + Title.ToUTF8ByteCount() + Description.ToUTF8ByteCount() + Name.ToUTF8ByteCount();
                }
            }
        }
        public List<RoleDataEntry> RoleData = new List<RoleDataEntry>();

        public GroupRoleDataReply()
        {

        }

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
            GroupRoleDataReply m = new GroupRoleDataReply();
            m.AgentID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RequestID = p.ReadUUID();
            m.RoleCount = p.ReadInt32();
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                RoleDataEntry d = new RoleDataEntry();
                d.RoleID = p.ReadUUID();
                d.Name = p.ReadStringLen8();
                d.Title = p.ReadStringLen8();
                d.Description = p.ReadStringLen8();
                d.Powers = (GroupPowers)p.ReadUInt64();
                d.Members = p.ReadUInt32();
                m.RoleData.Add(d);
            }
            return m;
        }
    }
}
