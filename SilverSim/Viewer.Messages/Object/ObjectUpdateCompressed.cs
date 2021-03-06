﻿// SilverSim is distributed under the terms of the
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
    [UDPMessage(MessageType.ObjectUpdateCompressed)]
    [Reliable]
    [Trusted]
    public class ObjectUpdateCompressed : Message
    {
        public GridVector Location;
        public ushort TimeDilation;
        public struct ObjData
        {
            public uint UpdateFlags;
            public byte[] Data;
        }

        [Flags]
        public enum CompressedFlags : uint
        {
            None = 0x0000,
            ScratchPad = 0x0001,
            Tree = 0x0002,
            HasText = 0x0004,
            /** <summary>86 byte particle system</summary> */
            HasParticles = 0x0008,
            HasSound = 0x0010,
            HasParent = 0x0020,
            TextureAnimation = 0x0040,
            HasAngularVelocity = 0x0080,
            HasNameValues = 0x0100,
            MediaURL = 0x0200,
            /** <summary>particles new flag signal</summary>
             * When set and HasParticles is not set (particle system is not 86 bytes in that case),
             * it notifies the viewer not to remove the particle system when ObjectUpdateCompressed is received.
             */
            HasParticlesNew = 0x0400
        }

        public List<ObjData> ObjectData = new List<ObjData>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt64(Location.RegionHandle);
            p.WriteUInt16(TimeDilation);
            p.WriteUInt8((byte)ObjectData.Count);
            foreach (var d in ObjectData)
            {
                p.WriteUInt32(d.UpdateFlags);
                p.WriteUInt16((ushort)d.Data.Length);
                p.WriteBytes(d.Data);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            var m = new ObjectUpdateCompressed
            {
                Location = new GridVector(p.ReadUInt64()),
                TimeDilation = p.ReadUInt16()
            };
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                m.ObjectData.Add(new ObjData
                {
                    UpdateFlags = p.ReadUInt32(),
                    Data = p.ReadBytes(p.ReadUInt16())
                });
            }
            return m;
        }
    }
}
