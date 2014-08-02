/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Object
{
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

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ObjectShape;
            }
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
