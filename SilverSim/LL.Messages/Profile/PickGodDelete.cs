// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Profile
{
    [UDPMessage(MessageType.PickGodDelete)]
    [Reliable]
    [NotTrusted]
    public class PickGodDelete : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID PickID;
        public UUID QueryID;

        public PickGodDelete()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            PickGodDelete m = new PickGodDelete();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.PickID = p.ReadUUID();
            m.QueryID = p.ReadUUID();

            return m;
        }
    }
}
