// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Viewer.Messages.Economy
{
    [UDPMessage(MessageType.EconomyDataRequest)]
    [Reliable]
    [NotTrusted]
    public class EconomyDataRequest : Message
    {
        public EconomyDataRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            EconomyDataRequest m = new EconomyDataRequest();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            /* no data to serialize */
        }
    }
}
