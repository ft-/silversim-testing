﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupProfileRequest)]
    [Reliable]
    [NotTrusted]
    public class GroupProfileRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;

        public GroupProfileRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            GroupProfileRequest m = new GroupProfileRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
        }
    }
}
