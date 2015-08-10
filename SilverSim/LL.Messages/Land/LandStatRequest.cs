// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Land
{
    [UDPMessage(MessageType.LandStatRequest)]
    [Reliable]
    [NotTrusted]
    public class LandStatRequest : Message
    {
        public UUID AgentID;
        public UUID SessionID;
        public UInt32 ReportType;
        public UInt32 RequestFlags;
        public string Filter;
        public Int32 ParcelLocalID;

        public LandStatRequest()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            LandStatRequest m = new LandStatRequest();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.ReportType = p.ReadUInt32();
            m.RequestFlags = p.ReadUInt32();
            m.Filter = p.ReadStringLen8();
            m.ParcelLocalID = p.ReadInt32();

            return m;
        }
    }
}
