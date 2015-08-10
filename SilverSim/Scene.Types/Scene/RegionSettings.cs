// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;

namespace SilverSim.Scene.Types.Scene
{
    public class RegionSettings
    {
        public static readonly UUID DEFAULT_TERRAIN_TEXTURE_1 = new UUID("b8d3965a-ad78-bf43-699b-bff8eca6c975");
        public static readonly UUID DEFAULT_TERRAIN_TEXTURE_2 = new UUID("abb783e6-3e93-26c0-248a-247666855da3");
        public static readonly UUID DEFAULT_TERRAIN_TEXTURE_3 = new UUID("179cdabd-398a-9b6b-1391-4dc333ba321f");
        public static readonly UUID DEFAULT_TERRAIN_TEXTURE_4 = new UUID("beb169c7-11ea-fff2-efe5-0f24dc881df2");

        public bool BlockTerraform = false;
        public bool BlockFly = false;
        public bool AllowDamage = false;
        public bool RestrictPushing = false;
        public bool AllowLandResell = false;
        public bool AllowLandJoinDivide = false;
        public bool BlockShowInSearch = false;
        public int AgentLimit = 40;
        public double ObjectBonus = 1.0;
        public int Maturity = 0;
        public bool DisableScripts = false;
        public bool DisableCollisions = false;
        public bool DisablePhysics = false;

        private UUID m_TerrainTexture1 = DEFAULT_TERRAIN_TEXTURE_1;
        public UUID TerrainTexture1
        {
            get
            {
                return m_TerrainTexture1;
            }
            set
            {
                if(value == UUID.Zero)
                {
                    m_TerrainTexture1 = DEFAULT_TERRAIN_TEXTURE_1;
                }
                else
                {
                    m_TerrainTexture1 = value;
                }
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
                if (value == UUID.Zero)
                {
                    m_TerrainTexture2 = DEFAULT_TERRAIN_TEXTURE_2;
                }
                else
                {
                    m_TerrainTexture2 = value;
                }
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
                if (value == UUID.Zero)
                {
                    m_TerrainTexture3 = DEFAULT_TERRAIN_TEXTURE_3;
                }
                else
                {
                    m_TerrainTexture3 = value;
                }
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
                if (value == UUID.Zero)
                {
                    m_TerrainTexture4 = DEFAULT_TERRAIN_TEXTURE_4;
                }
                else
                {
                    m_TerrainTexture4 = value;
                }
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

        public bool Sandbox = false;

        public UUID TelehubObject = UUID.Zero;
    }
}
