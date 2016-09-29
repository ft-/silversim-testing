// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using WindLightSettings = SilverSim.Scene.Types.WindLight.EnvironmentSettings;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage : ISimulationDataEnvSettingsStorageInterface
    {
        readonly RwLockedDictionary<UUID, byte[]> m_EnvSettingsData = new RwLockedDictionary<UUID, byte[]>();

        bool ISimulationDataEnvSettingsStorageInterface.TryGetValue(UUID regionID, out WindLightSettings settings)
        {
            byte[] data;
            if(m_EnvSettingsData.TryGetValue(regionID, out data))
            { 
                using (MemoryStream ms = new MemoryStream(data))
                {
                    settings = WindLightSettings.Deserialize(ms);
                    return true;
                }
            }
            settings = null;
            return false;
        }

        /* setting value to null will delete the entry */
        WindLightSettings ISimulationDataEnvSettingsStorageInterface.this[UUID regionID]
        {
            get
            {
                WindLightSettings settings;
                if (!EnvironmentSettings.TryGetValue(regionID, out settings))
                {
                    throw new KeyNotFoundException();
                }
                return settings;
            }
            set
            {
                if(value == null)
                {
                    m_EnvSettingsData.Remove(regionID);
                }

                else
                {
                    using(MemoryStream ms = new MemoryStream())
                    {
                        value.Serialize(ms, regionID);
                        m_EnvSettingsData[regionID] = ms.ToArray();
                    }
                }
            }
        }

        bool ISimulationDataEnvSettingsStorageInterface.Remove(UUID regionID)
        {
            return m_EnvSettingsData.Remove(regionID);
        }
    }
}
