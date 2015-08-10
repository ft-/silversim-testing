// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Script
{
    [UDPMessage(MessageType.ForceScriptControlRelease)]
    [Reliable]
    [NotTrusted]
    public class ForceScriptControlRelease : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public ForceScriptControlRelease()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ForceScriptControlRelease m = new ForceScriptControlRelease();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            return m;
        }
    }
}
