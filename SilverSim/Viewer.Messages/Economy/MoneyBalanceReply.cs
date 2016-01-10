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

        public static Message Decode(UDPPacket p)
        {
            MoneyBalanceReply m = new MoneyBalanceReply();
            m.AgentID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();
            m.TransactionSuccess = p.ReadBoolean();
            m.MoneyBalance = p.ReadInt32();
            m.SquareMetersCredit = p.ReadInt32();
            m.SquareMetersCommitted = p.ReadInt32();
            m.Description = p.ReadStringLen8();
            m.TransactionType = p.ReadInt32();
            m.SourceID = p.ReadUUID();
            m.IsSourceGroup = p.ReadBoolean();
            m.DestID = p.ReadUUID();
            m.IsDestGroup = p.ReadBoolean();
            m.Amount = p.ReadInt32();
            m.ItemDescription = p.ReadStringLen8();
            return m;
        }
    }
}
