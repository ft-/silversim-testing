// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Scene.Types.WindLight;
using SilverSim.Types.Estate;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public TerrainController Terrain;
        public EnvironmentController Environment;

        public void UpdateEnvironmentSettings()
        {
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
