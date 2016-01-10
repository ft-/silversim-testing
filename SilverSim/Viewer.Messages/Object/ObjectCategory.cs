// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectCategory)]
    [Reliable]
    [Zerocoded]
    [NotTrusted]
    public class ObjectCategory : Message
    {
        public struct Data
        {
            public UInt32 ObjectLocalID;
            public UInt32 Category;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public List<Data> ObjectData = new List<Data>();

        public ObjectCategory()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectCategory m = new ObjectCategory();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.ObjectLocalID = p.ReadUInt32();
                d.Category = p.ReadUInt32();
                m.ObjectData.Add(d);
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);

            p.WriteUInt8((byte)ObjectData.Count);
            foreach(Data d in ObjectData)
            {
                p.WriteUInt32(d.ObjectLocalID);
                p.WriteUInt32(d.Category);
            }
        }
    }
}
