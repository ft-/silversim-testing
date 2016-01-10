// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Groups
{
    [UDPMessage(MessageType.GroupAccountDetailsReply)]
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
            GroupAccountTransactionsReply m = new GroupAccountTransactionsReply();
            m.AgentID = p.ReadUUID();
            m.GroupID = p.ReadUUID();
            m.RequestID = p.ReadUUID();
            m.IntervalDays = p.ReadInt32();
            m.CurrentInterval = p.ReadInt32();
            m.StartDate = p.ReadStringLen8();
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                HistoryDataEntry d = new HistoryDataEntry();
                d.Time = p.ReadStringLen8();
                d.User = p.ReadStringLen8();
                d.Type = p.ReadInt32();
                d.Item = p.ReadStringLen8();
                d.Amount = p.ReadInt32();
                m.HistoryData.Add(d);
            }
            return m;
        }
    }
}
