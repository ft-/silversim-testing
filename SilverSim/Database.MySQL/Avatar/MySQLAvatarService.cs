// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Avatar;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Database.MySQL.Avatar
{
    #region Service Implementation
    public sealed class MySQLAvatarService : AvatarServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        string m_ConnectionString;
        static readonly ILog m_Log = LogManager.GetLogger("MYSQL AVATAR SERVICE");

        public MySQLAvatarService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        public override Dictionary<string, string> this[UUID avatarID]
        {
            get
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT `Name`,`Value` FROM avatars WHERE PrincipalID LIKE ?principalid", connection))
                    {
                        cmd.Parameters.AddWithValue("?principalid", avatarID.ToString());
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            while (dbReader.Read())
                            {
                                result.Add((string)dbReader["Name"], (string)dbReader["Value"]);
                            }
                        }
                    }
                }

                return result;
            }
            set
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    if (null == value)
                    {
                        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM avatars WHERE PrincipalID LIKE ?principalid", connection))
                        {
                            cmd.Parameters.AddWithValue("?principalid", avatarID.ToString());
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        connection.InsideTransaction(delegate()
                        {
                            using (MySqlCommand cmd = new MySqlCommand("DELETE FROM avatars WHERE PrincipalID LIKE ?principalid", connection))
                            {
                                cmd.Parameters.AddWithValue("?principalid", avatarID.ToString());
                                cmd.ExecuteNonQuery();
                            }

                            Dictionary<string, object> vals = new Dictionary<string, object>();
                            vals["PrincipalID"] = avatarID.ToString();
                            foreach (KeyValuePair<string, string> kvp in value)
                            {
                                vals["Name"] = kvp.Key;
                                vals["Value"] = kvp.Value;
                                connection.ReplaceInsertInto("avatars", vals);
                            }
                        });
                    }
                }
            }
        }

        public override List<string> this[UUID avatarID, IList<string> itemKeys]
        {
            get
            {
                List<string> result = new List<string>();
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    connection.InsideTransaction(delegate()
                    {
                        foreach (string key in itemKeys)
                        {
                            using (MySqlCommand cmd = new MySqlCommand("SELECT `Value` FROM avatars WHERE PrincipalID LIKE ?principalid AND `Name` LIKE ?name", connection))
                            {
                                cmd.Parameters.AddWithValue("?principalid", avatarID.ToString());
                                cmd.Parameters.AddWithValue("?name", key);
                                using (MySqlDataReader dbReader = cmd.ExecuteReader())
                                {
                                    if (dbReader.Read())
                                    {
                                        result.Add((string)dbReader["Value"]);
                                    }
                                    else
                                    {
                                        result.Add(string.Empty);
                                    }
                                }
                            }
                        }
                    });
                }
                return result;
            }

            set
            {
                if (value == null || itemKeys == null)
                {
                    throw new ArgumentNullException();
                }
                if (itemKeys.Count != value.Count)
                {
                    throw new ArgumentException("value and itemKeys must have identical Count");
                }

                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    Dictionary<string, object> vals = new Dictionary<string, object>();
                    vals["PrincipalID"] = avatarID.ToString();

                    connection.InsideTransaction(delegate()
                    {
                        for (int i = 0; i < itemKeys.Count; ++i)
                        {
                            vals["Name"] = itemKeys[i];
                            vals["Value"] = value[i];
                            connection.ReplaceInsertInto("avatars", vals);
                        }
                    });
                }
            }
        }

        public override string this[UUID avatarID, string itemKey]
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT `Value` FROM avatars WHERE PrincipalID LIKE ?principalid AND `Name` LIKE ?name", connection))
                    {
                        cmd.Parameters.AddWithValue("?principalid", avatarID.ToString());
                        cmd.Parameters.AddWithValue("?name", itemKey);
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                return (string)dbReader["Value"];
                            }
                        }
                    }
                }

                throw new KeyNotFoundException(string.Format("{0},{1} not found", avatarID, itemKey));
            }
            set
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    Dictionary<string, object> vals = new Dictionary<string, object>();
                    vals["PrincipalID"] = avatarID.ToString();
                    vals["Name"] = itemKey;
                    vals["Value"] = value;
                    connection.ReplaceInsertInto("avatars", vals);
                }
            }
        }

        public override void Remove(UUID avatarID, IList<string> nameList)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction(delegate()
                {
                    foreach (string name in nameList)
                    {
                        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM avatars WHERE PrincipalID LIKE ?principalid AND `Name` LIKE ?name", connection))
                        {
                            cmd.Parameters.AddWithValue("?principalid", avatarID.ToString());
                            cmd.Parameters.AddWithValue("?name", name);
                            cmd.ExecuteNonQuery();
                        }
                    }
                });
            }
        }

        public override void Remove(UUID avatarID, string name)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM avatars WHERE PrincipalID LIKE ?principalid AND `Name` LIKE ?name", connection))
                {
                    cmd.Parameters.AddWithValue("?principalid", avatarID.ToString());
                    cmd.Parameters.AddWithValue("?name", name);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "avatars", Migrations_avatars, m_Log);
        }

        private static readonly string[] Migrations_avatars = new string[]{
            "CREATE TABLE %tablename% (" +
                "PrincipalID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "Name VARCHAR(32) NOT NULL DEFAULT ''," +
                "Value TEXT," +
                "PRIMARY KEY(PrincipalID, Name)," +
                "KEY avatars_principalid (PrincipalID))"
        };

        public void Remove(UUID scopeID, UUID userAccount)
        {
            this[userAccount] = null;
        }
    }
    #endregion

    #region Factory
    [PluginName("Avatar")]
    public class MySQLInventoryServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL AVATAR SERVICE");
        public MySQLInventoryServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLAvatarService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
