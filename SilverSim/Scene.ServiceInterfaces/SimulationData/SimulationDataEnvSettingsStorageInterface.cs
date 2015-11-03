// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.WindLight;
using SilverSim.Types;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataEnvSettingsStorageInterface
    {
        protected SimulationDataEnvSettingsStorageInterface()
        {
        }

        /* setting value to null will delete the entry */
        public abstract EnvironmentSettings this[UUID regionID]
        {
            get;
            set;
        }
    }
}
