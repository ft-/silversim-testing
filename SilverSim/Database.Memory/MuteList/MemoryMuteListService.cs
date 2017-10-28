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

using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.MuteList;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.MuteList;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Database.Memory.MuteList
{
    [PluginName("MuteList")]
    [Description("memory MuteList Backend")]
    public sealed class MemoryMuteListService : MuteListServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, MuteListEntry>> m_MuteLists = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, MuteListEntry>>(() => new RwLockedDictionary<string, MuteListEntry>());

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        private string GetKey(UUID muteID, string muteName) => $"{muteID}/${muteName}";

        public override List<MuteListEntry> GetList(UUID muteListOwnerID)
        {
            var res = new List<MuteListEntry>();
            RwLockedDictionary<string, MuteListEntry> list;
            if(m_MuteLists.TryGetValue(muteListOwnerID, out list))
            {
                foreach(MuteListEntry e in list.Values)
                {
                    res.Add(new MuteListEntry(e));
                }
            }
            return res;
        }

        public override bool Remove(UUID muteListOwnerID, UUID muteID, string muteName)
        {
            RwLockedDictionary<string, MuteListEntry> list;
            return m_MuteLists.TryGetValue(muteListOwnerID, out list) && list.Remove(GetKey(muteID, muteName));
        }

        public void Remove(UUID scopeID, UUID accountID)
        {
            m_MuteLists.Remove(accountID);
        }

        public override void Store(UUID muteListOwnerID, MuteListEntry mute)
        {
            RwLockedDictionary<string, MuteListEntry> list = m_MuteLists[muteListOwnerID];
            list[GetKey(mute.MuteID, mute.MuteName)] = new MuteListEntry(mute);
        }
    }
}
