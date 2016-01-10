// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Search
{
    [UDPMessage(MessageType.DirFindQuery)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class DirFindQuery : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID QueryID;
        public string QueryText;
        public SearchFlags QueryFlags;
        public int QueryStart;

        public DirFindQuery()
        {

        }

        public static DirFindQuery Decode(UDPPacket p)
        {
            DirFindQuery m = new DirFindQuery();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            m.QueryText = p.ReadStringLen8();
            m.QueryFlags = (SearchFlags)p.ReadUInt32();
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
            p.WriteInt32(QueryStart);
        }
    }
}
