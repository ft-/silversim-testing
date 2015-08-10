// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Grid;

namespace SilverSim.LL.Messages.Teleport
{
    [UDPMessage(MessageType.TeleportLureRequest)]
    [Reliable]
    [NotTrusted]
    public class TeleportLureRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID LureID;
        public TeleportFlags TeleportFlags;

        public TeleportLureRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            TeleportLureRequest m = new TeleportLureRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.LureID = p.ReadUUID();
            m.TeleportFlags = (TeleportFlags)p.ReadUInt32();

            return m;
        }
    }
}
