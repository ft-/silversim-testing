// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Object
{
    [UDPMessage(MessageType.ObjectUpdateCached)]
    [Reliable]
    [Trusted]
    public class ObjectUpdateCached : Message
    {
        public UInt64 RegionHandle;
        public UInt16 TimeDilation;
        public struct Data
        {
            public UInt32 LocalID;
            public UInt32 CRC;
            public UInt32 UpdateFlags;
        }

        public List<Data> ObjectData = new List<Data>();

        public ObjectUpdateCached()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt64(RegionHandle);
            p.WriteUInt16(TimeDilation);
            p.WriteUInt8((byte)ObjectData.Count);
            foreach (Data d in ObjectData)
            {
                p.WriteUInt32(d.LocalID);
                p.WriteUInt32(d.CRC);
                p.WriteUInt32(d.UpdateFlags);
            }
        }
    }
}
