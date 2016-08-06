// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Circuit
{
    [UDPMessage(MessageType.CompleteAgentMovement)]
    [Reliable]
    [NotTrusted]
    public class CompleteAgentMovement : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UInt32 CircuitCode;

        public CompleteAgentMovement()
        {

        }
        public static Message Decode(UDPPacket p)
        {
            CompleteAgentMovement m = new CompleteAgentMovement();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.CircuitCode = p.ReadUInt32();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32(CircuitCode);
        }
    }
}
