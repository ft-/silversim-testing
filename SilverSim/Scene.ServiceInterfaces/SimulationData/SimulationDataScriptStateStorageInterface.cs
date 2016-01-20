// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataScriptStateStorageInterface
    {
        protected SimulationDataScriptStateStorageInterface()
        {

        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract byte[] this[UUID regionID, UUID primID, UUID itemID] { get; set; }

        public abstract bool TryGetValue(UUID regionID, UUID primID, UUID itemID, out byte[] state);

        public abstract bool Remove(UUID regionID, UUID primID, UUID itemID);
    }
}
