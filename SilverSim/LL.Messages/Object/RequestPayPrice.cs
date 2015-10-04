// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.RequestPayPrice)]
    [Reliable]
    [NotTrusted]
    public class RequestPayPrice : Message
    {
        public UUID ObjectID = UUID.Zero;

        public RequestPayPrice()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RequestPayPrice m = new RequestPayPrice();
            m.ObjectID = p.ReadUUID();

            return m;
        }
    }
}
