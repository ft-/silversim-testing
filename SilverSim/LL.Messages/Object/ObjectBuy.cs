// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Object
{
    [UDPMessage(MessageType.ObjectBuy)]
    [Reliable]
    [NotTrusted]
    public class ObjectBuy : Message
    {
        public struct Data
        {
            public UInt32 ObjectLocalID;
            public byte SaleType;
            public Int32 SalePrice;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public UUID GroupID = UUID.Zero;
        public UUID CategoryID = UUID.Zero;

        public List<Data> ObjectData = new List<Data>(); 

        public ObjectBuy()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectBuy m = new ObjectBuy();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.ObjectLocalID = p.ReadUInt32();
                d.SaleType = p.ReadUInt8();
                d.SalePrice = p.ReadInt32();
                m.ObjectData.Add(d);
            }
            return m;
        }
    }
}
