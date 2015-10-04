// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.ActivateGroup)]
    [Reliable]
    [NotTrusted]
    public class ActivateGroup : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;

        public ActivateGroup()
        {

        }

        public static ActivateGroup Decode(UDPPacket p)
        {
            ActivateGroup m = new ActivateGroup();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GroupID = p.ReadUUID();

            return m;
        }
    }
}
