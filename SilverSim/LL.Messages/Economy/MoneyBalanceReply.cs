/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Economy
{
    [UDPMessage(MessageType.MoneyBalanceReply)]
    [Reliable]
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
