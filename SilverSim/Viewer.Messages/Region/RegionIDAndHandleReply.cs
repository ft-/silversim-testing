// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Region
{
    [UDPMessage(MessageType.RegionIDAndHandleReply)]
    [Reliable]
    [Trusted]
    public class RegionIDAndHandleReply : Message
    {
        public UUID RegionID;
        public GridVector RegionPosition;

        public RegionIDAndHandleReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(RegionID);
            p.WriteUInt64(RegionPosition.RegionHandle);
        }
    }
}
