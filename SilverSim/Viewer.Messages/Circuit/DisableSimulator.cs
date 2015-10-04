// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Viewer.Messages.Circuit
{
    [UDPMessage(MessageType.DisableSimulator)]
    [Reliable]
    [EventQueueGet("DisableSimulator")]
    [Trusted]
    public class DisableSimulator : Message
    {
        public DisableSimulator()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
        }

        public override SilverSim.Types.IValue SerializeEQG()
        {
            SilverSim.Types.Map m = new SilverSim.Types.Map();

            return m;
        }
    }
}
