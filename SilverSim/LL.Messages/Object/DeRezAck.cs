// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Object
{
    [UDPMessage(MessageType.DeRezAck)]
    [Reliable]
    [Trusted]
    public class DeRezAck : Message
    {
        public UUID TransactionID;
        public bool Success;

        public DeRezAck()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(TransactionID);
            p.WriteBoolean(Success);
        }
    }
}
