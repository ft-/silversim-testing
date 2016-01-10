﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Transfer
{
    [UDPMessage(MessageType.RequestXfer)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class RequestXfer : Message
    {
        public UInt64 ID;
        public string Filename;
        public byte FilePath;
        public bool DeleteOnCompletion;
        public bool UseBigPackets;
        public UUID VFileID;
        public Int16 VFileType;

        public RequestXfer()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RequestXfer m = new RequestXfer();
            m.ID = p.ReadUInt64();
            m.Filename = p.ReadStringLen8();
            m.FilePath = p.ReadUInt8();
            m.DeleteOnCompletion = p.ReadBoolean();
            m.UseBigPackets = p.ReadBoolean();
            m.VFileID = p.ReadUUID();
            m.VFileType = p.ReadInt16();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt64(ID);
            p.WriteStringLen8(Filename);
            p.WriteUInt8(FilePath);
            p.WriteBoolean(DeleteOnCompletion);
            p.WriteBoolean(UseBigPackets);
            p.WriteUUID(VFileID);
            p.WriteInt16(VFileType);
        }
    }
}
