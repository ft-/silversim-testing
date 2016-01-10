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
            /* no data to serialize */
        }

        public override Types.IValue SerializeEQG()
        {
            Types.Map m = new Types.Map();

            return m;
        }
    }
}
