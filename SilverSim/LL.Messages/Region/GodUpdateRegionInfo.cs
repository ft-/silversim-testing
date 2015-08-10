﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.Region
{
    [UDPMessage(MessageType.GodUpdateRegionInfo)]
    [Reliable]
    [NotTrusted]
    public class GodUpdateRegionInfo : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public string SimName = string.Empty;
        public UInt32 EstateID = 0;
        public UInt32 ParentEstateID = 0;
        public UInt32 RegionFlags = 0;
        public double BillableFactor = 0;
        public Int32 PricePerMeter = 0;
        public Int32 RedirectGridX = 0;
        public Int32 RedirectGridY = 0;

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
