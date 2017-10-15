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

        public override void Serialize(UDPPacket p)
        {
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

        public static Message Decode(UDPPacket p)
        {
            var m = new RegionInfo
            {
                AgentID = p.ReadUUID(),
                SessionID = p.ReadUUID(),
                SimName = p.ReadStringLen8(),
                EstateID = p.ReadUInt32(),
                ParentEstateID = p.ReadUInt32(),
                RegionFlags = (RegionOptionFlags)p.ReadUInt32(),
                SimAccess = (RegionAccess)p.ReadUInt8(),
                BillableFactor = p.ReadFloat(),
                ObjectBonusFactor = p.ReadFloat(),
                WaterHeight = p.ReadFloat(),
                TerrainRaiseLimit = p.ReadFloat(),
                TerrainLowerLimit = p.ReadFloat(),
                PricePerMeter = p.ReadInt32(),
                RedirectGridX = p.ReadInt32(),
                RedirectGridY = p.ReadInt32(),
                UseEstateSun = p.ReadBoolean(),
                SunHour = p.ReadFloat(),
                ProductSKU = p.ReadStringLen8(),
                ProductName = p.ReadStringLen8(),
                MaxAgents = p.ReadUInt32(),
                HardMaxAgents = p.ReadUInt32(),
                HardMaxObjects = p.ReadUInt32()
            };
            uint n = p.ReadUInt8();
            for(uint i = 0; i < n; ++i)
            {
                m.RegionFlagsExtended.Add(p.ReadUInt64());
            }
            return m;
        }
    }
}
