// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Transfer
{
    [UDPMessage(MessageType.TransferRequest)]
    [Reliable]
    [NotTrusted]
    public class TransferRequest : Message
    {
        public UUID TransferID;
        public Int32 ChannelType;
        public SourceType SourceType;
        public double Priority;
        public byte[] Params = new byte[0];

        public TransferRequest()
        {

        }

        public static TransferRequest Decode(UDPPacket p)
        {
            TransferRequest m = new TransferRequest();
            m.TransferID = p.ReadUUID();
            m.ChannelType = p.ReadInt32();
            m.SourceType = (SourceType)p.ReadInt32();
            m.Priority = p.ReadFloat();
            uint c = p.ReadUInt16();
            m.Params = p.ReadBytes((int)c);

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(TransferID);
            p.WriteInt32(ChannelType);
            p.WriteInt32((int)SourceType);
            p.WriteFloat((float)Priority);
            p.WriteUInt16((ushort)Params.Length);
            p.WriteBytes(Params);
        }
    }
}
