// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Estate
{
    [UDPMessage(MessageType.EstateCovenantRequest)]
    [Reliable]
    [NotTrusted]
    public class EstateCovenantRequest : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public EstateCovenantRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            EstateCovenantRequest m = new EstateCovenantRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            return m;
        }
    }
}
