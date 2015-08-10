// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Object
{
    [UDPMessage(MessageType.ObjectShape)]
    [Reliable]
    [NotTrusted]
    public class ObjectShape : Message
    {
        public struct Data
        {
            public UInt32 ObjectLocalID;
            public byte PathCurve;
            public byte ProfileCurve;
            public UInt16 PathBegin;// 0 to 1, quanta = 0.01
            public UInt16 PathEnd; // 0 to 1, quanta = 0.01
            public byte PathScaleX; // 0 to 1, quanta = 0.01
            public byte PathScaleY; // 0 to 1, quanta = 0.01
            public byte PathShearX; // -.5 to .5, quanta = 0.01
            public byte PathShearY; // -.5 to .5, quanta = 0.01
            public sbyte PathTwist;  // -1 to 1, quanta = 0.01
            public sbyte PathTwistBegin; // -1 to 1, quanta = 0.01
            public sbyte PathRadiusOffset; // -1 to 1, quanta = 0.01
            public sbyte PathTaperX; // -1 to 1, quanta = 0.01
            public sbyte PathTaperY; // -1 to 1, quanta = 0.01
            public byte PathRevolutions; // 0 to 3, quanta = 0.015
            public sbyte PathSkew; // -1 to 1, quanta = 0.01
            public UInt16 ProfileBegin; // 0 to 1, quanta = 0.01
            public UInt16 ProfileEnd; // 0 to 1, quanta = 0.01
            public UInt16 ProfileHollow; // 0 to 1, quanta = 0.01
        }

        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;
        public List<Data> ObjectData = new List<Data>(); 

        public ObjectShape()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ObjectShape m = new ObjectShape();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                Data d = new Data();
                d.ObjectLocalID = p.ReadUInt32();
                d.PathCurve = p.ReadUInt8();
                d.ProfileCurve = p.ReadUInt8();
                d.PathBegin = p.ReadUInt16();
                d.PathEnd = p.ReadUInt16();
                d.PathScaleX = p.ReadUInt8();
                d.PathScaleY = p.ReadUInt8();
                d.PathShearX = p.ReadUInt8();
                d.PathShearY = p.ReadUInt8();
                d.PathTwist = p.ReadInt8();
                d.PathTwistBegin = p.ReadInt8();
                d.PathRadiusOffset = p.ReadInt8();
                d.PathTaperX = p.ReadInt8();
                d.PathTaperY = p.ReadInt8();
                d.PathRevolutions = p.ReadUInt8();
                d.PathSkew = p.ReadInt8();
                d.ProfileBegin = p.ReadUInt16();
                d.ProfileEnd = p.ReadUInt16();
                d.ProfileHollow = p.ReadUInt16();
                m.ObjectData.Add(d);
            }
            return m;
        }
    }
}
