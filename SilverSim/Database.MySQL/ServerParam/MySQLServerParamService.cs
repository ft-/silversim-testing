/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.ServiceInterfaces.ServerParam;
using log4net;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using MySql.Data.MySqlClient;
using SilverSim.Types;
using ThreadedClasses;
using Nini.Config;

namespace SilverSim.Database.MySQL.ServerParam
{
    #region Service Implementation
    public class MySQLServerParamService : ServerParamServiceInterface, IDBServiceInterface, IPlugin
    {
        string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SERVER PARAM SERVICE");

        #region Cache
        private RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>> m_Cache = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>>(delegate() { return new RwLockedDictionary<string, string>(); });
        #endregion

        #region Constructor
        public MySQLServerParamService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();

                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM serverparams", connection))
                {
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while(dbReader.Read())
                        {
                            UUID regionid = new UUID((string)dbReader["regionid"]);
                            m_Cache[regionid].Add((string)dbReader["parametername"], (string)dbReader["parametervalue"]);
                        }
                    }
                }
            }
        }
        #endregion

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "serverparams", Migrations, m_Log);
        }

        public override List<string> this[UUID regionID]
        {
            get
            {
                RwLockedDictionary<string, string> regParams;
                if (m_Cache.TryGetValue(regionID, out regParams))
                {
                    List<string> list = new List<string>(regParams.Keys);
                    if(m_Cache.TryGetValue(regionID, out regParams) && regionID != UUID.Zero)
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

        public override string this[UUID regionID, string parameter, string defvalue]
        {
            get
            {
                RwLockedDictionary<string, string> regParams;
                if (m_Cache.TryGetValue(regionID, out regParams))
                {
                    string val;
                    if (regParams.TryGetValue(parameter, out val))
                    {
                        return val;
                    }
                }

                m_Cache[regionID][parameter] = defvalue;
                return defvalue;
            }
        }
        public override string this[UUID regionID, string parameter]
        {
            get
            {
                RwLockedDictionary<string, string> regParams;
                if(m_Cache.TryGetValue(regionID, out regParams))
                {
                    string val;
                    if(regParams.TryGetValue(parameter, out val))
                    {
                        return val;
                    }
                }

                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM serverparams WHERE (regionid LIKE '" + regionID.ToString() + "' OR regionid LIKE '00000000-0000-0000-0000-000000000000') AND parametername LIKE '" + MySqlHelper.EscapeString(parameter) + "'", connection))
                    {
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if(dbReader.Read())
                            {
                                m_Cache[regionID][parameter] = (string)dbReader["parametervalue"];
                                return (string)dbReader["parametervalue"];
                            }
                        }
                    }
                }

                throw new KeyNotFoundException("Key " + regionID.ToString() + ":" + parameter);
            }

            set
            {
                using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    using(MySqlCommand cmd = new MySqlCommand("REPLACE INTO serverparams (regionid, parametername, parametervalue) VALUES (?regionid, ?parametername, ?parametervalue)"))
                    {
                        cmd.Parameters.AddWithValue("?regionid", regionID);
                        cmd.Parameters.AddWithValue("?parametername", parameter);
                        cmd.Parameters.AddWithValue("?parametervalue", value);
                    }
                }
                m_Cache[regionID][parameter] = value;
            }
        }


        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "regionid CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "parametername VARCHAR(255)," +
                "parametervalue TEXT," +
                "PRIMARY KEY(regionid, parametername))"
        };
    }
    #endregion

    #region Factory
    [PluginName("ServerParams")]
    class MySQLServerParamServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SERVER PARAM SERVICE");
        public MySQLServerParamServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLServerParamService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
