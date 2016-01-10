// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Search
{
    [UDPMessage(MessageType.DirClassifiedReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class DirClassifiedReply : Message
    {
        public UUID AgentID;
        public UUID QueryID;

        public struct QueryReplyData
        {
            public UUID ClassifiedID;
            public string Name;
            public byte ClassifiedFlags;
            public Date CreationDate;
            public Date ExpirationDate;
            public int PriceForListing;

            public UInt32 Status;
        }

        public List<QueryReplyData> QueryReplies = new List<QueryReplyData>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(QueryID);
            p.WriteUInt8((byte)QueryReplies.Count);
            foreach(QueryReplyData d in QueryReplies)
            {
                p.WriteUUID(d.ClassifiedID);
                p.WriteStringLen8(d.Name);
                p.WriteUInt8(d.ClassifiedFlags);
                p.WriteUInt32(d.CreationDate.AsUInt);
                p.WriteUInt32(d.ExpirationDate.AsUInt);
                p.WriteInt32(d.PriceForListing);
            }

            p.WriteUInt8((byte)QueryReplies.Count);
            foreach (QueryReplyData d in QueryReplies)
            {
                p.WriteUInt32(d.Status);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            DirClassifiedReply m = new DirClassifiedReply();
            m.AgentID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            uint n = p.ReadUInt8();
            while (n-- != 0)
            {
                QueryReplyData d = new QueryReplyData();
                d.ClassifiedID = p.ReadUUID();
                d.Name = p.ReadStringLen8();
                d.ClassifiedFlags = p.ReadUInt8();
                d.CreationDate = Date.UnixTimeToDateTime(p.ReadUInt32());
                d.ExpirationDate = Date.UnixTimeToDateTime(p.ReadUInt32());
                d.PriceForListing = p.ReadInt32();
                m.QueryReplies.Add(d);
            }
            n = p.ReadUInt8();
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
