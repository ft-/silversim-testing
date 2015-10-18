// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

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

        public GodUpdateRegionInfo()
        {

        }

        public static Message Decode(UDPPacket p)
        {
            GodUpdateRegionInfo m = new GodUpdateRegionInfo();
            m.AgentID = p.ReadUUID();
            m.SessionID = p.ReadUUID();
            m.SimName = p.ReadStringLen8();
            m.EstateID = p.ReadUInt32();
            m.ParentEstateID = p.ReadUInt32();
            m.RegionFlags = p.ReadUInt32();
            m.BillableFactor = p.ReadFloat();
            m.PricePerMeter = p.ReadInt32();
            m.RedirectGridX = p.ReadInt32();
            m.RedirectGridY = p.ReadInt32();

            return m;
        }
    }
}
