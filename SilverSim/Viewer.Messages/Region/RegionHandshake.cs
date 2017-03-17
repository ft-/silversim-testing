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
    [UDPMessage(MessageType.RegionHandshake)]
    [Reliable]
    [Trusted]
    public class RegionHandshake : Message
    {
        public RegionOptionFlags RegionFlags;
        public RegionAccess SimAccess;
        public string SimName = string.Empty;
        public UUID SimOwner = UUID.Zero;
        public bool IsEstateManager;
        public double WaterHeight;
        public double BillableFactor;
        public UUID CacheID = UUID.Zero;
        public UUID TerrainBase0 = UUID.Zero;
        public UUID TerrainBase1 = UUID.Zero;
        public UUID TerrainBase2 = UUID.Zero;
        public UUID TerrainBase3 = UUID.Zero;
        public UUID TerrainDetail0 = UUID.Zero;
        public UUID TerrainDetail1 = UUID.Zero;
        public UUID TerrainDetail2 = UUID.Zero;
        public UUID TerrainDetail3 = UUID.Zero;
        public double TerrainStartHeight00;
        public double TerrainStartHeight01;
        public double TerrainStartHeight10;
        public double TerrainStartHeight11;
        public double TerrainHeightRange00;
        public double TerrainHeightRange01;
        public double TerrainHeightRange10;
        public double TerrainHeightRange11;

        public UUID RegionID = UUID.Zero;

        public Int32 CPUClassID;
        public Int32 CPURatio;
        public string ColoName = string.Empty;
        public string ProductSKU = string.Empty;
        public string ProductName = string.Empty;

        public struct RegionExtDataEntry
        {
            public UInt64 RegionFlagsExtended;
            public UInt64 RegionProtocols;
        }

        public readonly List<RegionExtDataEntry> RegionExtData = new List<RegionExtDataEntry>();

        public RegionHandshake()
        {

        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteUInt32((uint)RegionFlags);
            p.WriteUInt8((byte)SimAccess);
            p.WriteStringLen8(SimName);
            p.WriteUUID(SimOwner);
            p.WriteBoolean(IsEstateManager);
            p.WriteFloat((float)WaterHeight);
            p.WriteFloat((float)BillableFactor);
            p.WriteUUID(CacheID);
            p.WriteUUID(TerrainBase0);
            p.WriteUUID(TerrainBase1);
            p.WriteUUID(TerrainBase2);
            p.WriteUUID(TerrainBase3);
            p.WriteUUID(TerrainDetail0);
            p.WriteUUID(TerrainDetail1);
            p.WriteUUID(TerrainDetail2);
            p.WriteUUID(TerrainDetail3);
            p.WriteFloat((float)TerrainStartHeight00);
            p.WriteFloat((float)TerrainStartHeight01);
            p.WriteFloat((float)TerrainStartHeight10);
            p.WriteFloat((float)TerrainStartHeight11);
            p.WriteFloat((float)TerrainHeightRange00);
            p.WriteFloat((float)TerrainHeightRange01);
            p.WriteFloat((float)TerrainHeightRange10);
            p.WriteFloat((float)TerrainHeightRange11);
            p.WriteUUID(RegionID);
            p.WriteInt32(CPUClassID);
            p.WriteInt32(CPURatio);
            p.WriteStringLen8(ColoName);
            p.WriteStringLen8(ProductSKU);
            p.WriteStringLen8(ProductName);
            p.WriteUInt8((byte)RegionExtData.Count);
            foreach(RegionExtDataEntry e in RegionExtData)
            {
                p.WriteUInt64(e.RegionFlagsExtended);
                p.WriteUInt64(e.RegionProtocols);
            }
        }

        public static Message Decode(UDPPacket p)
        {
            RegionHandshake m = new RegionHandshake();
            m.RegionFlags = (RegionOptionFlags)p.ReadUInt32();
            m.SimAccess = (RegionAccess)p.ReadUInt8();
            m.SimName = p.ReadStringLen8();
            m.SimOwner = p.ReadUUID();
            m.IsEstateManager = p.ReadBoolean();
            m.WaterHeight = p.ReadFloat();
            m.BillableFactor = p.ReadFloat();
            m.CacheID = p.ReadUUID();
            m.TerrainBase0 = p.ReadUUID();
            m.TerrainBase1 = p.ReadUUID();
            m.TerrainBase2 = p.ReadUUID();
            m.TerrainBase3 = p.ReadUUID();
            m.TerrainDetail0 = p.ReadUUID();
            m.TerrainDetail1 = p.ReadUUID();
            m.TerrainDetail2 = p.ReadUUID();
            m.TerrainDetail3 = p.ReadUUID();
            m.TerrainStartHeight00 = p.ReadFloat();
            m.TerrainStartHeight01 = p.ReadFloat();
            m.TerrainStartHeight10 = p.ReadFloat();
            m.TerrainStartHeight11 = p.ReadFloat();
            m.TerrainHeightRange00 = p.ReadFloat();
            m.TerrainHeightRange01 = p.ReadFloat();
            m.TerrainHeightRange10 = p.ReadFloat();
            m.TerrainHeightRange11 = p.ReadFloat();
            m.RegionID = p.ReadUUID();
            m.CPUClassID = p.ReadInt32();
            m.CPURatio = p.ReadInt32();
            m.ColoName = p.ReadStringLen8();
            m.ProductSKU = p.ReadStringLen8();
            m.ProductName = p.ReadStringLen8();
            uint n = p.ReadUInt8();
            while(n-- != 0)
            {
                RegionExtDataEntry d = new RegionExtDataEntry();
                d.RegionFlagsExtended = p.ReadUInt64();
                d.RegionProtocols = p.ReadUInt64();
                m.RegionExtData.Add(d);
            }
            return m;
        }
    }
}
