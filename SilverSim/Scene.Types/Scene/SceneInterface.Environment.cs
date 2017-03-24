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

using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Scene.Types.WindLight;
using SilverSim.Types;
using SilverSim.Types.Estate;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public TerrainController Terrain;
        public EnvironmentController Environment;

        public struct LocationInfo
        {
            public double GroundHeight;
            public double WaterHeight;
        }

        public class LocationInfoProvider
        {
            TerrainController m_TerrainController;
            RegionSettings m_RegionSettings;

            internal LocationInfoProvider(TerrainController terrain, RegionSettings regionSettings)
            {

            }

            public LocationInfo At(Vector3 pos)
            {
                LocationInfo locInfo = new LocationInfo();
                locInfo.WaterHeight = m_RegionSettings.WaterHeight;
                locInfo.GroundHeight = m_TerrainController[pos];

                return locInfo;
            }
        }

        public LocationInfoProvider GetLocationInfoProvider()
        {
            return new LocationInfoProvider(Terrain, RegionSettings);
        }

        public void UpdateEnvironmentSettings()
        {
            Terrain.LowerLimit = (float)RegionSettings.TerrainLowerLimit;
            Terrain.RaiseLimit = (float)RegionSettings.TerrainRaiseLimit;
            if(RegionSettings.UseEstateSun)
            {
                uint estateID;
                EstateInfo estate;
                if(EstateService.RegionMap.TryGetValue(ID, out estateID) &&
                    EstateService.TryGetValue(estateID, out estate))
                {
                    Environment.FixedSunPosition = estate.SunPosition;
                    Environment.IsSunFixed = (estate.Flags & RegionOptionFlags.SunFixed) != 0;
                }
            }
            else
            {
                Environment.FixedSunPosition = RegionSettings.SunPosition;
                Environment.IsSunFixed = RegionSettings.IsSunFixed;
            }
        }

        public abstract void TriggerStoreOfEnvironmentSettings();

        EnvironmentSettings m_EnvironmentSettings;

        public EnvironmentSettings EnvironmentSettings
        {
            get
            {
                EnvironmentSettings envSettings = m_EnvironmentSettings;
                if (envSettings == null)
                {
                    return null;
                }
                return new EnvironmentSettings(envSettings);
            }
            set
            {
                m_EnvironmentSettings = (null != value) ?
                    new EnvironmentSettings(value) :
                    null;
            }
        }

    }
}
