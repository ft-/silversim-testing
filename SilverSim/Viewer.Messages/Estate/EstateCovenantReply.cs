// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Estate
{
    [UDPMessage(MessageType.EstateCovenantReply)]
    [Reliable]
    [Trusted]
    public class EstateCovenantReply : Message
    {
        public UUID CovenantID = UUID.Zero;
        public UInt32 CovenantTimestamp;
        public string EstateName = string.Empty;
        public UUID EstateOwnerID = UUID.Zero;

        public EstateCovenantReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(CovenantID);
            p.WriteUInt32(CovenantTimestamp);
            p.WriteStringLen8(EstateName);
            p.WriteUUID(EstateOwnerID);
        }

        public static Message Decode(UDPPacket p)
        {
            EstateCovenantReply m = new EstateCovenantReply();
            m.CovenantID = p.ReadUUID();
            m.CovenantTimestamp = p.ReadUInt32();
            m.EstateName = p.ReadStringLen8();
            m.EstateOwnerID = p.ReadUUID();
            return m;
        }
    }
}
