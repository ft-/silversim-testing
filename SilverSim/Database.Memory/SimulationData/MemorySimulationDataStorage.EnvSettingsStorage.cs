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

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.IO;
using WindLightSettings = SilverSim.Scene.Types.WindLight.EnvironmentSettings;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage : ISimulationDataEnvSettingsStorageInterface
    {
        private readonly RwLockedDictionary<UUID, byte[]> m_EnvSettingsData = new RwLockedDictionary<UUID, byte[]>();

        bool ISimulationDataEnvSettingsStorageInterface.TryGetValue(UUID regionID, out WindLightSettings settings)
        {
            byte[] data;
            if(m_EnvSettingsData.TryGetValue(regionID, out data))
            {
                using (var ms = new MemoryStream(data))
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
                    using(var ms = new MemoryStream())
                    {
                        value.Serialize(ms, regionID);
                        m_EnvSettingsData[regionID] = ms.ToArray();
                    }
                }
            }
        }

        bool ISimulationDataEnvSettingsStorageInterface.Remove(UUID regionID) =>
            m_EnvSettingsData.Remove(regionID);
    }
}
