// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectIncludeInSearch)]
    [Reliable]
    [NotTrusted]
    public class ObjectIncludeInSearch : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 ObjectLocalID;
        public bool IncludeInSearch;

        public ObjectIncludeInSearch()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectIncludeInSearch m = new ObjectIncludeInSearch();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ObjectLocalID = p.ReadUInt32();
            m.IncludeInSearch = p.ReadBoolean();

            return m;
        }
    }
}
