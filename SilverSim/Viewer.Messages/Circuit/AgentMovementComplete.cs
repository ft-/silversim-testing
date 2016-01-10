// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Circuit
{
    [UDPMessage(MessageType.AgentMovementComplete)]
    [Reliable]
    [Trusted]
    public class AgentMovementComplete : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public Vector3 Position;
        public Vector3 LookAt;
        public GridVector GridPosition;
        public UInt32 Timestamp;
        public string ChannelVersion;

        public AgentMovementComplete()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteVector3f(Position);
            p.WriteVector3f(LookAt);
            p.WriteUInt64(GridPosition.RegionHandle);
            p.WriteUInt32(Timestamp);
            p.WriteStringLen16(ChannelVersion);
        }

        public static Message Decode(UDPPacket p)
        {
            AgentMovementComplete m = new AgentMovementComplete();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.Position = p.ReadVector3f();
            m.LookAt = p.ReadVector3f();
            m.GridPosition.RegionHandle = p.ReadUInt64();
            m.Timestamp = p.ReadUInt32();
            m.ChannelVersion = p.ReadStringLen16();
            return m;
        }
    }
}
