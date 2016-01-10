// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Search
{
    [UDPMessage(MessageType.DirEventsReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class DirEventsReply : Message
    {
        public UUID AgentID;
        public UUID QueryID;

        public struct QueryReplyData
        {
            public UUID OwnerID;
            public string Name;
            public UUID EventID;
            public string Date;
            public UInt32 UnixTime;
            public UInt32 EventFlags;
            public UInt32 Status;
        }

        public List<QueryReplyData> QueryReplies = new List<QueryReplyData>();

        public DirEventsReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(QueryID);
            p.WriteUInt8((byte)QueryReplies.Count);
            foreach(QueryReplyData d in QueryReplies)
            {
                p.WriteUUID(d.OwnerID);
                p.WriteStringLen8(d.Name);
                p.WriteUUID(d.EventID);
                p.WriteStringLen8(d.Date);
                p.WriteUInt32(d.UnixTime);
                p.WriteUInt32(d.EventFlags);
            }

            p.WriteUInt8((byte)QueryReplies.Count);
            foreach (QueryReplyData d in QueryReplies)
            {
                p.WriteUInt32(d.Status);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            DirEventsReply m = new DirEventsReply();
            m.AgentID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                QueryReplyData d = new QueryReplyData();
                d.OwnerID = p.ReadUUID();
                d.Name = p.ReadStringLen8();
                d.EventID = p.ReadUUID();
                d.Date = p.ReadStringLen8();
                d.UnixTime = p.ReadUInt32();
                d.EventFlags = p.ReadUInt32();
                m.QueryReplies.Add(d);
            }

            for (int i = 0; i < n && i < m.QueryReplies.Count; ++i)
            {
                QueryReplyData d = m.QueryReplies[i];
                d.Status = p.ReadUInt32();
                m.QueryReplies[i] = d;
            }
            return m;
        }
    }
}
