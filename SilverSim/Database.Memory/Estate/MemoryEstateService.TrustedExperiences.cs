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
using System.Linq;

namespace SilverSim.Database.Memory.Estate
{
    public sealed partial class MemoryEstateService : IEstateTrustedExperienceServiceInterface
    {
        private readonly RwLockedDictionaryAutoAdd<uint, RwLockedDictionary<UUID, bool>> m_TrustedExperiences = new RwLockedDictionaryAutoAdd<uint, RwLockedDictionary<UUID, bool>>(() => new RwLockedDictionary<UUID, bool>());

        List<UEI> IEstateTrustedExperienceServiceInterface.this[uint estateID]
        {
            get
            {
                RwLockedDictionary<UUID, bool> exp;
                return m_TrustedExperiences.TryGetValue(estateID, out exp) ? new List<UEI>(from k in exp.Keys select new UEI(k)) : new List<UEI>();
            }
        }

        bool IEstateTrustedExperienceServiceInterface.this[uint estateID, UEI experienceID]
        {
            get
            {
                RwLockedDictionary<UUID, bool> exp;
                return m_TrustedExperiences.TryGetValue(estateID, out exp) && exp.ContainsKey(experienceID.ID);
            }

            set
            {
                if(value)
                {
                    m_TrustedExperiences[estateID][experienceID.ID] = value;
                }
                else
                {
                    m_TrustedExperiences[estateID].Remove(experienceID.ID);
                }
            }
        }

        bool IEstateTrustedExperienceServiceInterface.Remove(uint estateID, UEI experienceID)
        {
            RwLockedDictionary<UUID, bool> exp;
            return m_TrustedExperiences.TryGetValue(estateID, out exp) && exp.Remove(experienceID.ID);
        }

        bool IEstateTrustedExperienceServiceInterface.TryGetValue(uint estateID, UEI experienceID, out bool trusted)
        {
            RwLockedDictionary<UUID, bool> exp;
            trusted = m_TrustedExperiences.TryGetValue(estateID, out exp) && exp.ContainsKey(experienceID.ID);
            return true;
        }
    }
}
