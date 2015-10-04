// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.PickDelete)]
    [Reliable]
    [NotTrusted]
    public class PickDelete : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID PickID;

        public PickDelete()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            PickDelete m = new PickDelete();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.PickID = p.ReadUUID();

            return m;
        }
    }
}
