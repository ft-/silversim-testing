// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Scene;
using SilverSim.Types;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataRegionSettingsStorageInterface
    {
        public SimulationDataRegionSettingsStorageInterface()
        {

        }

        public abstract RegionSettings this[UUID regionID] { get; set; }

        public abstract bool TryGetValue(UUID regionID, out RegionSettings settings);

        public abstract bool ContainsKey(UUID regionID);

        public abstract bool Remove(UUID regionID);
    }
}
