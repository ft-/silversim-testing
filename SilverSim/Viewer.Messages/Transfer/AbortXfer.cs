// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Transfer
{
    [UDPMessage(MessageType.AbortXfer)]
    [Reliable]
    [NotTrusted]
    public class AbortXfer : Message
    {
        public UInt64 ID;
        public Int32 Result;

        public AbortXfer()
        {

        }

        public static AbortXfer Decode(UDPPacket p)
        {
            AbortXfer m = new AbortXfer();
            m.ID = p.ReadUInt64();
            m.Result = p.ReadInt32();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt64(ID);
            p.WriteInt32(Result);
        }
    }
}
