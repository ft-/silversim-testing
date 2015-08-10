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
        public SimulationDataObjectStorageInterface()
        {
        }
        #endregion

        public abstract ObjectGroup this[UUID regionID, UUID objectID]
        {
            get;
        }

        public abstract List<UUID> ObjectsInRegion(UUID key);

        public abstract List<UUID> PrimitivesInRegion(UUID key);

        public abstract void DeleteObjectPart(UUID obj);
        public abstract void DeleteObjectGroup(UUID obj);

        public abstract void UpdateObjectGroup(ObjectGroup objgroup);
        public abstract void UpdateObjectPart(ObjectPart objpart);
        public abstract void UpdateObjectPartInventory(ObjectPart objpart);
    }
}
