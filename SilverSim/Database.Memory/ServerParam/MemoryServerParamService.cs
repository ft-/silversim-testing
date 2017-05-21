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
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Database.Memory.ServerParam
{
    #region Service Implementation
    [Description("Memory ServerParam Backend")]
    public sealed class MemoryServerParamService : ServerParamServiceInterface, IPlugin
    {
        private readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>> m_Parameters = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>>(() => new RwLockedDictionary<string, string>());

        public override List<KeyValuePair<UUID, string>> KnownParameters
        {
            get
            {
                var result = new List<KeyValuePair<UUID, string>>();
                foreach(var kvp in m_Parameters)
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
                    var list = new List<string>(regParams.Keys);
                    if(m_Parameters.TryGetValue(regionID, out regParams) && regionID != UUID.Zero)
                    {
                        foreach(var k in regParams.Keys)
                        {
                            if(!list.Exists((string p) => p == k))
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
                var resultSet = new List<KeyValuePair<UUID, string>>();
                foreach(var kvp in m_Parameters)
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
            if(TryGetExplicitValue(regionID, parameter, out value))
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

        public override bool TryGetExplicitValue(UUID regionID, string parameter, out string value)
        {
            RwLockedDictionary<string, string> regParams;
            value = string.Empty;
            return m_Parameters.TryGetValue(regionID, out regParams) && regParams.TryGetValue(parameter, out value);
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

        public override bool Remove(UUID regionID, string parameter) =>
            m_Parameters[regionID].Remove(parameter);
    }
    #endregion

    #region Factory
    [PluginName("ServerParams")]
    public class MemoryServerParamServiceFactory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection) =>
            new MemoryServerParamService();
    }
    #endregion
}
