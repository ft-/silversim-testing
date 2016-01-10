// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Transfer
{
    [UDPMessage(MessageType.ConfirmXferPacket)]
    [Reliable]
    [NotTrusted]
    public class ConfirmXferPacket : Message
    {
        public UInt64 ID;
        public UInt32 Packet;

        public ConfirmXferPacket()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ConfirmXferPacket m = new ConfirmXferPacket();
            m.ID = p.ReadUInt64();
            m.Packet = p.ReadUInt32();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt64(ID);
            p.WriteUInt32(Packet);
        }
    }
}
