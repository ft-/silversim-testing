﻿// SilverSim is distributed under the terms of the
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
    public sealed partial class MemoryExperienceService : IExperienceAdminInterface
    {
        private readonly RwLockedDictionaryAutoAdd<UEI, RwLockedDictionary<UGUI, bool>> m_Admins = new RwLockedDictionaryAutoAdd<UEI, RwLockedDictionary<UGUI, bool>>(() => new RwLockedDictionary<UGUI, bool>());

        bool IExperienceAdminInterface.this[UEI experienceID, UGUI agent]
        {
            get
            {
                RwLockedDictionary<UGUI, bool> k;
                return m_Admins.TryGetValue(experienceID, out k) && k.ContainsKey(agent);
            }
            set
            {
                if (value)
                {
                    m_Admins[experienceID][agent] = true;
                }
                else
                {
                    m_Admins[experienceID].Remove(agent);
                }
            }
        }

        bool IExperienceAdminInterface.TryGetValue(UEI experienceID, UGUI agent, out bool allowed)
        {
            RwLockedDictionary<UGUI, bool> k;
            if (m_Admins.TryGetValue(experienceID, out k))
            {
                allowed = k.ContainsKey(agent);
                return true;
            }
            allowed = false;
            return false;
        }

        List<UEI> IExperienceAdminInterface.this[UGUI agent]
        {
            get
            {
                var res = new List<UEI>();
                foreach(KeyValuePair<UEI, RwLockedDictionary<UGUI, bool>> kvp in m_Admins)
                {
                    if(kvp.Value[agent])
                    {
                        UEI uei;
                        if (!TryGetValue(kvp.Key.ID, out uei))
                        {
                            uei = kvp.Key;
                        }
                        res.Add(kvp.Key);
                    }
                }

                return res;
            }
        }

    }
}
