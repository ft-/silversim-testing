﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Region
{
    [UDPMessage(MessageType.RegionInfo)]
    [Reliable]
    [Trusted]
    public class RegionInfo : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public string SimName = string.Empty;
        public UInt32 EstateID = 0;
        public UInt32 ParentEstateID = 0;
        public RegionFlags RegionFlags = 0;
        public RegionAccess SimAccess = 0;
        public UInt32 MaxAgents = 0;
        public double BillableFactor = 0;
        public double ObjectBonusFactor = 0;
        public double WaterHeight = 0;
        public double TerrainRaiseLimit = 0;
        public double TerrainLowerLimit = 0;
        public Int32 PricePerMeter = 0;
        public Int32 RedirectGridX = 0;
        public Int32 RedirectGridY = 0;
        public bool UseEstateSun = false;
        public double SunHour = 0;
        public string ProductSKU = string.Empty;
        public string ProductName = string.Empty;
        public UInt32 HardMaxAgents = 0;
        public UInt32 HardMaxObjects = 0;

        public List<UInt64> RegionFlagsExtended = new List<UInt64>();

        public RegionInfo()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUUID(AgentID);
            p.WriteUUID(SessionID);
            p.WriteStringLen8(SimName);
            p.WriteUInt32(EstateID);
            p.WriteUInt32(ParentEstateID);
            p.WriteUInt32((uint)RegionFlags);
            p.WriteUInt8((byte)SimAccess);
            if (MaxAgents > 255)
            {
                p.WriteUInt8(255);
            }
            else
            {
                p.WriteUInt8((byte)MaxAgents);
            }
            p.WriteFloat((float)BillableFactor);
            p.WriteFloat((float)ObjectBonusFactor);
            p.WriteFloat((float)WaterHeight);
            p.WriteFloat((float)TerrainRaiseLimit);
            p.WriteFloat((float)TerrainLowerLimit);
            p.WriteInt32(PricePerMeter);
            p.WriteInt32(RedirectGridX);
            p.WriteInt32(RedirectGridY);
            p.WriteBoolean(UseEstateSun);
            p.WriteFloat((float)SunHour);
            p.WriteStringLen8(ProductSKU);
            p.WriteStringLen8(ProductName);
            p.WriteUInt32(MaxAgents);
            p.WriteUInt32(HardMaxAgents);
            p.WriteUInt32(HardMaxObjects);
            p.WriteUInt8((byte)RegionFlagsExtended.Count);
            foreach(UInt64 v in RegionFlagsExtended)
            {
                p.WriteUInt64(v);
            }
        }
    }
}
