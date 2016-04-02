// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Scene;
using SilverSim.Types;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public interface ISimulationDataRegionSettingsStorageInterface
    {
        RegionSettings this[UUID regionID] { get; set; }

        bool TryGetValue(UUID regionID, out RegionSettings settings);

        bool ContainsKey(UUID regionID);

        bool Remove(UUID regionID);
    }
}
