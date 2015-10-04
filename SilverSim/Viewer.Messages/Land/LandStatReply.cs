// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
        }

        public UInt32 ReportType;
        public UInt32 RequestFlags;
        public UInt32 TotalObjectCount;

        public List<ReportDataEntry> ReportData = new List<ReportDataEntry>();

        public LandStatReply()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt32(ReportType);
            p.WriteUInt32(RequestFlags);
            p.WriteUInt32(TotalObjectCount);

            p.WriteUInt8((byte)ReportData.Count);
            foreach (ReportDataEntry d in ReportData)
            {
                p.WriteUInt32(d.TaskLocalID);
                p.WriteUUID(d.TaskID);
                p.WriteVector3f(d.Location);
                p.WriteFloat((float)d.Score);
                p.WriteStringLen8(d.TaskName);
                p.WriteStringLen8(d.OwnerName);
            }
        }
    }
}
