// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.Estate
{
    public partial class MemoryEstateService : IEstateRegionMapServiceInterface
    {
        readonly RwLockedDictionaryAutoAdd<uint, RwLockedList<UUID>> m_RegionMapData = new RwLockedDictionaryAutoAdd<uint, RwLockedList<UUID>>(delegate () { return new RwLockedList<UUID>(); });

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
            foreach(KeyValuePair<uint, RwLockedList<UUID>> kvp in m_RegionMapData)
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
            foreach (KeyValuePair<uint, RwLockedList<UUID>> kvp in m_RegionMapData)
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
