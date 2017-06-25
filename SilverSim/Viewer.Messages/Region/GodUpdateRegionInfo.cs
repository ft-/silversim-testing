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

namespace SilverSim.Viewer.Messages.Region
{
    [UDPMessage(MessageType.GodUpdateRegionInfo)]
    [Reliable]
    [NotTrusted]
    public class GodUpdateRegionInfo : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public string SimName = string.Empty;
        public UInt32 EstateID;
        public UInt32 ParentEstateID;
        public UInt32 RegionFlags;
        public double BillableFactor;
        public Int32 PricePerMeter;
        public Int32 RedirectGridX;
        public Int32 RedirectGridY;

        public List<UInt64> RegionFlagsExtended = new List<UInt64>();

        private static List<UInt64> ReadRegionFlagsExtended(UDPPacket p)
        {
            List<UInt64> res = new List<ulong>();
            uint n;
            try
            {
                n = p.ReadUInt8();
            }
            catch
            {
                n = 0;
            }
            while(n--!=0)
            {
                res.Add(p.ReadUInt64());
            }
            return res;
        }

        public static Message Decode(UDPPacket p) => new GodUpdateRegionInfo
        {
            AgentID = p.ReadUUID(),
            SessionID = p.ReadUUID(),
            SimName = p.ReadStringLen8(),
            EstateID = p.ReadUInt32(),
            ParentEstateID = p.ReadUInt32(),
            RegionFlags = p.ReadUInt32(),
            BillableFactor = p.ReadFloat(),
            PricePerMeter = p.ReadInt32(),
            RedirectGridX = p.ReadInt32(),
            RedirectGridY = p.ReadInt32(),
            RegionFlagsExtended = ReadRegionFlagsExtended(p)
        };

        public override void Serialize(UDPPacket p)
        {
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteStringLen8(SimName);
            p.WriteUInt32(EstateID);
            p.WriteUInt32(ParentEstateID);
            p.WriteUInt32(RegionFlags);
            p.WriteFloat((float)BillableFactor);
            p.WriteInt32(PricePerMeter);
            p.WriteInt32(RedirectGridX);
            p.WriteInt32(RedirectGridY);
            p.WriteUInt8((byte)RegionFlagsExtended.Count);
            foreach(ulong d in RegionFlagsExtended)
            {
                p.WriteUInt64(d);
            }
        }
    }
}
