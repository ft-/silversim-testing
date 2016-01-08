// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataLightShareStorageInterface
    {
        protected SimulationDataLightShareStorageInterface()
        {

        }

        public abstract bool TryGetValue(
            UUID regionID,
            out EnvironmentController.WindlightSkyData skyData,
            out EnvironmentController.WindlightWaterData waterData);

        public abstract void Store(UUID regionID,
            EnvironmentController.WindlightSkyData skyData,
            EnvironmentController.WindlightWaterData waterData);

        public abstract bool Remove(UUID regionID);
    }
}
