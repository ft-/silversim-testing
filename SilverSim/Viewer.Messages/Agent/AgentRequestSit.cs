// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.AgentRequestSit)]
    [Reliable]
    [NotTrusted]
    public class AgentRequestSit : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID TargetID = UUID.Zero;
        public Vector3 Offset = Vector3.Zero;

        public static Message Decode(UDPPacket p)
        {
            AgentRequestSit m = new AgentRequestSit();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.TargetID = p.ReadUUID();
            m.Offset = p.ReadVector3f();
            return m;
        }
    }
}
