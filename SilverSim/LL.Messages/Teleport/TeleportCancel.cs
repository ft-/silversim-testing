// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Teleport
{
    [UDPMessage(MessageType.TeleportCancel)]
    [Reliable]
    [NotTrusted]
    public class TeleportCancel : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public TeleportCancel()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            TeleportCancel m = new TeleportCancel();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            return m;
        }
    }
}
