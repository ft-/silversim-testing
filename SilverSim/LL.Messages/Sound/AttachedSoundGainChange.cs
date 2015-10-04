// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Sound
{
    [UDPMessage(MessageType.AttachedSoundGainChange)]
    [Reliable]
    [Trusted]
    public class AttachedSoundGainChange : Message
    {
        public UUID ObjectID;
        public double Gain;

        public AttachedSoundGainChange()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(ObjectID);
            p.WriteFloat((float)Gain);
        }
    }
}
