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

using System;
using System.Collections.Generic;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using SilverSim.Threading;
using System.Linq;
using SilverSim.Scene.Types.Scene;

namespace SilverSim.Database.Memory.SimulationData
{
    public sealed partial class MemorySimulationDataStorage : ISimulationDataParcelExperienceListStorageInterface
    {
        private readonly RwLockedDictionaryAutoAdd<string, RwLockedDictionary<UUID, ParcelExperienceEntry>> m_Data = new RwLockedDictionaryAutoAdd<string, RwLockedDictionary<UUID, ParcelExperienceEntry>>(() => new RwLockedDictionary<UUID, ParcelExperienceEntry>());

        private string GenParcelAccessListKey(UUID regionID, UUID parcelID) => regionID.ToString() + ":" + parcelID.ToString();

        List<ParcelExperienceEntry> IParcelExperienceList.this[UUID regionID, UUID parcelID]
        {
            get
            {
                RwLockedDictionary<UUID, ParcelExperienceEntry> list;
                return (m_Data.TryGetValue(GenParcelAccessListKey(regionID, parcelID), out list)) ?
                    new List<ParcelExperienceEntry>(from entry in list.Values where true select new ParcelExperienceEntry(entry)) :
                    new List<ParcelExperienceEntry>();
            }
        }

        ParcelExperienceEntry IParcelExperienceList.this[UUID regionID, UUID parcelID, UUID experienceID]
        {
            get
            {
                RwLockedDictionary<UUID, ParcelExperienceEntry> list;
                ParcelExperienceEntry ret;
                if(!m_Data.TryGetValue(GenParcelAccessListKey(regionID, parcelID), out list) || !list.TryGetValue(experienceID, out ret))
                {
                    throw new KeyNotFoundException();
                }
                return new ParcelExperienceEntry(ret);
            }
        }

        bool IParcelExperienceList.TryGetValue(UUID regionID, UUID parcelID, UUID experienceID, out ParcelExperienceEntry entry)
        {
            RwLockedDictionary<UUID, ParcelExperienceEntry> list;
            ParcelExperienceEntry ret;
            if (m_Data.TryGetValue(GenParcelAccessListKey(regionID, parcelID), out list) && list.TryGetValue(experienceID, out ret))
            {
                entry = new ParcelExperienceEntry(ret);
                return true;
            }
            entry = default(ParcelExperienceEntry);
            return false;
        }

        bool IParcelExperienceList.Remove(UUID regionID, UUID parcelID) => 
            m_Data.Remove(GenParcelAccessListKey(regionID, parcelID));

        bool IParcelExperienceList.Remove(UUID regionID, UUID parcelID, UUID experienceID)
        {
            RwLockedDictionary<UUID, ParcelExperienceEntry> list;
            return m_Data.TryGetValue(GenParcelAccessListKey(regionID, parcelID), out list) && list.Remove(experienceID);
        }

        bool ISimulationDataParcelExperienceListStorageInterface.RemoveAllFromRegion(UUID regionID)
        {
            bool found = false;
            foreach (var key in new List<string>(from key in m_Data.Keys where key.StartsWith(regionID.ToString()) select key))
            {
                found = m_Data.Remove(key) || found;
            }
            return found;
        }

        public void Store(ParcelExperienceEntry entry)
        {
            string key = GenParcelAccessListKey(entry.RegionID, entry.ParcelID);
            m_Data[key][entry.ExperienceID] = new ParcelExperienceEntry(entry);
        }
    }
}
