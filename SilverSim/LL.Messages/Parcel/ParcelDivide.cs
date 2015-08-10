// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Parcel
{
    [UDPMessage(MessageType.ParcelDivide)]
    [Reliable]
    [NotTrusted]
    public class ParcelDivide : Message
    {
        public UUID AgentID;
        public UUID SessionID;

        public struct ParcelDataEntry
        {
            public double West;
            public double South;
            public double East;
            public double North;
        }

        public List<ParcelDataEntry> ParcelData = new List<ParcelDataEntry>();

        public ParcelDivide()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            ParcelDivide m = new ParcelDivide();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();

            uint c = p.ReadUInt8();
            for (uint i = 0; i < c; ++i)
            {
                ParcelDataEntry d;
                d.West = p.ReadFloat();
                d.South = p.ReadFloat();
                d.East = p.ReadFloat();
                d.North = p.ReadFloat();
                m.ParcelData.Add(d);
            }

            return m;
        }
    }
}
