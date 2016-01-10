// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Search
{
    [UDPMessage(MessageType.DirClassifiedQuery)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class DirClassifiedQuery : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID QueryID;
        public string QueryText;
        public SearchFlags QueryFlags;
        public UInt32 Category;
        public int QueryStart;

        public DirClassifiedQuery()
        {

        }

        public static DirClassifiedQuery Decode(UDPPacket p)
        {
            DirClassifiedQuery m = new DirClassifiedQuery();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            m.QueryText = p.ReadStringLen8();
            m.QueryFlags = (SearchFlags)p.ReadUInt32();
            m.Category = p.ReadUInt32();
            m.QueryStart = p.ReadInt32();
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(QueryID);
            p.WriteStringLen8(QueryText);
            p.WriteUInt32((uint)QueryFlags);
            p.WriteUInt32(Category);
            p.WriteInt32(QueryStart);
        }
    }
}
