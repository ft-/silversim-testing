﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Parcel;
using System.Collections.Generic;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataParcelStorageInterface
    {
        #region Constructor
        public SimulationDataParcelStorageInterface()
        {
        }
        #endregion

        public abstract ParcelInfo this[UUID regionID, UUID parcelID]
        {
            get;
        }
        public abstract List<UUID> ParcelsInRegion(UUID key);

        public abstract void Store(UUID regionID, ParcelInfo parcel);
    }
}
