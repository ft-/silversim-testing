// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
