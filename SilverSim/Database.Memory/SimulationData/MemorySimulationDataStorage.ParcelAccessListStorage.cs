// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Database.Memory.SimulationData
{
    public class MemorySimulationDataParcelAccessListStorage : ISimulationDataParcelAccessListStorageInterface
    {
        readonly RwLockedDictionaryAutoAdd<string, RwLockedDictionary<UUI, ParcelAccessEntry>> m_Data = new RwLockedDictionaryAutoAdd<string, RwLockedDictionary<UUI, ParcelAccessEntry>>(delegate() { return new RwLockedDictionary<UUI, ParcelAccessEntry>(); });
        public MemorySimulationDataParcelAccessListStorage()
        {
        }

        string GenParcelAccessListKey(UUID regionID, UUID parcelID)
        {
            return regionID.ToString() + ":" + parcelID.ToString();
        }

        bool IParcelAccessList.this[UUID regionID, UUID parcelID, UUI accessor]
        {
            get
            {
                RwLockedDictionary<UUI, ParcelAccessEntry> list;
                if(m_Data.TryGetValue(GenParcelAccessListKey(regionID, parcelID), out list))
                {
                    IEnumerable<ParcelAccessEntry> en = from entry in list.Values where entry.Accessor.EqualsGrid(accessor) select entry;
                    return en.GetEnumerator().MoveNext();
                }
                return false;
            }
        }

        List<ParcelAccessEntry> IParcelAccessList.this[UUID regionID, UUID parcelID]
        {
            get
            {
                RwLockedDictionary<UUI, ParcelAccessEntry> list;
                return (m_Data.TryGetValue(GenParcelAccessListKey(regionID, parcelID), out list)) ?
                    new List<ParcelAccessEntry>(from entry in list.Values where true select new ParcelAccessEntry(entry)) :
                    new List<ParcelAccessEntry>();
            }
        }

        void IParcelAccessList.Store(ParcelAccessEntry entry)
        {
            string key = GenParcelAccessListKey(entry.RegionID, entry.ParcelID);
            m_Data[key][entry.Accessor] = new ParcelAccessEntry(entry);
        }

        bool ISimulationDataParcelAccessListStorageInterface.RemoveAllFromRegion(UUID regionID)
        {
            bool found = false;
            List<string> keys = new List<string>(from key in m_Data.Keys where key.StartsWith(regionID.ToString()) select key);
            foreach(string key in keys)
            {
                found = m_Data.Remove(key) || found;
            }
            return found;
        }

        public bool Remove(UUID regionID, UUID parcelID)
        {
            return m_Data.Remove(GenParcelAccessListKey(regionID, parcelID));
        }

        public bool Remove(UUID regionID, UUID parcelID, UUI accessor)
        {
            RwLockedDictionary<UUI, ParcelAccessEntry> list;
            return m_Data.TryGetValue(GenParcelAccessListKey(regionID, parcelID), out list) && list.Remove(accessor);
        }
    }
}
