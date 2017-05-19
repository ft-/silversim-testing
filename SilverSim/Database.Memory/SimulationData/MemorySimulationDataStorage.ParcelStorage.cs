// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

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

        ISimulationDataParcelAccessListStorageInterface ISimulationDataParcelStorageInterface.WhiteList => m_WhiteListStorage;

        ISimulationDataParcelAccessListStorageInterface ISimulationDataParcelStorageInterface.BlackList => m_BlackListStorage;
    }
}
