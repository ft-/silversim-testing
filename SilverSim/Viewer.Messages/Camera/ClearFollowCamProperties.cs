// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Camera
{
    [UDPMessage(MessageType.ClearFollowCamProperties)]
    [Reliable]
    [Trusted]
    public class ClearFollowCamProperties : Message
    {
        public UUID ObjectID = UUID.Zero;

        public ClearFollowCamProperties()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(ObjectID);
        }
    }
}
