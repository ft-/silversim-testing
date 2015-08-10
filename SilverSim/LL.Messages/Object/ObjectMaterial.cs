// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Object
{
    [UDPMessage(MessageType.ObjectMaterial)]
    [Reliable]
    [NotTrusted]
    public class ObjectMaterial : Message
    {
        public struct Data
        {
            public UInt32 ObjectLocalID;
            public PrimitiveMaterial Material;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public List<Data> ObjectData = new List<Data>();
        

        public ObjectMaterial()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectMaterial m = new ObjectMaterial();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.ObjectLocalID = p.ReadUInt32();
                d.Material = (PrimitiveMaterial)p.ReadUInt8();
                m.ObjectData.Add(d);
            }
            return m;
        }
    }
}
