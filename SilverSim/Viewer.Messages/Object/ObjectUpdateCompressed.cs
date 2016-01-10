// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
        public UInt16 TimeDilation;
        public struct ObjData
        {
            public UInt32 UpdateFlags;
            public byte[] Data;
        }

        public List<ObjData> ObjectData = new List<ObjData>();

        public ObjectUpdateCompressed()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt64(Location.RegionHandle);
            p.WriteUInt16(TimeDilation);
            p.WriteUInt8((byte)ObjectData.Count);
            foreach (ObjData d in ObjectData)
            {
                p.WriteUInt32(d.UpdateFlags);
                p.WriteUInt16((UInt16)d.Data.Length);
                p.WriteBytes(d.Data);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            ObjectUpdateCompressed m = new ObjectUpdateCompressed();
            m.Location.RegionHandle = p.ReadUInt64();
            m.TimeDilation = p.ReadUInt16();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                ObjData d = new ObjData();
                d.UpdateFlags = p.ReadUInt32();
                d.Data = p.ReadBytes(p.ReadUInt16());
                m.ObjectData.Add(d);
            }
            return m;
        }
    }
}
