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
        public abstract ExperienceInfo this[UUID experienceID] { get; }
        public abstract bool TryGetValue(UUID experienceID, out ExperienceInfo experienceInfo);
        public abstract void Add(ExperienceInfo info);
        public abstract void Update(UUI requestingAgent, ExperienceInfo info);
        public abstract bool Remove(UUI requestingAgent, UUID id);

        public abstract List<UUID> GetGroupExperiences(UGI group);
        public abstract List<UUID> GetCreatorExperiences(UUI creator);
        public abstract List<UUID> GetOwnerExperiences(UUI owner);
        public abstract List<UUID> FindExperienceByName(string query);
        public abstract List<ExperienceInfo> FindExperienceInfoByName(string query);

        public interface IExperiencePermissionsInterface
        {
            bool this[UUID experienceID, UUI agent] { get; set; }
            bool TryGetValue(UUID experienceID, UUI agent, out bool allowed);
            bool Remove(UUID experienceID, UUI agent);
            Dictionary<UUID, bool> this[UUI agent] { get; }
        }

        public abstract IExperiencePermissionsInterface Permissions { get; }

        public interface IExperienceAdminInterface
        {
            bool this[UUID experienceID, UUI agent] { get; set; }
            bool TryGetValue(UUID experienceID, UUI agent, out bool allowed);
            List<UUID> this[UUI agent] { get; }
        }

        public abstract IExperienceAdminInterface Admins { get; }

        public interface IExperienceKeyValueInterface
        {
            bool TryGetValue(UUID experienceID, string key, out string val);
            bool Remove(UUID experienceID, string key);
            void Add(UUID experienceID, string key, string value);
            void Store(UUID experienceID, string key, string value);
            bool StoreOnlyIfEqualOrig(UUID experienceID, string key, string value, string orig_value);
            List<string> GetKeys(UUID experienceID);
            bool GetDatasize(UUID experienceID, out int used, out int quota);
        }

        public abstract IExperienceKeyValueInterface KeyValueStore { get; }
    }
}
