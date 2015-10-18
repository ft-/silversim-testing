// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Economy
{
    [UDPMessage(MessageType.MoneyBalanceReply)]
    [Reliable]
    [Trusted]
    public class MoneyBalanceReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID TransactionID = UUID.Zero;
        public bool TransactionSuccess;
        public Int32 MoneyBalance;
        public Int32 SquareMetersCredit;
        public Int32 SquareMetersCommitted;
        public string Description = string.Empty;
        public Int32 TransactionType;
        public UUID SourceID = UUID.Zero;
        public bool IsSourceGroup;
        public UUID DestID = UUID.Zero;
        public bool IsDestGroup;
        public Int32 Amount;
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
