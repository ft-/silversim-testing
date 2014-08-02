using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.LL.Messages.Object
{
    public class ImprovedTerseObjectUpdate : Message
    {
        public UInt64 RegionHandle = 0;
        public UInt16 TimeDilation = 0;

        public struct ObjData
        {
            public byte[] Data;
            public byte[] TextureEntry;
        }

        public List<ObjData> ObjectData = new List<ObjData>();

        public ImprovedTerseObjectUpdate()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.ImprovedTerseObjectUpdate;
            }
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt64(RegionHandle);
            p.WriteUInt16(TimeDilation);
            p.WriteUInt8((byte)ObjectData.Count);
            foreach (ObjData d in ObjectData)
            {
                p.WriteUInt8((byte)d.Data.Length);
                p.WriteBytes(d.Data);
                p.WriteUInt16BE((UInt16)d.TextureEntry.Length);
                p.WriteBytes(d.TextureEntry);
            }
        }
    }
}
