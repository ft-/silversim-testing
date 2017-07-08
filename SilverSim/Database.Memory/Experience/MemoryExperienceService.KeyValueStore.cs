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
    public sealed partial class MemoryExperienceService : ExperienceServiceInterface.IExperienceKeyInterface
    {
        private readonly object m_UpdateLock = new object();
        private readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>> m_KeyValues = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>>(() => new RwLockedDictionary<string, string>());

        bool IExperienceKeyInterface.Remove(UUID experienceID, string key)
        {
            RwLockedDictionary<string, string> exp;
            return m_KeyValues.TryGetValue(experienceID, out exp) && exp.Remove(key);
        }

        bool IExperienceKeyInterface.TryGetValue(UUID experienceID, string key, out string val)
        {
            RwLockedDictionary<string, string> exp;
            val = string.Empty;
            return m_KeyValues.TryGetValue(experienceID, out exp) && exp.TryGetValue(key, out val);
        }

        void IExperienceKeyInterface.Add(UUID experienceID, string key, string value)
        {
            m_KeyValues[experienceID].Add(key, value);
        }

        void IExperienceKeyInterface.Store(UUID experienceID, string key, string value)
        {
            RwLockedDictionary<string, string> exp = m_KeyValues[experienceID];
            lock (m_UpdateLock)
            {
                exp[key] = value;
            }
        }

        bool IExperienceKeyInterface.StoreOnlyIfEqualOrig(UUID experienceID, string key, string value, string orig_value)
        {
            bool changed;
            RwLockedDictionary<string, string> exp = m_KeyValues[experienceID];
            lock (m_UpdateLock)
            {
                string ov;
                changed = exp.TryGetValue(key, out ov) && ov == orig_value;
                if (changed)
                {
                    exp[key] = value;
                }
            }

            return changed;
        }
    }
}
