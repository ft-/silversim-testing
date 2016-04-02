// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Parcel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public interface ISimulationDataParcelStorageInterface
    {
        ISimulationDataParcelAccessListStorageInterface WhiteList
        {
            get;
        }

        ISimulationDataParcelAccessListStorageInterface BlackList
        {
            get;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        ParcelInfo this[UUID regionID, UUID parcelID]
        {
            get;
        }
        List<UUID> ParcelsInRegion(UUID key);

        void Store(UUID regionID, ParcelInfo parcel);

        bool Remove(UUID regionID, UUID parcelID);
    }
}
