﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.GroupRoleMembersRequest)]
    [Reliable]
    [NotTrusted]
    public class GroupRoleMembersRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID RequestID = UUID.Zero;

        public GroupRoleMembersRequest()
        {

        }

        public static GroupRoleMembersRequest Decode(UDPPacket p)
        {
            GroupRoleMembersRequest m = new GroupRoleMembersRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RequestID = p.ReadUUID();

            return m;
        }
    }
}
