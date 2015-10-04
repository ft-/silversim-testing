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
        public UInt32 CovenantTimestamp = 0;
        public string EstateName = string.Empty;
        public UUID EstateOwnerID = UUID.Zero;

        public EstateCovenantReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(CovenantID);
            p.WriteUInt32(CovenantTimestamp);
            p.WriteStringLen8(EstateName);
            p.WriteUUID(EstateOwnerID);
        }
    }
}
