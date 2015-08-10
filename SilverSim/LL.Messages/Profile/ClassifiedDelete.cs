// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.LL.Messages.Profile
{
    [UDPMessage(MessageType.ClassifiedDelete)]
    [Reliable]
    [NotTrusted]
    public class ClassifiedDelete : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID ClassifiedID;

        public ClassifiedDelete()
        {

        }

        public static ClassifiedDelete Decode(UDPPacket p)
        {
            ClassifiedDelete m = new ClassifiedDelete();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ClassifiedID = p.ReadUUID();

            return m;
        }
    }
}
