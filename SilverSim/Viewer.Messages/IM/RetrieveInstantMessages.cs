// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.IM
{
    [UDPMessage(MessageType.RetrieveInstantMessages)]
    [Reliable]
    [NotTrusted]
    public class RetrieveInstantMessages : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public RetrieveInstantMessages()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            RetrieveInstantMessages m = new RetrieveInstantMessages();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
        }
    }
}
