// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataTerrainStorageInterface
    {
        public abstract LayerPatch this[UUID regionID, uint ExtendedPatchID]
        {
            get;
            set;
        }
        public abstract List<LayerPatch> this[UUID key]
        {
            get;
        }

        public class TerrainStorageFailedException : Exception
        {
            public TerrainStorageFailedException(UUID regionID, uint extendedPatchID)
                :base(string.Format("Failed to store terrain at {1} for region {0}", regionID, extendedPatchID))
            {
                
            }
        }
    }
}
