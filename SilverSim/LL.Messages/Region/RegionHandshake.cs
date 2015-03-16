/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
using SilverSim.Types.Estate;
using System;
using System.Collections.Generic;

namespace SilverSim.LL.Messages.Region
{
    public class RegionHandshake : Message
    {
        public RegionOptionFlags RegionFlags = RegionOptionFlags.None;
        public byte SimAccess = 0;
        public string SimName = string.Empty;
        public UUID SimOwner = UUID.Zero;
        public bool IsEstateManager = false;
        public double WaterHeight = 0f;
        public double BillableFactor = 0f;
        public UUID CacheID = UUID.Zero;
        public UUID TerrainBase0 = UUID.Zero;
        public UUID TerrainBase1 = UUID.Zero;
        public UUID TerrainBase2 = UUID.Zero;
        public UUID TerrainBase3 = UUID.Zero;
        public UUID TerrainDetail0 = UUID.Zero;
        public UUID TerrainDetail1 = UUID.Zero;
        public UUID TerrainDetail2 = UUID.Zero;
        public UUID TerrainDetail3 = UUID.Zero;
        public double TerrainStartHeight00 = 0f;
        public double TerrainStartHeight01 = 0f;
        public double TerrainStartHeight10 = 0f;
        public double TerrainStartHeight11 = 0f;
        public double TerrainHeightRange00 = 0f;
        public double TerrainHeightRange01 = 0f;
        public double TerrainHeightRange10 = 0f;
        public double TerrainHeightRange11 = 0f;

        public UUID RegionID = UUID.Zero;

        public Int32 CPUClassID = 0;
        public Int32 CPURatio = 0;
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

        public override MessageType Number
        {
            get
            {
                return MessageType.RegionHandshake;
            }
        }

        public override bool IsReliable
        {
            get
            {
                return true;
            }
        }

        public override void Serialize(UDPPacket p)
        {
            p.WriteMessageType(Number);
            p.WriteUInt32((uint)RegionFlags);
            p.WriteUInt8(SimAccess);
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
    }
}
