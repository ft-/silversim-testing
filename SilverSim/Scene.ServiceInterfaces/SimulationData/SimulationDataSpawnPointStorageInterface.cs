using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataSpawnPointStorageInterface
    {
        public SimulationDataSpawnPointStorageInterface()
        {

        }

        public abstract List<Vector3> this[UUID regionID] { get; set; }
        public abstract bool Remove(UUID regionID);
    }
}
