// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Types;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public interface ISimulationDataLightShareStorageInterface
    {
        bool TryGetValue(
            UUID regionID,
            out EnvironmentController.WindlightSkyData skyData,
            out EnvironmentController.WindlightWaterData waterData);

        void Store(UUID regionID,
            EnvironmentController.WindlightSkyData skyData,
            EnvironmentController.WindlightWaterData waterData);

        bool Remove(UUID regionID);
    }
}
