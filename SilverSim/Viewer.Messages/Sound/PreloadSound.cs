﻿// SilverSim is distributed under the terms of the
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

        public static Message Decode(UDPPacket p)
        {
            PreloadSound m = new PreloadSound();
            m.ObjectID = p.ReadUUID();
            m.OwnerID = p.ReadUUID();
            m.SoundID = p.ReadUUID();
            return m;
        }
    }
}
