// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Threading;
using SilverSim.Types;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage : ISimulationDataEnvControllerStorageInterface
    {
        readonly RwLockedDictionary<UUID, byte[]> m_EnvControllerData = new RwLockedDictionary<UUID, byte[]>();

        byte[] ISimulationDataEnvControllerStorageInterface.this[UUID regionID]
        {
            get
            {
                return m_EnvControllerData[regionID];
            }

            set
            {
                if (value != null)
                {
                    m_EnvControllerData[regionID] = value;
                }
                else
                {
                    m_EnvControllerData.Remove(regionID);
                }
            }
        }

        bool ISimulationDataEnvControllerStorageInterface.Remove(UUID regionID)
        {
            return m_EnvControllerData.Remove(regionID);
        }

        bool ISimulationDataEnvControllerStorageInterface.TryGetValue(UUID regionID, out byte[] settings)
        {
            return m_EnvControllerData.TryGetValue(regionID, out settings);
        }
    }
}
