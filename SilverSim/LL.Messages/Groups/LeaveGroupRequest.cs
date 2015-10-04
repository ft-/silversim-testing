// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.LeaveGroupRequest)]
    [Reliable]
    [NotTrusted]
    public class LeaveGroupRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;

        public LeaveGroupRequest()
        {

        }

        public static LeaveGroupRequest Decode(UDPPacket p)
        {
            LeaveGroupRequest m = new LeaveGroupRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            return m;
        }
    }
}
