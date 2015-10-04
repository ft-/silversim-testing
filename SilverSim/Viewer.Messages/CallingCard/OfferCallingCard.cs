// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.CallingCard
{
    [UDPMessage(MessageType.OfferCallingCard)]
    [Reliable]
    [NotTrusted]
    public class OfferCallingCard : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID DestID;
        public UUID TransactionID;

        public OfferCallingCard()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            OfferCallingCard m = new OfferCallingCard();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.DestID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();

            return m;
        }
    }
}
