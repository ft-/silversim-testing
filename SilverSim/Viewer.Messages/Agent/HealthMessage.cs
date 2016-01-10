// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.HealthMessage)]
    [Reliable]
    [Trusted]
    public class HealthMessage : Message
    {
        public double Health;

        public HealthMessage()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteFloat((float)Health);
        }

        public static Message Decode(UDPPacket p)
        {
            HealthMessage m = new HealthMessage();
            m.Health = p.ReadFloat();
            return m;
        }
    }
}
