// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Search
{
    [UDPMessage(MessageType.DirGroupsReply)]
    [Reliable]
    [Zerocoded]
    [Trusted]
    public class DirGroupsReply : Message
    {
        public UUID AgentID;
        public UUID QueryID;

        public struct QueryReplyData
        {
            public UUID GroupID;
            public string GroupName;
            public int Members;
            public double SearchOrder;
        }

        public List<QueryReplyData> QueryReplies = new List<QueryReplyData>();

        public DirGroupsReply()
        {
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(QueryID);
            p.WriteUInt8((byte)QueryReplies.Count);
            foreach(QueryReplyData d in QueryReplies)
            {
                p.WriteUUID(d.GroupID);
                p.WriteStringLen8(d.GroupName);
                p.WriteInt32(d.Members);
                p.WriteFloat((float)d.SearchOrder);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            DirGroupsReply m = new DirGroupsReply();
            m.AgentID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                QueryReplyData d = new QueryReplyData();
                d.GroupID = p.ReadUUID();
                d.GroupName = p.ReadStringLen8();
                d.Members = p.ReadInt32();
                d.SearchOrder = p.ReadFloat();
                m.QueryReplies.Add(d);
            }
            return m;
        }
    }
}
