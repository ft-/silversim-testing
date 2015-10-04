// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectName)]
    [Reliable]
    [NotTrusted]
    public class ObjectName : Message
    {
        public struct Data
        {
            public UInt32 ObjectLocalID;
            public string Name;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public List<Data> ObjectData = new List<Data>();

        public ObjectName()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectName m = new ObjectName();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.ObjectLocalID = p.ReadUInt32();
                d.Name = p.ReadStringLen8();
                m.ObjectData.Add(d);
            }
            return m;
        }
    }
}
