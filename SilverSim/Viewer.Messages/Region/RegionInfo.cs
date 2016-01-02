// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.Region
{
    [UDPMessage(MessageType.RegionInfo)]
    [Reliable]
    [Trusted]
    public class RegionInfo : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public string SimName = string.Empty;
        public UInt32 EstateID;
        public UInt32 ParentEstateID;
        public RegionOptionFlags RegionFlags;
        public RegionAccess SimAccess;
        public UInt32 MaxAgents;
        public double BillableFactor;
        public double ObjectBonusFactor;
        public double WaterHeight;
        public double TerrainRaiseLimit;
        public double TerrainLowerLimit;
        public Int32 PricePerMeter;
        public Int32 RedirectGridX;
        public Int32 RedirectGridY;
        public bool UseEstateSun;
        public double SunHour;
        public string ProductSKU = string.Empty;
        public string ProductName = string.Empty;
        public UInt32 HardMaxAgents;
        public UInt32 HardMaxObjects;

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
