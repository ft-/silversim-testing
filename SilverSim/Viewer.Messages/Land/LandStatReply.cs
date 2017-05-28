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

namespace SilverSim.Viewer.Messages.Land
{
    [UDPMessage(MessageType.LandStatReply)]
    [Reliable]
    [Trusted]
    public class LandStatReply : Message
    {
        public struct ReportDataEntry
        {
            public UInt32 TaskLocalID;
            public UUID TaskID;
            public Vector3 Location;
            public double Score;
            public string TaskName;
            public string OwnerName;

            public int MessageLength
            {
                get
                {
                    return 40 + TaskName.ToUTF8ByteCount() + OwnerName.ToUTF8ByteCount();
                }
            }
        }

        public UInt32 ReportType;
        public UInt32 RequestFlags;
        public UInt32 TotalObjectCount;

        public List<ReportDataEntry> ReportData = new List<ReportDataEntry>();

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt32(ReportType);
            p.WriteUInt32(RequestFlags);
            p.WriteUInt32(TotalObjectCount);

            p.WriteUInt8((byte)ReportData.Count);
            foreach (var d in ReportData)
            {
                p.WriteUInt32(d.TaskLocalID);
                p.WriteUUID(d.TaskID);
                p.WriteVector3f(d.Location);
                p.WriteFloat((float)d.Score);
                p.WriteStringLen8(d.TaskName);
                p.WriteStringLen8(d.OwnerName);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            var m = new LandStatReply()
            {
                ReportType = p.ReadUInt32(),
                RequestFlags = p.ReadUInt32(),
                TotalObjectCount = p.ReadUInt32()
            };
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                m.ReportData.Add(new ReportDataEntry()
                {
                    TaskLocalID = p.ReadUInt32(),
                    TaskID = p.ReadUUID(),
                    Location = p.ReadVector3f(),
                    Score = p.ReadFloat(),
                    TaskName = p.ReadStringLen8(),
                    OwnerName = p.ReadStringLen8()
                });
            }
            return m;
        }
    }
}
