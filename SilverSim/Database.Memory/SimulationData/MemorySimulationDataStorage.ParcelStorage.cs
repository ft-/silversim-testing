// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.Memory.SimulationData
{
    public partial class MemorySimulationDataStorage : ISimulationDataParcelStorageInterface
    {
        readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, ParcelInfo>> m_ParcelData = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, ParcelInfo>>(delegate () { return new RwLockedDictionary<UUID, ParcelInfo>(); });
        readonly MemorySimulationDataParcelAccessListStorage m_WhiteListStorage = new MemorySimulationDataParcelAccessListStorage();
        readonly MemorySimulationDataParcelAccessListStorage m_BlackListStorage = new MemorySimulationDataParcelAccessListStorage();

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        ParcelInfo ISimulationDataParcelStorageInterface.this[UUID regionID, UUID parcelID]
        {
            get
            {
                RwLockedDictionary<UUID, ParcelInfo> parcels;
                if(m_ParcelData.TryGetValue(regionID, out parcels))
                {
                    return new ParcelInfo(parcels[parcelID]);
                }
                throw new KeyNotFoundException();
            }
        }

        bool ISimulationDataParcelStorageInterface.Remove(UUID regionID, UUID parcelID)
        {
            RwLockedDictionary<UUID, ParcelInfo> parcels;
            return m_ParcelData.TryGetValue(regionID, out parcels) && parcels.Remove(parcelID);
        }

        void RemoveAllParcelsInRegion(UUID regionID)
        {
            m_ParcelData.Remove(regionID);
        }

        List<UUID> ISimulationDataParcelStorageInterface.ParcelsInRegion(UUID key)
        {
            RwLockedDictionary<UUID, ParcelInfo> parcels;
            return (m_ParcelData.TryGetValue(key, out parcels)) ?
                new List<UUID>(parcels.Keys) :
                new List<UUID>();
        }

        void ISimulationDataParcelStorageInterface.Store(UUID regionID, ParcelInfo parcel)
        {
            m_ParcelData[regionID][parcel.ID] = new ParcelInfo(parcel);
        }

        ISimulationDataParcelAccessListStorageInterface ISimulationDataParcelStorageInterface.WhiteList
        {
            get
            {
                return m_WhiteListStorage;
            }
        }

        ISimulationDataParcelAccessListStorageInterface ISimulationDataParcelStorageInterface.BlackList
        {
            get
            {
                return m_BlackListStorage;
            }
        }
    }
}
