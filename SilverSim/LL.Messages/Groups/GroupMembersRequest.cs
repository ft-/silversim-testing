﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.GroupMembersRequest)]
    [Reliable]
    [NotTrusted]
    public class GroupMembersRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID RequestID = UUID.Zero;

        public GroupMembersRequest()
        {

        }

        public static GroupMembersRequest Decode(UDPPacket p)
        {
            GroupMembersRequest m = new GroupMembersRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RequestID = p.ReadUUID();
            return m;
        }
    }
}
