// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Database.Memory.Estate
{
    public partial class MemoryEstateService : IEstateOwnerServiceInterface
    {
        readonly RwLockedDictionary<uint, UUI> m_EstateOwnerData = new RwLockedDictionary<uint, UUI>();

        bool IEstateOwnerServiceInterface.TryGetValue(uint estateID, out UUI uui)
        {
            if(m_EstateOwnerData.TryGetValue(estateID, out uui))
            {
                uui = new UUI(uui);
                return true;
            }
            uui = default(UUI);
            return false;
        }

        List<uint> IEstateOwnerServiceInterface.this[UUI owner]
        {
            get
            {
                return new List<uint>(from data in m_EstateOwnerData where data.Value.EqualsGrid(owner) select data.Key);
            }
        }

        UUI IEstateOwnerServiceInterface.this[uint estateID]
        {
            get
            {
                UUI uui;
                if(!EstateOwner.TryGetValue(estateID, out uui))
                {
                    throw new KeyNotFoundException();
                }
                return uui;
            }
            set
            {
                m_EstateOwnerData[estateID] = new UUI(value);
            }
        }
    }
}
