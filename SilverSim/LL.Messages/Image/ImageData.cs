// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Image
{
    [UDPMessage(MessageType.ImageData)]
    [Reliable]
    [Trusted]
    public class ImageData : Message
    {
        public UUID ID = UUID.Zero;
        public ImageCodec Codec = ImageCodec.Invalid;
        public UInt32 Size = 0;
        public UInt16 Packets = 0;
        public byte[] Data = new byte[0];

        public ImageData()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(ID);
            p.WriteUInt8((byte)Codec);
            p.WriteUInt32(Size);
            p.WriteUInt16(Packets);
            p.WriteUInt16((UInt16)Data.Length);
            p.WriteBytes(Data);
        }
    }
}
