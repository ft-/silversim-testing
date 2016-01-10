// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Agent
{
    [UDPMessage(MessageType.AgentThrottle)]
    [Reliable]
    [NotTrusted]
    public class AgentThrottle : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UInt32 CircuitCode;

        public UInt32 GenCounter;
        public byte[] Throttles;

        public AgentThrottle()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt32(CircuitCode);
            p.WriteUInt32(GenCounter);
            p.WriteUInt8((byte)Throttles.Length);
            p.WriteBytes(Throttles);
        }

        public static Message Decode(UDPPacket p)
        {
            AgentThrottle m = new AgentThrottle();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.CircuitCode = p.ReadUInt32();
            m.GenCounter = p.ReadUInt32();
            m.Throttles = p.ReadBytes((int)(uint)p.ReadUInt8());
            return m;
        }
    }
}
