// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public interface ISimulationDataObjectStorageInterface
    {
        /* load all objects of region */
        List<ObjectGroup> this[UUID regionID]
        {
            get;
        }

        List<UUID> ObjectsInRegion(UUID key);

        List<UUID> PrimitivesInRegion(UUID key);
    }
}
