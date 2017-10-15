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
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupAccountTransactionsReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class GroupAccountTransactionsReply : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID RequestID = UUID.Zero;
        public int IntervalDays;
        public int CurrentInterval;
        public string StartDate = string.Empty;

        public class HistoryDataEntry
        {
            public string Time = string.Empty;
            public string User = string.Empty;
            public int Type;
            public string Item = string.Empty;
            public int Amount;
        }

        public List<HistoryDataEntry> HistoryData = new List<HistoryDataEntry>();

        public override void Serialize(UDPPacket p)
        {
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

        public static Message Decode(UDPPacket p)
        {
            var m = new GroupAccountTransactionsReply
            {
                AgentID = p.ReadUUID(),
                GroupID = p.ReadUUID(),
                RequestID = p.ReadUUID(),
                IntervalDays = p.ReadInt32(),
                CurrentInterval = p.ReadInt32(),
                StartDate = p.ReadStringLen8()
            };
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                m.HistoryData.Add(new HistoryDataEntry
                {
                    Time = p.ReadStringLen8(),
                    User = p.ReadStringLen8(),
                    Type = p.ReadInt32(),
                    Item = p.ReadStringLen8(),
                    Amount = p.ReadInt32()
                });
            }
            return m;
        }
    }
}
