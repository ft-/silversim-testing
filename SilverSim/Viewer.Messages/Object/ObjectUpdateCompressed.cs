// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectUpdateCompressed)]
    [Reliable]
    [Trusted]
    public class ObjectUpdateCompressed : Message
    {
        public UInt64 RegionHandle;
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
            p.WriteUInt64(RegionHandle);
            p.WriteUInt16(TimeDilation);
            p.WriteUInt8((byte)ObjectData.Count);
            foreach (ObjData d in ObjectData)
            {
                p.WriteUInt32(d.UpdateFlags);
                p.WriteUInt16((UInt16)d.Data.Length);
                p.WriteBytes(d.Data);
            }
        }
    }
}
