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
            var m = new DirClassifiedReply()
            {
                AgentID = p.ReadUUID(),
                QueryID = p.ReadUUID()
            };
            uint n = p.ReadUInt8();
            while (n-- != 0)
            {
                m.QueryReplies.Add(new QueryReplyData()
                {
                    ClassifiedID = p.ReadUUID(),
                    Name = p.ReadStringLen8(),
                    ClassifiedFlags = p.ReadUInt8(),
                    CreationDate = Date.UnixTimeToDateTime(p.ReadUInt32()),
                    ExpirationDate = Date.UnixTimeToDateTime(p.ReadUInt32()),
                    PriceForListing = p.ReadInt32()
                });
            }
            n = p.ReadUInt8();
            for (int i = 0; i < n && i < m.QueryReplies.Count; ++i)
            {
                var d = m.QueryReplies[i];
                d.Status = p.ReadUInt32();
                m.QueryReplies[i] = d;
            }
            return m;
        }
    }
}
