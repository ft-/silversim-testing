// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Profile
{
    [UDPMessage(MessageType.ClassifiedInfoRequest)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ClassifiedInfoRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID ClassifiedID;

        public ClassifiedInfoRequest()
        {

        }

        public static ClassifiedInfoRequest Decode(UDPPacket p)
        {
            ClassifiedInfoRequest m = new ClassifiedInfoRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ClassifiedID = p.ReadUUID();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(ClassifiedID);
        }
    }
}
