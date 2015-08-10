// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Generic
{
    [UDPMessage(MessageType.GenericMessage)]
    [Reliable]
    [NotTrusted]
    public class GenericMessage : GenericMessageFormat
    {
        public GenericMessage()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            return Decode(p, new GenericMessage());
        }
    }
}
