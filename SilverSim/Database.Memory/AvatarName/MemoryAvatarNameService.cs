// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
        readonly RwLockedDictionary<UUID, UUI> m_Data = new RwLockedDictionary<UUID, UUI>();

        #region Constructor
        public MemoryAvatarNameService()
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* nothing to do */
        }
        #endregion

        #region Accessors
        public override bool TryGetValue(string firstName, string lastName, out UUI uui)
        {
            IEnumerable<UUI> res = from data in m_Data where data.Value.FirstName.ToLower() == firstName.ToLower() && data.Value.LastName.ToLower() == lastName.ToLower() select data.Value;
            foreach(UUI entry in res)
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

        public override bool TryGetValue(UUID key, out UUI uui)
        {
            return m_Data.TryGetValue(key, out uui);
        }

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

        public override bool Remove(UUID key)
        {
            return m_Data.Remove(key);
        }


        public override List<UUI> Search(string[] names)
        {
            if(names.Length < 1 || names.Length > 2)
            {
                return new List<UUI>();
            }

            IEnumerable<UUI> res;

            res = (names.Length == 1) ?
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
        public MemoryAvatarNameServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryAvatarNameService();
        }
    }
    #endregion
}
