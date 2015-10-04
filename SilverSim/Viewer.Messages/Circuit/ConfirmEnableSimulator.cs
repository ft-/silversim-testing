// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Circuit
{
    [UDPMessage(MessageType.ConfirmEnableSimulator)]
    [Reliable]
    [NotTrusted]
    public class ConfirmEnableSimulator : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;


        public ConfirmEnableSimulator()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
        }

        public static Message Decode(UDPPacket p)
        {
            ConfirmEnableSimulator m = new ConfirmEnableSimulator();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            return m;
        }
    }
}
