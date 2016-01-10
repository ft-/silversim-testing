// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Object
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
            p.WriteUUID(TransactionID);
            p.WriteBoolean(Success);
        }

        public static Message Decode(UDPPacket p)
        {
            DeRezAck m = new DeRezAck();
            m.TransactionID = p.ReadUUID();
            m.Success = p.ReadBoolean();
            return m;
        }
    }
}
