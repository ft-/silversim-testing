// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Object
{
    [UDPMessage(MessageType.ObjectDescription)]
    [Reliable]
    [NotTrusted]
    public class ObjectDescription : Message
    {
        public struct Data
        {
            public UInt32 ObjectLocalID;
            public string Description;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public List<Data> ObjectData = new List<Data>();

        public ObjectDescription()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectDescription m = new ObjectDescription();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.ObjectLocalID = p.ReadUInt32();
                d.Description = p.ReadStringLen8();
                m.ObjectData.Add(d);
            }
            return m;
        }
    }
}
