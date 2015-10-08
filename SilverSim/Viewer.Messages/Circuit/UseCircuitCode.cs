// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Circuit
{
    [UDPMessage(MessageType.UseCircuitCode)]
    [Reliable]
    [NotTrusted]
    public class UseCircuitCode : Message
    {
        public UInt32 CircuitCode = 0;
        public UUID SessionID = UUID.Zero;
        public UUID AgentID = UUID.Zero;
        
        public UseCircuitCode()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            UseCircuitCode m = new UseCircuitCode();
            m.CircuitCode = p.ReadUInt32();
            m.SessionID = p.ReadUUID();
            m.AgentID = p.ReadUUID();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(MessageType.UseCircuitCode);
            p.WriteUInt32(CircuitCode);
            p.WriteUUID(SessionID);
            p.WriteUUID(AgentID);
        }
    }
}
