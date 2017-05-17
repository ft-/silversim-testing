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
            var keys = new List<string>(from key in m_Data.Keys where key.StartsWith(regionID.ToString()) select key);
            foreach(var key in keys)
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
