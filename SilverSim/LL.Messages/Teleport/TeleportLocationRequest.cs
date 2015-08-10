// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Teleport
{
    [UDPMessage(MessageType.TeleportLocationRequest)]
    [Reliable]
    [NotTrusted]
    public class TeleportLocationRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public GridVector GridPosition = GridVector.Zero;
        public Vector3 Position;
        public Vector3 LookAt;

        public TeleportLocationRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            TeleportLocationRequest m = new TeleportLocationRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.GridPosition.RegionHandle = p.ReadUInt64();
            m.Position = p.ReadVector3f();
            m.LookAt = p.ReadVector3f();

            return m;
        }
    }
}
