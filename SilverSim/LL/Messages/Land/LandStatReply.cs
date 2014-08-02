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

namespace SilverSim.LL.Messages.Land
{
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

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.LandStatReply;
            }
        }

        public new void Serialize(UDPPacket p)
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
