// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Scene;
using SilverSim.Types;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public interface ISimulationDataParcelAccessListStorageInterface : IParcelAccessList
    {
        bool RemoveAllFromRegion(UUID regionID);
    }
}
