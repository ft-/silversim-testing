// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using EnvController = SilverSim.Scene.Types.SceneEnvironment.EnvironmentController;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage : ISimulationDataLightShareStorageInterface
    {
        readonly RwLockedDictionary<UUID, KeyValuePair<EnvController.WindlightSkyData, EnvController.WindlightWaterData>> m_LightShareData = new RwLockedDictionary<UUID, KeyValuePair<EnvController.WindlightSkyData, EnvController.WindlightWaterData>>();

        bool ISimulationDataLightShareStorageInterface.TryGetValue(UUID regionID, out EnvController.WindlightSkyData skyData, out EnvController.WindlightWaterData waterData)
        {
            KeyValuePair<EnvController.WindlightSkyData, EnvController.WindlightWaterData> kvp;
            if(m_LightShareData.TryGetValue(regionID, out kvp))
            {
                skyData = kvp.Key;
                waterData = kvp.Value;
                return true;
            }
            else
            {
                skyData = EnvController.WindlightSkyData.Defaults;
                waterData = EnvController.WindlightWaterData.Defaults;
                return false;
            }
        }

        void ISimulationDataLightShareStorageInterface.Store(UUID regionID, EnvController.WindlightSkyData skyData, EnvController.WindlightWaterData waterData)
        {
            m_LightShareData[regionID] = new KeyValuePair<EnvController.WindlightSkyData, EnvController.WindlightWaterData>(skyData, waterData);
        }

        bool ISimulationDataLightShareStorageInterface.Remove(UUID regionID)
        {
            return m_LightShareData.Remove(regionID);
        }
    }
}
