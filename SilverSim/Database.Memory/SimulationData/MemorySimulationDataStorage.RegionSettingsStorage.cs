// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage : ISimulationDataRegionSettingsStorageInterface
    {
        readonly RwLockedDictionary<UUID, RegionSettings> m_RegionSettingsData = new RwLockedDictionary<UUID, RegionSettings>();

        RegionSettings ISimulationDataRegionSettingsStorageInterface.this[UUID regionID]
        {
            get
            {
                RegionSettings settings;
                if (!RegionSettings.TryGetValue(regionID, out settings))
                {
                    throw new KeyNotFoundException();
                }
                return settings;
            }
            set
            {
                m_RegionSettingsData[regionID] = new RegionSettings(value);
            }
        }

        bool ISimulationDataRegionSettingsStorageInterface.TryGetValue(UUID regionID, out RegionSettings settings)
        {
            if(m_RegionSettingsData.TryGetValue(regionID, out settings))
            {
                settings = new RegionSettings(settings);
                return true;
            }
            settings = null;
            return false;
        }

        bool ISimulationDataRegionSettingsStorageInterface.ContainsKey(UUID regionID)
        {
            return m_RegionSettingsData.ContainsKey(regionID);
        }

        bool ISimulationDataRegionSettingsStorageInterface.Remove(UUID regionID)
        {
            return m_RegionSettingsData.Remove(regionID);
        }
    }
}
