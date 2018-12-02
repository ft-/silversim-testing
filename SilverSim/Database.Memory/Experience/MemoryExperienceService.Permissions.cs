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

using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.Experience
{
    public sealed partial class MemoryExperienceService : IExperiencePermissionsInterface
    {
        private readonly RwLockedDictionaryAutoAdd<UEI, RwLockedDictionary<UGUI, bool>> m_Perms = new RwLockedDictionaryAutoAdd<UEI, RwLockedDictionary<UGUI, bool>>(() => new RwLockedDictionary<UGUI, bool>());

        bool IExperiencePermissionsInterface.this[UEI experienceID, UGUI agent]
        {
            get
            {
                bool result;
                if(!Permissions.TryGetValue(experienceID, agent, out result))
                {
                    throw new KeyNotFoundException();
                }
                return result;
            }

            set
            {
                m_Perms[experienceID][agent] = value;
            }
        }

        bool IExperiencePermissionsInterface.Remove(UEI experienceID, UGUI agent)
        {
            RwLockedDictionary<UGUI, bool> k;
            return m_Perms.TryGetValue(experienceID, out k) && k.Remove(agent);
        }

        bool IExperiencePermissionsInterface.TryGetValue(UEI experienceID, UGUI agent, out bool allowed)
        {
            RwLockedDictionary<UGUI, bool> k;
            allowed = false;
            return m_Perms.TryGetValue(experienceID, out k) && k.TryGetValue(agent, out allowed);
        }

        Dictionary<UEI, bool> IExperiencePermissionsInterface.this[UGUI agent]
        {
            get
            {
                var res = new Dictionary<UEI, bool>();
                foreach(KeyValuePair<UEI, RwLockedDictionary<UGUI, bool>> kvp in m_Perms)
                {
                    bool allowed;
                    if(kvp.Value.TryGetValue(agent ,out allowed))
                    {
                        UEI uei;
                        if (!TryGetValue(kvp.Key.ID, out uei))
                        {
                            uei = kvp.Key;
                        }
                        res.Add(uei, allowed);
                    }
                }
                return res;
            }
        }

    }
}
