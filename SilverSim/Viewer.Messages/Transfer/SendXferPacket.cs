// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Transfer
{
    [UDPMessage(MessageType.SendXferPacket)]
    [Reliable]
    [NotTrusted]
    public class SendXferPacket : Message
    {
        public UInt64 ID;
        public UInt32 Packet;
        public byte[] Data = new byte[0];

        public SendXferPacket()
        {

        }

        public static SendXferPacket Decode(UDPPacket p)
        {
            SendXferPacket m = new SendXferPacket();
            m.ID = p.ReadUInt64();
            m.Packet = p.ReadUInt32();
            int len = (int)p.ReadUInt16();
            m.Data = p.ReadBytes(len);
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt64(ID);
            p.WriteUInt32(Packet);
            p.WriteUInt16((ushort)Data.Length);
            p.WriteBytes(Data);
        }
    }
}
