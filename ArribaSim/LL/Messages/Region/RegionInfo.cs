using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.LL.Messages.Region
{
    public class RegionInfo : Message
    {
        public UUID AgentID = UUID.Zero;
        public UUID SessionID = UUID.Zero;

        public string SimName = string.Empty;
        public UInt32 EstateID = 0;
        public UInt32 ParentEstateID = 0;
        public UInt32 RegionFlags = 0;
        public byte SimAccess = 0;
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

        public RegionInfo()
        {

        }

        public virtual new MessageType Number
        {
            get
            {
                return MessageType.RegionInfo;
            }
        }

        public new void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteStringLen8(SimName);
            p.WriteUInt32(EstateID);
            p.WriteUInt32(ParentEstateID);
            p.WriteUInt32(RegionFlags);
            p.WriteUInt8(SimAccess);
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
        }
    }
}
