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
    [UDPMessage(MessageType.ImprovedTerseObjectUpdate)]
    [Reliable]
    [Trusted]
    public class ImprovedTerseObjectUpdate : Message
    {
        public GridVector GridPosition;
        public UInt16 TimeDilation;

        public class ObjData
        {

            public byte[] Data;
            public byte[] TextureEntry;
        }

        public List<ObjData> ObjectData = new List<ObjData>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt64(GridPosition.RegionHandle);
            p.WriteUInt16(TimeDilation);
            p.WriteUInt8((byte)ObjectData.Count);
            foreach (var d in ObjectData)
            {
                p.WriteUInt8((byte)d.Data.Length);
                p.WriteBytes(d.Data);
                p.WriteUInt16((UInt16)d.TextureEntry.Length);
                p.WriteBytes(d.TextureEntry);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            var m = new ImprovedTerseObjectUpdate();
            m.GridPosition.RegionHandle = p.ReadUInt64();
            m.TimeDilation = p.ReadUInt16();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                m.ObjectData.Add(new ObjData()
                {
                    Data = p.ReadBytes(p.ReadUInt8()),
                    TextureEntry = p.ReadBytes(p.ReadUInt16())
                });
            }
            return m;
        }
    }
}
