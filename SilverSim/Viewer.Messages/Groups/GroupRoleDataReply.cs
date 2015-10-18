﻿// SilverSim is distributed under the terms of the
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
                    return 34 + UTF8NoBOM.GetByteCount(Title) + UTF8NoBOM.GetByteCount(Description) + UTF8NoBOM.GetByteCount(Name);
                }
            }
        }
        public List<RoleDataEntry> RoleData = new List<RoleDataEntry>();

        public GroupRoleDataReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
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
    }
}
