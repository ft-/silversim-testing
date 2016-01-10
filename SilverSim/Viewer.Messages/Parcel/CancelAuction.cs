// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.CancelAuction)]
    [Reliable]
    [NotTrusted]
    public class CancelAuction : Message
    {
        public UUID ParcelID;

        public CancelAuction()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            CancelAuction m = new CancelAuction();
            m.ParcelID = p.ReadUUID();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(ParcelID);
        }
    }
}
