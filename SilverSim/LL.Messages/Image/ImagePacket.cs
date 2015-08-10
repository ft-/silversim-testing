// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Image
{
    [UDPMessage(MessageType.ImagePacket)]
    [Reliable]
    [Trusted]
    public class ImagePacket : Message
    {
        public UUID ID = UUID.Zero;
        public UInt16 Packet = 0;
        public byte[] Data = new byte[0];

        public ImagePacket()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(ID);
            p.WriteUInt16(Packet);
            p.WriteUInt16((UInt16)Data.Length);
            p.WriteBytes(Data);
        }
    }
}
