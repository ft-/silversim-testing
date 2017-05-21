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

using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.Estate
{
    public partial class MemoryEstateService : IEstateRegionMapServiceInterface
    {
        private readonly RwLockedDictionaryAutoAdd<uint, RwLockedList<UUID>> m_RegionMapData = new RwLockedDictionaryAutoAdd<uint, RwLockedList<UUID>>(() => new RwLockedList<UUID>());

        List<UUID> IEstateRegionMapServiceInterface.this[uint estateID]
        {
            get
            {
                RwLockedList<UUID> regions;
                return (m_RegionMapData.TryGetValue(estateID, out regions)) ? new List<UUID>(regions) : new List<UUID>();
            }
        }

        bool IEstateRegionMapServiceInterface.TryGetValue(UUID regionID, out uint estateID)
        {
            foreach(var kvp in m_RegionMapData)
            {
                if(kvp.Value.Contains(regionID))
                {
                    estateID = kvp.Key;
                    return true;
                }
            }
            estateID = 0;
            return false;
        }

        bool IEstateRegionMapServiceInterface.Remove(UUID regionID)
        {
            bool found = false;
            foreach (var kvp in m_RegionMapData)
            {
                kvp.Value.Remove(regionID);
                found = true;
            }
            return found;
        }

        uint IEstateRegionMapServiceInterface.this[UUID regionID]
        {
            get
            {
                uint estateID;
                if(!RegionMap.TryGetValue(regionID, out estateID))
                {
                    throw new KeyNotFoundException();
                }
                return estateID;
            }
            set
            {
                if(!m_RegionMapData[value].Contains(regionID))
                {
                    m_RegionMapData[value].Add(regionID);
                }
            }
        }
    }
}
