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

namespace SilverSim.Scene.Types.Scene
{
    public class RegionSettings
    {
        public RegionSettings()
        {
        }

        public RegionSettings(RegionSettings src)
        {
            BlockTerraform = src.BlockTerraform;
            BlockFly = src.BlockFly;
            AllowDamage = src.AllowDamage;
            BlockDwell = src.BlockDwell;
            RestrictPushing = src.RestrictPushing;
            AllowLandResell = src.AllowLandResell;
            AllowLandJoinDivide = src.AllowLandJoinDivide;
            BlockShowInSearch = src.BlockShowInSearch;
            AgentLimit = src.AgentLimit;
            ObjectBonus = src.ObjectBonus;
            DisableScripts = src.DisableScripts;
            DisableCollisions = src.DisableCollisions;
            DisablePhysics = src.DisablePhysics;
            BlockFlyOver = src.BlockFlyOver;
            SunPosition = src.SunPosition;
            IsSunFixed = src.IsSunFixed;
            UseEstateSun = src.UseEstateSun;
            ResetHomeOnTeleport = src.ResetHomeOnTeleport;
            AllowLandmark = src.AllowLandmark;
            AllowDirectTeleport = src.AllowDirectTeleport;
            m_TerrainTexture1 = src.m_TerrainTexture1;
            m_TerrainTexture2 = src.m_TerrainTexture2;
            m_TerrainTexture3 = src.m_TerrainTexture3;
            m_TerrainTexture4 = src.m_TerrainTexture4;
            Elevation1NW = src.Elevation1NW;
            Elevation2NW = src.Elevation2NW;
            Elevation1NE = src.Elevation1NE;
            Elevation2NE = src.Elevation2NE;
            Elevation1SE = src.Elevation1SE;
            Elevation2SE = src.Elevation2SE;
            Elevation1SW = src.Elevation1SW;
            Elevation2SW = src.Elevation2SW;
            WaterHeight = src.WaterHeight;
            TerrainRaiseLimit = src.TerrainRaiseLimit;
            TerrainLowerLimit = src.TerrainLowerLimit;
            Sandbox = src.Sandbox;
            TelehubObject = src.TelehubObject;
            MaxBasePrims = src.MaxBasePrims;
        }

        public bool BlockTerraform;
        public bool BlockFly;
        public bool AllowDamage;
        public bool BlockDwell = true;
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
        public bool ResetHomeOnTeleport;
        public bool AllowLandmark;
        public bool AllowDirectTeleport;
        public int MaxBasePrims = 45000;

        private UUID m_TerrainTexture1 = TextureConstant.DefaultTerrainTexture1;
        public UUID TerrainTexture1
        {
            get { return m_TerrainTexture1; }

            set
            {
                m_TerrainTexture1 = (value == UUID.Zero) ?
                    TextureConstant.DefaultTerrainTexture1 :
                    value;
            }
        }

        private UUID m_TerrainTexture2 = TextureConstant.DefaultTerrainTexture2;
        public UUID TerrainTexture2
        {
            get { return m_TerrainTexture2; }

            set
            {
                m_TerrainTexture2 = (value == UUID.Zero) ?
                    TextureConstant.DefaultTerrainTexture2 :
                    value;
            }
        }

        private UUID m_TerrainTexture3 = TextureConstant.DefaultTerrainTexture3;
        public UUID TerrainTexture3
        {
            get { return m_TerrainTexture3; }

            set
            {
                m_TerrainTexture3 = (value == UUID.Zero) ?
                    TextureConstant.DefaultTerrainTexture3 :
                    value;
            }
        }

        private UUID m_TerrainTexture4 = TextureConstant.DefaultTerrainTexture4;
        public UUID TerrainTexture4
        {
            get { return m_TerrainTexture4; }

            set
            {
                m_TerrainTexture4 = (value == UUID.Zero) ?
                    TextureConstant.DefaultTerrainTexture4 :
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

        public int MaxTotalPrims => (int)(MaxBasePrims * ObjectBonus);

        public RegionOptionFlags AsFlags
        {
            get
            {
                RegionOptionFlags flags = 0;
                if (AllowDirectTeleport)
                {
                    flags |= RegionOptionFlags.AllowDirectTeleport;
                }
                if (AllowLandmark)
                {
                    flags |= RegionOptionFlags.AllowLandmark;
                }
                if (ResetHomeOnTeleport)
                {
                    flags |= RegionOptionFlags.ResetHomeOnTeleport;
                }
                if (IsSunFixed)
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
                if (BlockDwell)
                {
                    flags |= RegionOptionFlags.BlockDwell;
                }
                if (BlockFlyOver)
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
                if (BlockFly)
                {
                    flags |= RegionOptionFlags.BlockFly;
                }
                if (RestrictPushing)
                {
                    flags |= RegionOptionFlags.RestrictPushObject;
                }
                if (AllowLandJoinDivide)
                {
                    flags |= RegionOptionFlags.AllowParcelChanges;
                }
                if (BlockShowInSearch)
                {
                    flags |= RegionOptionFlags.BlockParcelSearch;
                }
                if (Sandbox)
                {
                    flags |= RegionOptionFlags.Sandbox;
                }
                if (IsSunFixed)
                {
                    flags |= RegionOptionFlags.SunFixed;
                }

                return flags;
            }
        }
    }
}
