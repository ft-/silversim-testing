// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public interface ISimulationDataSpawnPointStorageInterface
    {
        List<Vector3> this[UUID regionID] { get; set; }
        bool Remove(UUID regionID);
    }
}
