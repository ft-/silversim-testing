﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Transfer
{
    [UDPMessage(MessageType.TransferInfo)]
    [Reliable]
    [Trusted]
    public class TransferInfo : Message
    {
        public UUID TransferID;
        public Int32 ChannelType;
        public Int32 TargetType;
        public Int32 Status;
        public Int32 Size;
        public byte[] Params = new byte[0];

        public TransferInfo()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(TransferID);
            p.WriteInt32(ChannelType);
            p.WriteInt32(TargetType);
            p.WriteInt32(Status);
            p.WriteInt32(Size);
            p.WriteUInt16((ushort)Params.Length);
            p.WriteBytes(Params);
        }

        public static Message Decode(UDPPacket p)
        {
            TransferInfo m = new TransferInfo();
            m.TransferID = p.ReadUUID();
            m.ChannelType = p.ReadInt32();
            m.TargetType = p.ReadInt32();
            m.Status = p.ReadInt32();
            m.Size = p.ReadInt32();
            m.Params = p.ReadBytes(p.ReadUInt16());
            return m;
        }
    }
}
