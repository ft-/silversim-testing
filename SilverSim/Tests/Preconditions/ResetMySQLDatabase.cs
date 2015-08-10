// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using System.Collections.Generic;

namespace SilverSim.Tests.Preconditions
{
    #region Precondition Implementation
    class ResetMySQLDatabase : IPlugin, IDBServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL DATABASE RESET");
        string m_ConnectionString;

        public ResetMySQLDatabase(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        public void VerifyConnection()
        {
            List<string> tables = new List<string>();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                m_Log.Info("Executing reset database");
                using(MySqlCommand cmd = new MySqlCommand("SHOW TABLES", connection))
                {
                    using(MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            tables.Add((string)reader.GetValue(0));
                        }
                    }
                }

                m_Log.InfoFormat("Deleting {0} tables", tables.Count);
                foreach (string table in tables)
                {
                    m_Log.InfoFormat("Deleting table {0}", table);
                    using (MySqlCommand cmd = new MySqlCommand(string.Format("DROP TABLE {0}", table), connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void ProcessMigrations()
        {
        }
    }
    #endregion

    #region Factory
    [PluginName("ResetMySQLDatabase")]
    class ResetMySQLDatabaseFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL DATABASE RESET");
        public ResetMySQLDatabaseFactory()
        {

        }

        #region Connection String Creator
        string BuildConnectionString(IConfig config, ILog log)
        {
            if (!(config.Contains("Server") && config.Contains("Username") && config.Contains("Password") && config.Contains("Database")))
            {
                if (!config.Contains("Server"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Server' missing in [{0}]", config.Name);
                }
                if (!config.Contains("Username"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Username' missing in [{0}]", config.Name);
                }
                if (!config.Contains("Password"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Password' missing in [{0}]", config.Name);
                }
                if (!config.Contains("Database"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Database' missing in [{0}]", config.Name);
                }
                throw new ConfigurationLoader.ConfigurationError();
            }
            return string.Format("Server={0};Uid={1};Pwd={2};Database={3};",
                config.GetString("Server"),
                config.GetString("Username"),
                config.GetString("Password"),
                config.GetString("Database"));
        }
        #endregion

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ResetMySQLDatabase(BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
