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
    partial class MemoryProfileService : ProfileServiceInterface.IPicksInterface
    {
        readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, ProfilePick>> m_Picks = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<UUID, ProfilePick>>(delegate () { return new RwLockedDictionary<UUID, ProfilePick>(); });

        ProfilePick IPicksInterface.this[UUI user, UUID id]
        {
            get
            {
                ProfilePick pick;
                if(!TryGetValue(user, id, out pick))
                {
                    throw new KeyNotFoundException();
                }
                return pick;
            }
        }

        public Dictionary<UUID, string> GetPicks(UUI user)
        {
            Dictionary<UUID, string> results = new Dictionary<UUID, string>();
            RwLockedDictionary<UUID, ProfilePick> picks;
            if(m_Picks.TryGetValue(user.ID, out picks))
            {
                foreach(KeyValuePair<UUID, ProfilePick> kvp in picks)
                {
                    results.Add(kvp.Key, kvp.Value.Name);
                }
            }
            return results;
        }

        public bool TryGetValue(UUI user, UUID id, out ProfilePick pick)
        {
            pick = default(ProfilePick);
            RwLockedDictionary<UUID, ProfilePick> picks;
            return m_Picks.TryGetValue(user.ID, out picks) && picks.TryGetValue(id, out pick);
        }

        public void Update(ProfilePick pick)
        {
            m_Picks[pick.Creator.ID][pick.PickID] = pick;
        }
    }
}
