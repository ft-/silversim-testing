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
        public UUID SessionID = UUID.Zero;
        public UUID AgentID = UUID.Zero;
        public UInt32 CircuitCode = 0;

        public CompleteAgentMovement()
        {

        }
        public static Message Decode(UDPPacket p)
        {
            CompleteAgentMovement m = new CompleteAgentMovement();
            m.SessionID = p.ReadUUID();
            m.AgentID = p.ReadUUID();
            m.CircuitCode = p.ReadUInt32();
            return m;
        }
    }
}
