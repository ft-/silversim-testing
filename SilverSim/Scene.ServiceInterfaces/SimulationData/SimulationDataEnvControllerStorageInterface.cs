// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataEnvControllerStorageInterface
    {
        /* setting value to null will delete the entry */
        public abstract byte[] this[UUID regionID]
        {
            get;
            set;
        }

        public abstract bool TryGetValue(UUID regionID, out byte[] settings);

        public abstract bool Remove(UUID regionID);
    }
}
