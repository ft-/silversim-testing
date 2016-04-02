// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.WindLight;
using SilverSim.Types;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public interface ISimulationDataEnvSettingsStorageInterface
    {
        /* setting value to null will delete the entry */
        EnvironmentSettings this[UUID regionID]
        {
            get;
            set;
        }

        bool TryGetValue(UUID regionID, out EnvironmentSettings settings);

        bool Remove(UUID regionID);
    }
}
