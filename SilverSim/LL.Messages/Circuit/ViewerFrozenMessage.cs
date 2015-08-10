// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.LL.Messages.Circuit
{
    [UDPMessage(MessageType.ViewerFrozenMessage)]
    [Reliable]
    [Trusted]
    public class ViewerFrozenMessage : Message
    {
        public bool Frozen;

        public ViewerFrozenMessage()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteBoolean(Frozen);
        }
    }
}
