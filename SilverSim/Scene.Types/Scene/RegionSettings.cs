// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Types.Estate;

namespace SilverSim.Scene.Types.Scene
{
    public class RegionSettings
    {
        public static readonly UUID DEFAULT_TERRAIN_TEXTURE_1 = new UUID("b8d3965a-ad78-bf43-699b-bff8eca6c975");
        public static readonly UUID DEFAULT_TERRAIN_TEXTURE_2 = new UUID("abb783e6-3e93-26c0-248a-247666855da3");
        public static readonly UUID DEFAULT_TERRAIN_TEXTURE_3 = new UUID("179cdabd-398a-9b6b-1391-4dc333ba321f");
        public static readonly UUID DEFAULT_TERRAIN_TEXTURE_4 = new UUID("beb169c7-11ea-fff2-efe5-0f24dc881df2");

        public bool BlockTerraform;
        public bool BlockFly;
        public bool AllowDamage;
        public bool RestrictPushing;
        public bool AllowLandResell;
        public bool AllowLandJoinDivide;
        public bool BlockShowInSearch;
        public int AgentLimit = 40;
        public double ObjectBonus = 1.0;
        public bool DisableScripts;
        public bool DisableCollisions;
        public bool DisablePhysics;
        public bool BlockFlyOver;
        public double SunPosition;
        public bool IsSunFixed;
        public bool UseEstateSun;

        private UUID m_TerrainTexture1 = DEFAULT_TERRAIN_TEXTURE_1;
        public UUID TerrainTexture1
        {
            get
            {
                return m_TerrainTexture1;
            }
            set
            {
                m_TerrainTexture1 = (value == UUID.Zero) ?
                    DEFAULT_TERRAIN_TEXTURE_1 :
                    value;
            }
        }

        private UUID m_TerrainTexture2 = DEFAULT_TERRAIN_TEXTURE_2;
        public UUID TerrainTexture2
        {
            get
            {
                return m_TerrainTexture2;
            }
            set
            {
                m_TerrainTexture2 = (value == UUID.Zero) ?
                    DEFAULT_TERRAIN_TEXTURE_2 :
                    value;
            }
        }

        private UUID m_TerrainTexture3 = DEFAULT_TERRAIN_TEXTURE_3;
        public UUID TerrainTexture3
        {
            get
            {
                return m_TerrainTexture3;
            }
            set
            {
                m_TerrainTexture3 = (value == UUID.Zero) ?
                    DEFAULT_TERRAIN_TEXTURE_3 :
                    value;
            }
        }

        private UUID m_TerrainTexture4 = DEFAULT_TERRAIN_TEXTURE_4;
        public UUID TerrainTexture4
        {
            get
            {
                return m_TerrainTexture4;
            }
            set
            {
                m_TerrainTexture4 = (value == UUID.Zero) ?
                    DEFAULT_TERRAIN_TEXTURE_4 :
                    value;
            }
        }

        public double Elevation1NW = 10;
        public double Elevation2NW = 60;
        public double Elevation1NE = 10;
        public double Elevation2NE = 60;
        public double Elevation1SE = 10;
        public double Elevation2SE = 60;
        public double Elevation1SW = 10;
        public double Elevation2SW = 60;

        public double WaterHeight = 20;
        public double TerrainRaiseLimit = 100;
        public double TerrainLowerLimit = -100;

        public bool Sandbox;

        public UUID TelehubObject = UUID.Zero;

        public RegionOptionFlags AsFlags
        {
            get
            {
                RegionOptionFlags flags = 0;
                if(IsSunFixed)
                {
                    flags |= RegionOptionFlags.SunFixed;
                }
                if (AllowDamage)
                {
                    flags |= RegionOptionFlags.AllowDamage;
                }
                if (BlockTerraform)
                {
                    flags |= RegionOptionFlags.BlockTerraform;
                }
                if (!AllowLandResell)
                {
                    flags |= RegionOptionFlags.BlockLandResell;
                }
                if(BlockFlyOver)
                {
                    flags |= RegionOptionFlags.BlockFlyOver;
                }
                if (DisableCollisions)
                {
                    flags |= RegionOptionFlags.DisableAgentCollisions;
                }
                if (DisableScripts)
                {
                    flags |= RegionOptionFlags.DisableScripts;
                }
                if (DisablePhysics)
                {
                    flags |= RegionOptionFlags.DisablePhysics;
                }
                if(BlockFly)
                {
                    flags |= RegionOptionFlags.BlockFly;
                }
                if(RestrictPushing)
                {
                    flags |= RegionOptionFlags.RestrictPushObject;
                }
                if(AllowLandJoinDivide)
                {
                    flags |= RegionOptionFlags.AllowParcelChanges;
                }
                if(BlockShowInSearch)
                {
                    flags |= RegionOptionFlags.BlockParcelSearch;
                }
                if(Sandbox)
                {
                    flags |= RegionOptionFlags.Sandbox;
                }

                return flags;
            }
        }
    }
}
