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
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Groups
{
    [UDPMessage(MessageType.GroupAccountDetailsReply)]
    [Reliable]
    [Zerocoded]
    public class GroupAccountTransactionsReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID RequestID = UUID.Zero;
        public int IntervalDays = 0;
        public int CurrentInterval = 0;
        public string StartDate = string.Empty;

        public class HistoryDataEntry
        {
            public string Time = string.Empty;
            public string User = string.Empty;
            public int Type = 0;
            public string Item = string.Empty;
            public int Amount = 0;

            public HistoryDataEntry()
            {

            }
        }

        public List<HistoryDataEntry> HistoryData = new List<HistoryDataEntry>();

        public GroupAccountTransactionsReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(GroupID);
            p.WriteUUID(RequestID);
            p.WriteInt32(IntervalDays);
            p.WriteInt32(CurrentInterval);
            p.WriteStringLen8(StartDate);
            p.WriteUInt8((byte)HistoryData.Count);
            foreach(HistoryDataEntry e in HistoryData)
            {
                p.WriteStringLen8(e.Time);
                p.WriteStringLen8(e.User);
                p.WriteInt32(e.Type);
                p.WriteStringLen8(e.Item);
                p.WriteInt32(e.Amount);
            }
        }
    }
}
