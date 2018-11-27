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

using SilverSim.Types;
using SilverSim.Types.Experience;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Experience
{
    public abstract class ExperienceServiceInterface
    {
        public ExperienceInfo this[UEI experienceID]
        {
            get
            {
                ExperienceInfo info;
                if(!TryGetValue(experienceID, out info))
                {
                    throw new KeyNotFoundException();
                }
                return info;
            }
        }

        public ExperienceInfo this[UUID experienceID]
        {
            get
            {
                ExperienceInfo info;
                if (!TryGetValue(experienceID, out info))
                {
                    throw new KeyNotFoundException();
                }
                return info;
            }
        }

        public abstract bool TryGetValue(UUID experienceID, out UEI uei);
        public virtual bool TryGetValue(UUID experienceID, out ExperienceInfo experienceInfo) => TryGetValue(new UEI(experienceID), out experienceInfo);
        public abstract bool TryGetValue(UEI experienceID, out ExperienceInfo experienceInfo);
        /** <summary>updates ID field in ExperienceInfo</summary> */
        public abstract void Add(ExperienceInfo info);
        public abstract void Update(UGUI requestingAgent, ExperienceInfo info);
        public abstract bool Remove(UGUI requestingAgent, UEI id);

        public abstract List<UEI> GetGroupExperiences(UGI group);
        public abstract List<UEI> GetCreatorExperiences(UGUI creator);
        public abstract List<UEI> GetOwnerExperiences(UGUI owner);
        public abstract List<UEI> FindExperienceByName(string query);
        public abstract List<ExperienceInfo> FindExperienceInfoByName(string query);

        public abstract IExperiencePermissionsInterface Permissions { get; }

        public abstract IExperienceAdminInterface Admins { get; }

        public abstract IExperienceKeyValueInterface KeyValueStore { get; }

        /** <summary>Support authorization data to be granted.</summary>
         * When granted, it adds AuthorizationToken data to UEI which needs to be stored on ExperienceNameStorage.
         */
        public virtual bool TryRequestAuthorization(UGUI requestingAgent, UEI experienceId) => false;
    }
}
