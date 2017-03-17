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
                return (m_EstateGroupsData.TryGetValue(estateID, out res)) ?
                    new List<UGI>(from ugi in res.Keys where true select new UGI(ugi)) :
                    new List<UGI>();
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
