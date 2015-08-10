// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Generic
{
    [UDPMessage(MessageType.GodlikeMessage)]
    [Reliable]
    [NotTrusted]
    public class GodlikeMessage : GenericMessageFormat
    {
        public GodlikeMessage()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            return Decode(p, new GodlikeMessage());
        }
    }
}
