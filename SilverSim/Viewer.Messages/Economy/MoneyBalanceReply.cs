// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

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
            return new MoneyBalanceReply()
            {
                AgentID = p.ReadUUID(),
                TransactionID = p.ReadUUID(),
                TransactionSuccess = p.ReadBoolean(),
                MoneyBalance = p.ReadInt32(),
                SquareMetersCredit = p.ReadInt32(),
                SquareMetersCommitted = p.ReadInt32(),
                Description = p.ReadStringLen8(),
                TransactionType = p.ReadInt32(),
                SourceID = p.ReadUUID(),
                IsSourceGroup = p.ReadBoolean(),
                DestID = p.ReadUUID(),
                IsDestGroup = p.ReadBoolean(),
                Amount = p.ReadInt32(),
                ItemDescription = p.ReadStringLen8()
            };
        }
    }
}
