// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupTitlesRequest)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class GroupTitlesRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID RequestID = UUID.Zero;

        public GroupTitlesRequest()
        {

        }

        public static GroupTitlesRequest Decode(UDPPacket p)
        {
            GroupTitlesRequest m = new GroupTitlesRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RequestID = p.ReadUUID();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RequestID);
        }
    }
}
