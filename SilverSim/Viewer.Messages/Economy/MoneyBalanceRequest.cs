// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Economy
{
    [UDPMessage(MessageType.MoneyBalanceRequest)]
    [Reliable]
    [NotTrusted]
    public class MoneyBalanceRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID TransactionID;

        public MoneyBalanceRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            MoneyBalanceRequest m = new MoneyBalanceRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();

            return m;
        }
    }
}
