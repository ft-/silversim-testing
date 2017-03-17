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

namespace SilverSim.Viewer.Messages.Search
{
    [UDPMessage(MessageType.DirPeopleReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class DirPeopleReply : Message
    {
        public UUID AgentID = UUID.Zero;

        public UUID QueryID = UUID.Zero;

        public class QueryReplyData
        {
            public UUID AgentID;
            public string FirstName = string.Empty;
            public string LastName = string.Empty;
            public string Group = string.Empty;
            public bool Online;
            public int Reputation;

            public QueryReplyData()
            {

            }
        }

        public List<QueryReplyData> QueryReplies = new List<QueryReplyData>();

        public DirPeopleReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(QueryID);
            p.WriteUInt8((byte)QueryReplies.Count);
            foreach(QueryReplyData d in QueryReplies)
            {
                p.WriteUUID(d.AgentID);
                p.WriteStringLen8(d.FirstName);
                p.WriteStringLen8(d.LastName);
                p.WriteStringLen8(d.Group);
                p.WriteBoolean(d.Online);
                p.WriteInt32(d.Reputation);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            DirPeopleReply m = new DirPeopleReply();
            m.AgentID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                QueryReplyData d = new QueryReplyData();
                d.AgentID = p.ReadUUID();
                d.FirstName = p.ReadStringLen8();
                d.LastName = p.ReadStringLen8();
                d.Group = p.ReadStringLen8();
                d.Online = p.ReadBoolean();
                d.Reputation = p.ReadInt32();
                m.QueryReplies.Add(d);
            }
            return m;
        }
    }
}
