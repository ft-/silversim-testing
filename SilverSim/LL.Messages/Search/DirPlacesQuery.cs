// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Viewer.Messages.Search
{
    [UDPMessage(MessageType.DirPlacesQuery)]
    [Reliable]
    public class DirPlacesQuery : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public UUID QueryID;
        public string QueryText;
        public SearchFlags QueryFlags;
        public sbyte Category;
        public string SimName;
        public int QueryStart;

        public DirPlacesQuery()
        {

        }

        public static DirPlacesQuery Decode(UDPPacket p)
        {
            DirPlacesQuery m = new DirPlacesQuery();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            m.QueryText = p.ReadStringLen8();
            m.QueryFlags = (SearchFlags)p.ReadUInt32();
            m.Category = p.ReadInt8();
            m.SimName = p.ReadStringLen8();
            m.QueryStart = p.ReadInt32();

            return m;
        }
    }
}
