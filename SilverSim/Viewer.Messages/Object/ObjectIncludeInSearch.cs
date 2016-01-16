// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectIncludeInSearch)]
    [Reliable]
    [NotTrusted]
    public class ObjectIncludeInSearch : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public struct Data
        {
            public UInt32 ObjectLocalID;
            public bool IncludeInSearch;
        }

        public List<Data> ObjectData = new List<Data>();

        public ObjectIncludeInSearch()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectIncludeInSearch m = new ObjectIncludeInSearch();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                Data d = new Data();
                d.ObjectLocalID = p.ReadUInt32();
                d.IncludeInSearch = p.ReadBoolean();
                m.ObjectData.Add(d);
            }

            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteUInt8((byte)ObjectData.Count);
            foreach (Data d in ObjectData)
            {
                p.WriteUInt32(d.ObjectLocalID);
                p.WriteBoolean(d.IncludeInSearch);
            }
        }
    }
}
