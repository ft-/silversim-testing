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
        public ImageCodec Codec;
        public UInt32 Size;
        public UInt16 Packets;
        public byte[] Data = new byte[0];

        public ImageData()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(ID);
            p.WriteUInt8((byte)Codec);
            p.WriteUInt32(Size);
            p.WriteUInt16(Packets);
            p.WriteUInt16((UInt16)Data.Length);
            p.WriteBytes(Data);
        }

        public static Message Decode(UDPPacket p)
        {
            ImageData m = new ImageData();
            m.ID = p.ReadUUID();
            m.Codec = (ImageCodec)p.ReadUInt8();
            m.Size = p.ReadUInt32();
            m.Packets = p.ReadUInt16();
            m.Data = p.ReadBytes(p.ReadUInt16());
            return m;
        }
    }
}
