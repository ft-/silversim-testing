// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Circuit
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
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteVector3f(Position);
            p.WriteVector3f(LookAt);
            p.WriteUInt64(GridPosition.RegionHandle);
            p.WriteUInt32(Timestamp);
            p.WriteStringLen16(ChannelVersion);
        }
    }
}
