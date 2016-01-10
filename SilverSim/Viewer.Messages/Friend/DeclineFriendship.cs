// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Friend
{
    [UDPMessage(MessageType.DeclineFriendship)]
    [Reliable]
    [NotTrusted]
    public class DeclineFriendship : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID TransactionID;

        public DeclineFriendship()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            DeclineFriendship m = new DeclineFriendship();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(TransactionID);
        }
    }
}
