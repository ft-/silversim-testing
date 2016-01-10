// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Search
{
    [UDPMessage(MessageType.PlacesQuery)]
    [Reliable]
    [NotTrusted]
    public class PlacesQuery : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UUID QueryID;
        public UUID TransactionID;
        public string QueryText;
        public UInt32 QueryFlags;
        public sbyte Category;
        public string SimName;

        public PlacesQuery()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            PlacesQuery m = new PlacesQuery();

            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.QueryID = p.ReadUUID();
            m.TransactionID = p.ReadUUID();
            m.QueryText = p.ReadStringLen8();
            m.QueryFlags = p.ReadUInt32();
            m.Category = p.ReadInt8();
            m.SimName = p.ReadStringLen8();

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUUID(QueryID);
            p.WriteUUID(TransactionID);
            p.WriteStringLen8(QueryText);
            p.WriteUInt32(QueryFlags);
            p.WriteInt8(Category);
            p.WriteStringLen8(SimName);
        }
    }
}
