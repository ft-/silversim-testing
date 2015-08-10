// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Region
{
    [UDPMessage(MessageType.RegionHandleRequest)]
    [Reliable]
    [NotTrusted]
    public class RegionHandleRequest : Message
    {
        public UUID RegionID;

        public RegionHandleRequest()
        {

        }

        public static RegionHandleRequest Decode(UDPPacket p)
        {
            RegionHandleRequest m = new RegionHandleRequest();
            m.RegionID = p.ReadUUID();

            return m;
        }
    }
}
