// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Generic
{
    [UDPMessage(MessageType.EstateOwnerMessage)]
    [Reliable]
    [NotTrusted]
    public class EstateOwnerMessage : GenericMessageFormat
    {
        public EstateOwnerMessage()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            return Decode(p, new EstateOwnerMessage());
        }
    }
}
