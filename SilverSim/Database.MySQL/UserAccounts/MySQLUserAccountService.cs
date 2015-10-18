// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using SilverSim.Types.Account;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.UserAccounts
{
    #region Service Implementation
    public sealed class MySQLUserAccountService : UserAccountServiceInterface, IDBServiceInterface, IPlugin
    {
        string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL USERACCOUNT SERVICE");

        #region Constructor
        public MySQLUserAccountService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
        #endregion

        public void VerifyConnection()
        {
            using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "useraccounts", Migrations, m_Log);
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "ID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "ScopeID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "FirstName VARCHAR(31) NOT NULL DEFAULT ''," +
                "LastName VARCHAR(31) NOT NULL DEFAULT ''," +
                "Email VARCHAR(255) NOT NULL DEFAULT ''," +
                "Created BIGINT(20) NOT NULL DEFAULT '0'," +
                "UserLevel INT(11) NOT NULL DEFAULT '0'," +
                "UserFlags INT(11) NOT NULL DEFAULT '0'," +
                "UserTitle VARCHAR(64) NOT NULL DEFAULT ''," +
                "PRIMARY KEY(ID), KEY Email (Email), UNIQUE KEY Name (FirstName, LastName), KEY FirstName (FirstName), KEY LastName (LastName))"
        };

        public override UserAccount this[UUID scopeID, UUID accountID]
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    if (scopeID != UUID.Zero)
                    {
                        using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM useraccounts WHERE ScopeID LIKE ?scopeid AND ID LIKE ?id", connection))
                        {
                            cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                            cmd.Parameters.AddWithValue("?id", accountID.ToString());
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    return reader.ToUserAccount();
                                }
                            }
                        }
                    }
                    else
                    {
                        using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM useraccounts WHERE ID LIKE ?id", connection))
                        {
                            cmd.Parameters.AddWithValue("?id", accountID.ToString());
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    return reader.ToUserAccount();
                                }
                            }
                        }
                    }
                }
                throw new UserAccountNotFoundException();
            }
        }

        public override UserAccount this[UUID scopeID, string email]
        {
            get 
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM useraccounts WHERE ScopeID LIKE ?scopeid AND Email LIKE ?email", connection))
                    {
                        cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                        cmd.Parameters.AddWithValue("?email", email);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return reader.ToUserAccount();
                            }
                        }
                    }
                }
                throw new UserAccountNotFoundException();
            }
        }

        public override UserAccount this[UUID scopeID, string firstName, string lastName]
        {
            get 
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    if (scopeID != UUID.Zero)
                    {
                        using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM useraccounts WHERE ScopeID LIKE ?scopeid AND FirstName LIKE ?firstname AND LastName LIKE ?lastname", connection))
                        {
                            cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                            cmd.Parameters.AddWithValue("?firstname", firstName);
                            cmd.Parameters.AddWithValue("?lastname", lastName);
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    return reader.ToUserAccount();
                                }
                            }
                        }
                    }
                    else
                    {
                        using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM useraccounts WHERE FirstName LIKE ?firstname AND LastName LIKE ?lastname", connection))
                        {
                            cmd.Parameters.AddWithValue("?firstname", firstName);
                            cmd.Parameters.AddWithValue("?lastname", lastName);
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    return reader.ToUserAccount();
                                }
                            }
                        }
                    }
                }
                throw new UserAccountNotFoundException();
            }
        }

        public override List<UserAccount> GetAccounts(UUID scopeID, string query)
        {
            string[] words = query.Split(new char[] {' '}, 2);
            List<UserAccount> accounts = new List<UserAccount>();
            if(words.Length == 0)
            {
                return accounts;
            }

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                string cmdstr = "select * from useraccounts where (ScopeID LIKE ?ScopeID or ScopeID LIKE '00000000-0000-0000-0000-000000000000') and (FirstName LIKE ?word0 or LastName LIKE ?word0)";
                if (words.Length == 2)
                {
                    cmdstr = "select * from useraccounts where (ScopeID LIKE ?ScopeID or ScopeID LIKE '00000000-0000-0000-0000-000000000000') and (FirstName LIKE ?word0 or LastName LIKE ?word1)";
                }
                using (MySqlCommand cmd = new MySqlCommand(cmdstr, connection))
                {
                    cmd.Parameters.AddWithValue("?ScopeID", scopeID.ToString());
                    for (int i = 0; i < words.Length; ++i)
                    {
                        cmd.Parameters.AddWithValue("?word" + i, words[i]);
                    }
                    using (MySqlDataReader dbreader = cmd.ExecuteReader())
                    {
                        while (dbreader.Read())
                        {
                            accounts.Add(dbreader.ToUserAccount());
                        }
                    }
                }
            }
            return accounts;
        }

        public override void Add(UserAccount userAccount)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data["ID"] = userAccount.Principal.ID.ToString();
            data["ScopeID"] = userAccount.ScopeID.ToString();
            data["FirstName"] = userAccount.Principal.FirstName;
            data["LastName"] = userAccount.Principal.LastName;
            data["Email"] = userAccount.Email;
            data["Created"] = userAccount.Created;
            data["UserLevel"] = userAccount.UserLevel;
            data["UserFlags"] = userAccount.UserFlags;
            data["UserTitle"] = userAccount.UserTitle;

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsertInto("useraccounts", data);
            }
        }

        public override void Update(UserAccount userAccount)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            data["FirstName"] = userAccount.Principal.FirstName;
            data["LastName"] = userAccount.Principal.LastName;
            data["Email"] = userAccount.Email;
            data["Created"] = userAccount.Created;
            data["UserLevel"] = userAccount.UserLevel;
            data["UserFlags"] = userAccount.UserFlags;
            data["UserTitle"] = userAccount.UserTitle;
            Dictionary<string, object> w = new Dictionary<string,object>();
            w["ScopeID"] = userAccount.ScopeID.ToString();
            w["ID"] = userAccount.Principal.ID.ToString();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.UpdateSet("useraccounts", data, w);
            }
        }

        public override void Remove(UUID scopeID, UUID accountID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM useraccounts WHERE ID LIKE ?id AND ScopeID LIKE ?scopeid", connection))
                {
                    cmd.Parameters.AddWithValue("?id", accountID.ToString());
                    cmd.Parameters.AddWithValue("?scopeid", scopeID.ToString());
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("UserAccounts")]
    public class MySQLUserAccountServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL USERACCOUNT SERVICE");
        public MySQLUserAccountServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLUserAccountService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion

}
