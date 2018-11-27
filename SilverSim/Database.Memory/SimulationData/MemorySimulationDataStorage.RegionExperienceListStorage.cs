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
using SilverSim.Types.Experience;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.SimulationData
{
    public sealed partial class MemorySimulationDataStorage : ISimulationDataRegionExperiencesStorageInterface
    {
        private readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, RegionExperienceInfo>> m_Experiences = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, RegionExperienceInfo>>(() => new RwLockedDictionary<UUID, RegionExperienceInfo>());

        List<RegionExperienceInfo> IRegionExperienceList.this[UUID regionID]
        {
            get
            {
                RwLockedDictionary<UUID, RegionExperienceInfo> exp;
                List<RegionExperienceInfo> result = new List<RegionExperienceInfo>();
                if (m_Experiences.TryGetValue(regionID, out exp))
                {
                    foreach(RegionExperienceInfo r in exp.Values)
                    {
                        result.Add(new RegionExperienceInfo(r));
                    }
                }
                return result;
            }
        }

        RegionExperienceInfo IRegionExperienceList.this[UUID regionID, UEI experienceID]
        {
            get
            {
                RegionExperienceInfo info;
                if(!RegionExperiences.TryGetValue(regionID, experienceID, out info))
                {
                    throw new KeyNotFoundException();
                }
                return info;
            }
        }

        bool IRegionExperienceList.TryGetValue(UUID regionID, UEI experienceID, out RegionExperienceInfo info)
        {
            RwLockedDictionary<UUID, RegionExperienceInfo> exp;
            RegionExperienceInfo i_info;
            if(m_Experiences.TryGetValue(regionID, out exp) && exp.TryGetValue(experienceID.ID, out i_info))
            {
                info = new RegionExperienceInfo(i_info);
                return true;
            }
            info = default(RegionExperienceInfo);
            return false;
        }

        void IRegionExperienceList.Store(RegionExperienceInfo info)
        {
            m_Experiences[info.RegionID][info.ExperienceID.ID] = new RegionExperienceInfo(info);
        }

        bool IRegionExperienceList.Remove(UUID regionID, UEI experienceID)
        {
            RwLockedDictionary<UUID, RegionExperienceInfo> exp;
            return m_Experiences.TryGetValue(regionID, out exp) && exp.Remove(experienceID.ID);
        }

        void ISimulationDataRegionExperiencesStorageInterface.RemoveRegion(UUID regionID)
        {
            m_Experiences.Remove(regionID);
        }
    }
}
