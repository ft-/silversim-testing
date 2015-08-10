// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Teleport
{
    [UDPMessage(MessageType.TeleportRequest)]
    [Reliable]
    [NotTrusted]
    public class TeleportRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID RegionID;
        public Vector3 Position;
        public Vector3 LookAt;

        public TeleportRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            TeleportRequest m = new TeleportRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.RegionID = p.ReadUUID();
            m.Position = p.ReadVector3f();
            m.LookAt = p.ReadVector3f();

            return m;
        }
    }
}
