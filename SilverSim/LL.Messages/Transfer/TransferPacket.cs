// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Transfer
{
    [UDPMessage(MessageType.TransferPacket)]
    [Reliable]
    [Trusted]
    public class TransferPacket : Message
    {
        public UUID TransferID;
        public Int32 ChannelType;
        public Int32 Packet;
        public Int32 Status;
        public byte[] Data = new byte[0];

        public TransferPacket()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(TransferID);
            p.WriteInt32(ChannelType);
            p.WriteInt32(Packet);
            p.WriteInt32(Status);
            p.WriteUInt16((ushort)Data.Length);
            p.WriteBytes(Data);
        }
    }
}
