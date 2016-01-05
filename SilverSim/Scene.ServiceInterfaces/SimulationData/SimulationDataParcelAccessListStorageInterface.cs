// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System.Collections.Generic;

namespace SilverSim.Scene.ServiceInterfaces.SimulationData
{
    public abstract class SimulationDataParcelAccessListStorageInterface : IParcelAccessList
    {
        public SimulationDataParcelAccessListStorageInterface()
        {

        }

        public abstract bool this[UUID parcelID, UUI accessor] { get; }
        public abstract List<ParcelAccessEntry> this[UUID parcelID] { get; }
        public abstract void Store(ParcelAccessEntry entry);
        public abstract bool RemoveAll(UUID parcelID);
        public abstract bool Remove(UUID parcelID, UUI accessor);
    }
}
