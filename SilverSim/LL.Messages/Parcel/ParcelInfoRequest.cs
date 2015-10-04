// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelInfoRequest)]
    [Reliable]
    [NotTrusted]
    public class ParcelInfoRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID ParcelID;

        public ParcelInfoRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelInfoRequest m = new ParcelInfoRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ParcelID = p.ReadUUID();

            return m;
        }
    }
}
