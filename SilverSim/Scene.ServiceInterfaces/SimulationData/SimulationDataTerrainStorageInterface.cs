// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataTerrainStorageInterface
    {
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract LayerPatch this[UUID regionID, uint extendedPatchID]
        {
            get;
            set;
        }
        public abstract List<LayerPatch> this[UUID key]
        {
            get;
        }

        [Serializable]
        public class TerrainStorageFailedException : Exception
        {
            public TerrainStorageFailedException(UUID regionID, uint extendedPatchID)
                :base(string.Format("Failed to store terrain at {1} for region {0}", regionID, extendedPatchID))
            {
                
            }

            public TerrainStorageFailedException()
            {

            }

            public TerrainStorageFailedException(string message)
                : base(message)
            {

            }

            protected TerrainStorageFailedException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public TerrainStorageFailedException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }
    }
}
