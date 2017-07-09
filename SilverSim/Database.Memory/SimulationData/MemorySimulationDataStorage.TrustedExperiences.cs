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

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.SimulationData
{
    public sealed partial class MemorySimulationDataStorage : ISimulationDataRegionTrustedExperiencesStorageInterface
    {
        private readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, bool>> m_TrustedExperiences = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, bool>>(() => new RwLockedDictionary<UUID, bool>());

        List<UUID> IRegionTrustedExperienceList.this[UUID regionID]
        {
            get
            {
                RwLockedDictionary<UUID, bool> exp;
                return m_TrustedExperiences.TryGetValue(regionID, out exp) ? new List<UUID>(exp.Keys) : new List<UUID>();
            }
        }

        bool IRegionTrustedExperienceList.this[UUID regionID, UUID experienceID]
        {
            get
            {
                RwLockedDictionary<UUID, bool> exp;
                return m_TrustedExperiences.TryGetValue(regionID, out exp) && exp.ContainsKey(experienceID);
            }
            set
            {
                if (value)
                {
                    m_TrustedExperiences[regionID][experienceID] = true;
                }
                else
                {
                    m_TrustedExperiences[regionID].Remove(experienceID);
                }
            }
        }

        bool IRegionTrustedExperienceList.Remove(UUID regionID, UUID experienceID)
        {
            RwLockedDictionary<UUID, bool> exp;
            return m_TrustedExperiences.TryGetValue(regionID, out exp) && exp.Remove(experienceID);
        }

        void ISimulationDataRegionTrustedExperiencesStorageInterface.RemoveRegion(UUID regionID)
        {
            m_TrustedExperiences.Remove(regionID);
        }

        bool IRegionTrustedExperienceList.TryGetValue(UUID regionID, UUID experienceID, out bool trusted)
        {
            trusted = false;
            RwLockedDictionary<UUID, bool> exp;
            if (m_TrustedExperiences.TryGetValue(regionID, out exp))
            {
                exp.TryGetValue(experienceID, out trusted);
            }
            return true;
        }
    }
}
