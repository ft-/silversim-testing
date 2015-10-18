// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
            p.WriteMessageType(Number);
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
    }
}
