// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public interface ISimulationDataScriptStateStorageInterface
    {
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        byte[] this[UUID regionID, UUID primID, UUID itemID] { get; set; }

        bool TryGetValue(UUID regionID, UUID primID, UUID itemID, out byte[] state);

        bool Remove(UUID regionID, UUID primID, UUID itemID);
    }
}
