// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SilverSim.Database.Memory.Estate
{
    public partial class MemoryEstateService : IEstateManagerServiceInterface, IEstateManagerServiceListAccessInterface
    {
        readonly RwLockedDictionaryAutoAdd<uint, RwLockedDictionary<UUI, bool>> m_EstateManagerData = new RwLockedDictionaryAutoAdd<uint, RwLockedDictionary<UUI, bool>>(delegate () { return new RwLockedDictionary<UUI, bool>(); });

        List<UUI> IEstateManagerServiceListAccessInterface.this[uint estateID]
        {
            get 
            {
                RwLockedDictionary<UUI, bool> res;
                if (m_EstateManagerData.TryGetValue(estateID, out res))
                {
                    return new List<UUI>(from uui in res.Keys where true select new UUI(uui));
                }
                else
                {
                    return new List<UUI>();
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        bool IEstateManagerServiceInterface.this[uint estateID, UUI agent]
        {
            get
            {
                RwLockedDictionary<UUI, bool> res;
                return m_EstateManagerData.TryGetValue(estateID, out res) && res.ContainsKey(agent);
            }
            set
            {
                if (value)
                {
                    m_EstateManagerData[estateID][agent] = true;
                }
                else
                {
                    m_EstateManagerData[estateID].Remove(agent);
                }
            }
        }

        IEstateManagerServiceListAccessInterface IEstateManagerServiceInterface.All
        {
            get
            {
                return this;
            }
        }
    }
}
