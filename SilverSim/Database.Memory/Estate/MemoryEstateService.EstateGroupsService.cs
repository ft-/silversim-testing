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
    public partial class MemoryEstateService : IEstateGroupsServiceInterface, IEstateGroupsServiceListAccessInterface
    {
        readonly RwLockedDictionaryAutoAdd<uint, RwLockedDictionary<UGI, bool>> m_EstateGroupsData = new RwLockedDictionaryAutoAdd<uint, RwLockedDictionary<UGI, bool>>(delegate () { return new RwLockedDictionary<UGI, bool>(); });

        List<UGI> IEstateGroupsServiceListAccessInterface.this[uint estateID]
        {
            get
            {
                RwLockedDictionary<UGI, bool> res;
                if (m_EstateGroupsData.TryGetValue(estateID, out res))
                {
                    return new List<UGI>(from ugi in res.Keys where true select new UGI(ugi));
                }
                else
                {
                    return new List<UGI>();
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        bool IEstateGroupsServiceInterface.this[uint estateID, UGI group]
        {
            get
            {
                RwLockedDictionary<UGI, bool> res;
                return m_EstateGroupsData.TryGetValue(estateID, out res) && res.ContainsKey(group);
            }
            set
            {
                if (value)
                {
                    m_EstateGroupsData[estateID][group] = true;
                }
                else
                {
                    m_EstateGroupsData[estateID].Remove(group);
                }
            }
        }

        IEstateGroupsServiceListAccessInterface IEstateGroupsServiceInterface.All
        {
            get 
            {
                return this;
            }
        }
    }
}
