// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Grid;
using System;

namespace SilverSim.Viewer.Messages.Teleport
{
    [UDPMessage(MessageType.TeleportLocal)]
    [Reliable]
    [Trusted]
    public class TeleportLocal : Message
    {
        public UUID AgentID;
        public UInt32 LocationID;
        public Vector3 Position;
        public Vector3 LookAt;
        public TeleportFlags TeleportFlags;

        public TeleportLocal()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUInt32(LocationID);
            p.WriteVector3f(Position);
            p.WriteVector3f(LookAt);
            p.WriteUInt32((UInt32)TeleportFlags);
        }
    }
}
