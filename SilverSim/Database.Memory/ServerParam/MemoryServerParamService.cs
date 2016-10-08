// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System;

namespace SilverSim.Database.Memory.ServerParam
{
    #region Service Implementation
    [Description("Memory ServerParam Backend")]
    public sealed class MemoryServerParamService : ServerParamServiceInterface, IPlugin
    {
        private static readonly ILog m_Log = LogManager.GetLogger("NULL SERVER PARAM SERVICE");

        readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>> m_Parameters = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>>(delegate() { return new RwLockedDictionary<string, string>(); });

        public override List<KeyValuePair<UUID, string>> KnownParameters
        {
            get
            {
                List<KeyValuePair<UUID, string>> result = new List<KeyValuePair<UUID, string>>();
                foreach(KeyValuePair<UUID, RwLockedDictionary<string, string>> kvp in m_Parameters)
                {
                    foreach(string parameter in kvp.Value.Keys)
                    {
                        result.Add(new KeyValuePair<UUID, string>(kvp.Key, parameter));
                    }
                }
                return result;
            }
        }

        #region Constructor
        public MemoryServerParamService()
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* nothing to do */
        }
        #endregion

        public override List<string> this[UUID regionID]
        {
            get
            {
                RwLockedDictionary<string, string> regParams;
                if (m_Parameters.TryGetValue(regionID, out regParams))
                {
                    List<string> list = new List<string>(regParams.Keys);
                    if(m_Parameters.TryGetValue(regionID, out regParams) && regionID != UUID.Zero)
                    {
                        foreach(string k in regParams.Keys)
                        {
                            if(!list.Exists(delegate(string p) { return p == k;}))
                            {
                                list.Add(k);
                            }
                        }
                    }
                    return list;
                }

                return new List<string>();
            }
        }

        public override List<KeyValuePair<UUID, string>> this[string parametername]
        {
            get
            {
                List<KeyValuePair<UUID, string>> resultSet = new List<KeyValuePair<UUID, string>>();
                foreach(KeyValuePair<UUID, RwLockedDictionary<string, string>> kvp in m_Parameters)
                {
                    string parametervalue;
                    if(kvp.Value.TryGetValue(parametername, out parametervalue))
                    {
                        resultSet.Add(new KeyValuePair<UUID, string>(kvp.Key, parametervalue));
                    }
                }
                return resultSet;
            }
        }

        public override bool TryGetValue(UUID regionID, string parameter, out string value)
        {
            RwLockedDictionary<string, string> regParams;
            if (m_Parameters.TryGetValue(regionID, out regParams) &&
                regParams.TryGetValue(parameter, out value))
            {
                return true;
            }

            if (UUID.Zero != regionID &&
                TryGetValue(UUID.Zero, parameter, out value))
            {
                return true;
            }

            value = string.Empty;
            return false;
        }

        public override bool Contains(UUID regionID, string parameter)
        {
            RwLockedDictionary<string, string> regParams;
            if (m_Parameters.TryGetValue(regionID, out regParams) &&
                regParams.ContainsKey(parameter))
            {
                return true;
            }

            if (UUID.Zero != regionID &&
                Contains(UUID.Zero, parameter))
            {
                return true;
            }

            return false;
        }

        protected override void Store(UUID regionID, string parameter, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Remove(regionID, parameter);
            }
            else
            {
                m_Parameters[regionID][parameter] = value;
            }
        }

        public override bool Remove(UUID regionID, string parameter)
        {
            return m_Parameters[regionID].Remove(parameter);
        }
    }
    #endregion

    #region Factory
    [PluginName("ServerParams")]
    public class MemoryServerParamServiceFactory : IPluginFactory
    {
        public MemoryServerParamServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MemoryServerParamService();
        }
    }
    #endregion
}
