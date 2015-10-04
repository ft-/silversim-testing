// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.HealthMessage)]
    [Reliable]
    [Trusted]
    public class HealthMessage : Message
    {
        public double Health = 0f;

        public HealthMessage()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteFloat((float)Health);
        }
    }
}
