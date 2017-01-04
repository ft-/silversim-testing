// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using Nini.Config;
using log4net;
using System.ComponentModel;

namespace SilverSim.Database.MySQL.UserAccounts
{
    [Description("MySQL UserAccount AvatarName backend")]
    public class MySQLUserAccountNameService : AvatarNameServiceInterface, IPlugin, IDBServiceInterface
    {
        readonly string m_ConnectionString;

        public MySQLUserAccountNameService(string connectionString)
        {
            m_ConnectionString = connectionString;
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

        public override void Store(UUI uui)
        {
            /* intentionally ignored */
        }

        public override bool Remove(UUID key)
        {
            return false;
        }

        public override UUI this[string firstName, string lastName]
        {
            get
            {
                UUI uui;
                if(!TryGetValue(firstName,lastName, out uui))
                {
                    throw new KeyNotFoundException();
                }
                return uui;
            }
        }

        public void ProcessMigrations()
        {
            /* intentionally left empty */
        }

        public override List<UUI> Search(string[] names)
        {
            List<UUI> list = new List<UUI>();

            if (names.Length == 1)
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM useraccounts WHERE FirstName LIKE ?name AND LastName LIKE ?name", connection))
                    {
                        cmd.Parameters.AddParameter("?name", "%" + names[0] + "%");
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                list.Add(GetUUIFromReader(reader));
                            }
                        }
                    }
                }
            }
            else if(names.Length == 2)
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM useraccounts WHERE FirstName LIKE ?name0 AND LastName LIKE ?name1", connection))
                    {
                        cmd.Parameters.AddParameter("?name0", "%" + names[0] + "%");
                        cmd.Parameters.AddParameter("?name1", "%" + names[1] + "%");
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add(GetUUIFromReader(reader));
                            }
                        }
                    }
                }
            }
            return list;
        }

        static UUI GetUUIFromReader(MySqlDataReader reader)
        {
            UUI uui = new UUI();
            uui.FirstName = reader.GetString("FirstName");
            uui.LastName = reader.GetString("LastName");
            uui.ID = reader.GetUUID("ID");
            uui.IsAuthoritative = true;
            return uui;
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override bool TryGetValue(UUID key, out UUI uui)
        {
            uui = null;
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT ID, FirstName, LastName FROM useraccounts WHERE ID LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", key);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            uui = GetUUIFromReader(reader);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool TryGetValue(string firstName, string lastName, out UUI uui)
        {
            uui = null;
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT ID, FirstName, LastName FROM useraccounts WHERE FirstName LIKE ?first AND LastName LIKE ?last", connection))
                {
                    cmd.Parameters.AddParameter("?first", firstName);
                    cmd.Parameters.AddParameter("?last", lastName);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            uui = GetUUIFromReader(reader);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }
    }

    [PluginName("UserAccountNames")]
    public class MySQLUserAccountNameServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL USERACCOUNTNAME SERVICE");

        public MySQLUserAccountNameServiceFactory()
        {
            /* intentionally left empty */
        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLUserAccountNameService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
}