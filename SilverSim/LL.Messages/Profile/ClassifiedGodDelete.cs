// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Profile
{
    [UDPMessage(MessageType.ClassifiedGodDelete)]
    [Reliable]
    [NotTrusted]
    public class ClassifiedGodDelete : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID ClassifiedID;
        public UUID QueryID;

        public ClassifiedGodDelete()
        {

        }

        public static ClassifiedGodDelete Decode(UDPPacket p)
        {
            ClassifiedGodDelete m = new ClassifiedGodDelete();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ClassifiedID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            return m;
        }
    }
}
