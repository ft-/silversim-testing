// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Economy
{
    [UDPMessage(MessageType.MoneyBalanceReply)]
    [Reliable]
    [Trusted]
    public class MoneyBalanceReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID TransactionID = UUID.Zero;
        public bool TransactionSuccess = false;
        public Int32 MoneyBalance = 0;
        public Int32 SquareMetersCredit = 0;
        public Int32 SquareMetersCommitted = 0;
        public string Description = string.Empty;
        public Int32 TransactionType = 0;
        public UUID SourceID = UUID.Zero;
        public bool IsSourceGroup = false;
        public UUID DestID = UUID.Zero;
        public bool IsDestGroup = false;
        public Int32 Amount = 0;
        public string ItemDescription = string.Empty;

        public MoneyBalanceReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(TransactionID);
            p.WriteBoolean(TransactionSuccess);
            p.WriteInt32(MoneyBalance);
            p.WriteInt32(SquareMetersCredit);
            p.WriteInt32(SquareMetersCommitted);
            p.WriteStringLen8(Description);
            p.WriteInt32(TransactionType);
            p.WriteUUID(SourceID);
            p.WriteBoolean(IsSourceGroup);
            p.WriteUUID(DestID);
            p.WriteBoolean(IsDestGroup);
            p.WriteInt32(Amount);
            p.WriteStringLen8(ItemDescription);
        }
    }
}
