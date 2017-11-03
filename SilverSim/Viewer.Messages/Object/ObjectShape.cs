// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
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

        public static Message Decode(UDPPacket p)
        {
            var m = new ObjectShape
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID()
            };
            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                m.ObjectData.Add(new Data
                {
                    ObjectLocalID = p.ReadUInt32(),
                    PathCurve = p.ReadUInt8(),
                    ProfileCurve = p.ReadUInt8(),
                    PathBegin = p.ReadUInt16(),
                    PathEnd = p.ReadUInt16(),
                    PathScaleX = p.ReadUInt8(),
                    PathScaleY = p.ReadUInt8(),
                    PathShearX = p.ReadUInt8(),
                    PathShearY = p.ReadUInt8(),
                    PathTwist = p.ReadInt8(),
                    PathTwistBegin = p.ReadInt8(),
                    PathRadiusOffset = p.ReadInt8(),
                    PathTaperX = p.ReadInt8(),
                    PathTaperY = p.ReadInt8(),
                    PathRevolutions = p.ReadUInt8(),
                    PathSkew = p.ReadInt8(),
                    ProfileBegin = p.ReadUInt16(),
                    ProfileEnd = p.ReadUInt16(),
                    ProfileHollow = p.ReadUInt16()
                });
            }
            return m;
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);

            p.WriteUInt8((byte)ObjectData.Count);
            foreach (var d in ObjectData)
            {
                p.WriteUInt32(d.ObjectLocalID);
                p.WriteUInt8(d.PathCurve);
                p.WriteUInt8(d.ProfileCurve);
                p.WriteUInt16(d.PathBegin);
                p.WriteUInt16(d.PathEnd);
                p.WriteUInt8(d.PathScaleX);
                p.WriteUInt8(d.PathScaleY);
                p.WriteUInt8(d.PathShearX);
                p.WriteUInt8(d.PathShearY);
                p.WriteInt8(d.PathTwist);
                p.WriteInt8(d.PathTwistBegin);
                p.WriteInt8(d.PathRadiusOffset);
                p.WriteInt8(d.PathTaperX);
                p.WriteInt8(d.PathTaperY);
                p.WriteUInt8(d.PathRevolutions);
                p.WriteInt8(d.PathSkew);
                p.WriteUInt16(d.ProfileBegin);
                p.WriteUInt16(d.ProfileEnd);
                p.WriteUInt16(d.ProfileHollow);
            }
        }
    }
}
