// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataObjectStorageInterface
    {
        #region Constructor
        protected SimulationDataObjectStorageInterface()
        {
        }
        #endregion

        /* load all objects of region */
        public abstract List<ObjectGroup> this[UUID regionID]
        {
            get;
        }

        public abstract List<UUID> ObjectsInRegion(UUID key);

        public abstract List<UUID> PrimitivesInRegion(UUID key);
    }
}
