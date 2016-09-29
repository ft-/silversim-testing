// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage : ISimulationDataSpawnPointStorageInterface
    {
        readonly RwLockedDictionary<UUID, RwLockedList<Vector3>> m_SpawnPointData = new RwLockedDictionary<UUID, RwLockedList<Vector3>>();

        List<Vector3> ISimulationDataSpawnPointStorageInterface.this[UUID regionID]
        {
            get
            {
                RwLockedList<Vector3> data;
                return (m_SpawnPointData.TryGetValue(regionID, out data)) ?
                    new List<Vector3>(data) :
                    new List<Vector3>();
            }
            set
            {
                m_SpawnPointData[regionID] = new RwLockedList<Vector3>(value);
            }
        }

        bool ISimulationDataSpawnPointStorageInterface.Remove(UUID regionID)
        {
            return m_SpawnPointData.Remove(regionID);
        }
    }
}
