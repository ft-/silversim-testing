// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ThreadedClasses;

namespace SilverSim.Database.MySQL.ServerParam
{
    #region Service Implementation
    [Description("MySQL ServerParam Backend")]
    public sealed class MySQLServerParamService : ServerParamServiceInterface, IDBServiceInterface, IPlugin
    {
        readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SERVER PARAM SERVICE");

        #region Cache
        readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>> m_Cache = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<string, string>>(delegate() { return new RwLockedDictionary<string, string>(); });
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
                            UUID regionid = dbReader.GetUUID("regionid");
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

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override string this[UUID regionID, string parameter, string defvalue]
        {
            get
            {
                string value;
                return (TryGetValue(regionID, parameter, out value)) ?
                    value :
                    defvalue;
            }
        }

        public override bool TryGetValue(UUID regionID, string parameter, out string value)
        {
            RwLockedDictionary<string, string> regParams;
            if (m_Cache.TryGetValue(regionID, out regParams) &&
                regParams.TryGetValue(parameter, out value))
            {
                return true;
            }

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();

                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM serverparams WHERE regionid LIKE ?regionid AND parametername LIKE ?parametername", connection))
                {
                    cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                    cmd.Parameters.AddWithValue("?parametername", parameter);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            m_Cache[regionID][parameter] = (string)dbReader["parametervalue"];
                            value = (string)dbReader["parametervalue"];
                            return true;
                        }
                    }
                }
            }

            if (UUID.Zero != regionID &&
                TryGetValue(UUID.Zero, parameter, out value))
            {
                return true;
            }

            value = string.Empty;
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override string this[UUID regionID, string parameter]
        {
            get
            {
                string value;
                if(!TryGetValue(regionID, parameter, out value))
                {
                    throw new KeyNotFoundException("Key " + regionID.ToString() + ":" + parameter);
                }
                return value;
            }

            set
            {
                using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    connection.InsideTransaction(delegate()
                    {
                        Dictionary<string, object> param = new Dictionary<string, object>();
                        param["regionid"] = regionID.ToString();
                        param["parametername"] = parameter;
                        param["parametervalue"] = value;
                        connection.ReplaceInto("serverparams", param);
                        m_Cache[regionID][parameter] = value;
                    });
                }
            }
        }

        public override bool Remove(UUID regionID, string parameter)
        {
            bool result = false;
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction(delegate()
                {
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM serverparams WHERE regionid LIKE ?regionid AND parametername LIKE ?parametername", connection))
                    {
                        cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                        cmd.Parameters.AddWithValue("?parametername", parameter);
                        if(cmd.ExecuteNonQuery() >= 1)
                        {
                            result = true;
                        }
                    }
                    m_Cache[regionID].Remove(parameter);
                });
            }

            return result;
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
    public class MySQLServerParamServiceFactory : IPluginFactory
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
