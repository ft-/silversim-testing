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
        
        public static Message Decode(UDPPacket p)
        {
            AttachedSound m = new AttachedSound();
            m.SoundID = p.ReadUUID();
            m.ObjectID = p.ReadUUID();
            m.OwnerID = p.ReadUUID();
            m.Gain = p.ReadFloat();
            m.Flags = (PrimitiveSoundFlags)p.ReadUInt8();
            return m;
        }
    }
}
