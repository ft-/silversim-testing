// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Sound
{
    [UDPMessage(MessageType.PreloadSound)]
    [Reliable]
    [Trusted]
    public class PreloadSound : Message
    {
        public UUID ObjectID;
        public UUID OwnerID;
        public UUID SoundID;

        public PreloadSound()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(ObjectID);
            p.WriteUUID(OwnerID);
            p.WriteUUID(SoundID);
        }
    }
}
