// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.SetGroupAcceptNotices)]
    [Reliable]
    [NotTrusted]
    public class SetGroupAcceptNotices : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public UUID GroupID = UUID.Zero;
        public bool AcceptNotices = false;
        public bool ListInProfile = false;

        public SetGroupAcceptNotices()
        {

        }

        public static SetGroupAcceptNotices Decode(UDPPacket p)
        {
            SetGroupAcceptNotices m = new SetGroupAcceptNotices();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.AcceptNotices = p.ReadBoolean();
            m.ListInProfile = p.ReadBoolean();

            return m;
        }
    }
}
