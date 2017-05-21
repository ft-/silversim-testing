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
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SilverSim.Database.Memory.AvatarName
{
    #region Service Implementation
    [Description("Memory AvatarName Backend")]
    public sealed class MemoryAvatarNameService : AvatarNameServiceInterface, IPlugin
    {
        private readonly RwLockedDictionary<UUID, UUI> m_Data = new RwLockedDictionary<UUID, UUI>();

        #region Constructor
        public void Startup(ConfigurationLoader loader)
        {
            /* nothing to do */
        }
        #endregion

        #region Accessors
        public override bool TryGetValue(string firstName, string lastName, out UUI uui)
        {
            foreach(UUI entry in from data in m_Data where data.Value.FirstName.ToLower() == firstName.ToLower() && data.Value.LastName.ToLower() == lastName.ToLower() select data.Value)
            {
                uui = new UUI(entry);
                return true;
            }
            uui = UUI.Unknown;
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override UUI this[string firstName, string lastName]
        {
            get
            {
                UUI uui;
                if(!TryGetValue(firstName, lastName, out uui))
                {
                    throw new KeyNotFoundException();
                }
                return uui;
            }
        }

        public override bool TryGetValue(UUID key, out UUI uui) => m_Data.TryGetValue(key, out uui);

        public override UUI this[UUID key]
        {
            get
            {
                UUI uui;
                if(!TryGetValue(key, out uui))
                {
                    throw new KeyNotFoundException();
                }
                return uui;
            }
        }
        #endregion

        public override void Store(UUI value)
        {
            if (value.IsAuthoritative) /* do not store non-authoritative entries */
            {
                m_Data[value.ID] = new UUI(value);
            }
        }

        public override bool Remove(UUID key) => m_Data.Remove(key);

        public override List<UUI> Search(string[] names)
        {
            if(names.Length < 1 || names.Length > 2)
            {
                return new List<UUI>();
            }

            IEnumerable<UUI> res = (names.Length == 1) ?
                from data in m_Data.Values where data.FirstName.ToLower().Contains(names[0].ToLower()) || data.LastName.ToLower().Contains(names[0].ToLower()) select new UUI(data) :
                from data in m_Data.Values where data.FirstName.ToLower().Contains(names[0].ToLower()) && data.LastName.ToLower().Contains(names[1].ToLower()) select new UUI(data);
            return new List<UUI>(res);
        }
    }
    #endregion

    #region Factory
    [PluginName("AvatarNames")]
    public class MemoryAvatarNameServiceFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection) =>
            new MemoryAvatarNameService();
    }
    #endregion
}
