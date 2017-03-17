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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SilverSim.Database.Memory.Groups
{
    #region Service Implementation
    [Description("Memory GroupsName Backend")]
    public sealed class MemoryGroupsNameService : GroupsNameServiceInterface, IPlugin
    {
        readonly RwLockedDictionary<UUID, UGI> m_Data = new RwLockedDictionary<UUID, UGI>();
        #region Constructor
        public MemoryGroupsNameService()
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* nothing to do */
        }
        #endregion

        #region Accessors
        public override UGI this[UUID groupID]
        {
            get
            {
                UGI ugi;
                if(!TryGetValue(groupID, out ugi))
                {
                    throw new KeyNotFoundException();
                }
                return ugi;
            }
        }

        public override bool TryGetValue(UUID groupID, out UGI ugi)
        {
            if(m_Data.TryGetValue(groupID, out ugi))
            {
                ugi = new UGI(ugi);
                return true;
            }
            ugi = default(UGI);
            return false;
        }

        public override List<UGI> GetGroupsByName(string groupName, int limit)
        {
            List<UGI> groups = new List<UGI>();
            IEnumerable<UGI> res = from grp in m_Data.Values
                                   where grp.GroupName.ToLower().Equals(groupName.ToLower())
                                   select grp;
            foreach(UGI ugi in res)
            {
                if(groups.Count < limit)
                {
                    groups.Add(new UGI(ugi));
                }
            }
            return groups;
        }

        public override void Store(UGI group)
        {
            m_Data[group.ID] = new UGI(group);
        }
        #endregion
    }
    #endregion

    #region Factory
    [PluginName("GroupNames")]
    public class MemoryGroupsNameServiceFactory : IPluginFactory
    {
        public MemoryGroupsNameServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryGroupsNameService();
        }
    }
    #endregion
}
