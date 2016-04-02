// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Viewer.Messages.LayerData;
using System.Collections.Generic;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public interface ISimulationDataTerrainStorageInterface
    {
        List<LayerPatch> this[UUID key]
        {
            get;
        }
    }
}
