// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Parcel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataParcelStorageInterface
    {
        #region Constructor
        protected SimulationDataParcelStorageInterface()
        {
        }
        #endregion

        public abstract SimulationDataParcelAccessListStorageInterface WhiteList
        {
            get;
        }

        public abstract SimulationDataParcelAccessListStorageInterface BlackList
        {
            get;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public abstract ParcelInfo this[UUID regionID, UUID parcelID]
        {
            get;
        }
        public abstract List<UUID> ParcelsInRegion(UUID key);

        public abstract void Store(UUID regionID, ParcelInfo parcel);

        public abstract bool Remove(UUID regionID, UUID parcelID);
    }
}
