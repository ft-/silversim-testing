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
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Avatar;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.Memory.Avatar
{
    #region Service Implementation
    [Description("Memory Avatar Backend")]
    public sealed class MemoryAvatarService : AvatarServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>> m_Data = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>>(delegate () { return new RwLockedDictionary<string, string>(); });

        public MemoryAvatarService()
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* nothing to do */
        }

        public override Dictionary<string, string> this[UUID avatarID]
        {
            get
            {
                RwLockedDictionary<string, string> data;
                return (m_Data.TryGetValue(avatarID, out data)) ?
                    new Dictionary<string, string>(data) :
                    new Dictionary<string, string>();
            }
            set
            {
                if (null == value)
                {
                    m_Data.Remove(avatarID);
                }
                else
                {
                    RwLockedDictionary<string, string> data = new RwLockedDictionary<string, string>(value);
                    m_Data[avatarID] = data;
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override List<string> this[UUID avatarID, IList<string> itemKeys]
        {
            get
            {
                RwLockedDictionary<string, string> data;
                List<string> result = new List<string>();
                if(!m_Data.TryGetValue(avatarID, out data))
                {
                    data = new RwLockedDictionary<string, string>();
                }

                foreach(string key in itemKeys)
                {
                    string val;
                    result.Add(data.TryGetValue(key, out val) ? val : string.Empty);
                }
                return result;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                else if (itemKeys == null)
                {
                    throw new ArgumentNullException("itemKeys");
                }
                if (itemKeys.Count != value.Count)
                {
                    throw new ArgumentException("value and itemKeys must have identical Count");
                }

                RwLockedDictionary<string, string> data = m_Data[avatarID];
                for (int i = 0; i < itemKeys.Count; ++i)
                {
                    data[itemKeys[i]] = value[i];
                }
            }
        }

        public override bool TryGetValue(UUID avatarID, string itemKey, out string value)
        {
            value = string.Empty;
            RwLockedDictionary<string, string> data;
            return m_Data.TryGetValue(avatarID, out data) && data.TryGetValue(itemKey, out value);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override string this[UUID avatarID, string itemKey]
        {
            get
            {
                string s;
                if (!TryGetValue(avatarID, itemKey, out s))
                {
                    throw new KeyNotFoundException(string.Format("{0},{1} not found", avatarID, itemKey));
                }
                return s;
            }
            set
            {
                m_Data[avatarID][itemKey] = value;
            }
        }

        public override void Remove(UUID avatarID, IList<string> nameList)
        {
            RwLockedDictionary<string, string> data;
            if(!m_Data.TryGetValue(avatarID, out data))
            {
                return;
            }
            foreach (string name in nameList)
            {
                data.Remove(name);
            }
        }

        public override void Remove(UUID avatarID, string name)
        {
            RwLockedDictionary<string, string> data;
            if (!m_Data.TryGetValue(avatarID, out data))
            {
                return;
            }
            data.Remove(name);
        }

        public void Remove(UUID scopeID, UUID userAccount)
        {
            m_Data.Remove(userAccount);
        }
    }
    #endregion

    #region Factory
    [PluginName("Avatar")]
    public class MemoryInventoryServiceFactory : IPluginFactory
    {
        public MemoryInventoryServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryAvatarService();
        }
    }
    #endregion
}
