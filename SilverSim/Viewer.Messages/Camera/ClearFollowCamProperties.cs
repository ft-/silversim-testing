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
            p.WriteUUID(ObjectID);
        }

        public static Message Decode(UDPPacket p)
        {
            ClearFollowCamProperties m = new ClearFollowCamProperties();
            m.ObjectID = p.ReadUUID();
            return m;
        }
    }
}
