// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Primitive;

namespace SilverSim.Viewer.Messages.Sound
{
    [UDPMessage(MessageType.AttachedSound)]
    [Reliable]
    [Trusted]
    public class AttachedSound : Message
    {
        public UUID SoundID;
        public UUID ObjectID;
        public UUID OwnerID;
        public double Gain;
        public PrimitiveSoundFlags Flags;

        public AttachedSound()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(SoundID);
            p.WriteUUID(ObjectID);
            p.WriteUUID(OwnerID);
            p.WriteFloat((float)Gain);
            p.WriteUInt8((byte)Flags);
        }
    }
}
