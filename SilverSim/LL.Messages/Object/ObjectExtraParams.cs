// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Object
{
    [UDPMessage(MessageType.ObjectExtraParams)]
    [Reliable]
    [NotTrusted]
    public class ObjectExtraParams : Message
    {
        public struct Data
        {
            public UInt32 ObjectLocalID;
            public UInt16 ParamType;
            public bool ParamInUse;
            public UInt32 ParamSize;
            public byte[] ParamData;
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public List<Data> ObjectData = new List<Data>(); 

        public ObjectExtraParams()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectExtraParams m = new ObjectExtraParams();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.ObjectLocalID = p.ReadUInt32();
                d.ParamType = p.ReadUInt16();
                d.ParamInUse = p.ReadBoolean();
                d.ParamSize = p.ReadUInt32();
                d.ParamData = p.ReadBytes(p.ReadUInt8());
                m.ObjectData.Add(d);
            }
            return m;
        }
    }
}
