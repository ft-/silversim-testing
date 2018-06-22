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

using SilverSim.ServiceInterfaces.Profile;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System.Collections.Generic;

namespace SilverSim.Database.Memory.Profile
{
    partial class MemoryProfileService : IClassifiedsInterface
    {
        private readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, ProfileClassified>> m_Classifieds = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, ProfileClassified>>(() => new RwLockedDictionary<UUID, ProfileClassified>());

        public ProfileClassified this[UGUI user, UUID id]
        {
            get
            {
                ProfileClassified classified;
                if(!TryGetValue(user, id, out classified))
                {
                    throw new KeyNotFoundException();
                }
                return classified;
            }
        }

        public bool ContainsKey(UGUI user, UUID id)
        {
            RwLockedDictionary<UUID, ProfileClassified> classifieds;
            return m_Classifieds.TryGetValue(user.ID, out classifieds) && classifieds.ContainsKey(id);
        }

        public void Delete(UUID id)
        {
            foreach(var classifieds in m_Classifieds.Values)
            {
                if(classifieds.Remove(id))
                {
                    break;
                }
            }
        }

        public Dictionary<UUID, string> GetClassifieds(UGUI user)
        {
            RwLockedDictionary<UUID, ProfileClassified> classifieds;
            var names = new Dictionary<UUID, string>();
            if(m_Classifieds.TryGetValue(user.ID, out classifieds))
            {
                foreach(var classified in classifieds)
                {
                    names.Add(classified.Key, classified.Value.Name);
                }
            }
            return names;
        }

        public bool TryGetValue(UGUI user, UUID id, out ProfileClassified classified)
        {
            classified = default(ProfileClassified);
            RwLockedDictionary<UUID, ProfileClassified> classifieds;
            return m_Classifieds.TryGetValue(user.ID, out classifieds) && classifieds.TryGetValue(id, out classified);
        }

        public void Update(ProfileClassified classified)
        {
            m_Classifieds[classified.Creator.ID][classified.ClassifiedID] = classified;
        }
    }
}
