// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public interface ISimulationDataEnvControllerStorageInterface
    {
        /* setting value to null will delete the entry */
        byte[] this[UUID regionID]
        {
            get;
            set;
        }

        bool TryGetValue(UUID regionID, out byte[] settings);

        bool Remove(UUID regionID);
    }
}
