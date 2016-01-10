// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using SilverSim.Types.Account;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.UserAccounts
{
    #region Service Implementation
    [Description("MySQL UserAccount Backend")]
    public sealed class MySQLUserAccountService : UserAccountServiceInterface, IDBServiceInterface, IPlugin
    {
        readonly string m_ConnectionString;
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
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.MigrateTables(Migrations, m_Log);
            }
        }

        static IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("useraccounts"),
            new AddColumn<UUID>("ID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("ScopeID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("FirstName") { Cardinality = 31, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("LastName") { Cardinality = 31, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("Email") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<Date>("Created") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new AddColumn<int>("UserLevel") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("UserFlags") { IsNullAllowed = false, Default = 0 },
            new AddColumn<string>("UserTitle") { Cardinality = 64, IsNullAllowed = false, Default = string.Empty },
            new PrimaryKeyInfo("ID"),
            new NamedKeyInfo("Email", "Email"),
            new NamedKeyInfo("Name", "FirstName", "LastName") { IsUnique = true },
            new NamedKeyInfo("FirstName", "FirstName"),
            new NamedKeyInfo("LastName", "LastName"),
            new TableRevision(2),
            new ChangeColumn<uint>("UserFlags") { IsNullAllowed = false, Default = (uint)0 }
        };

        public override bool ContainsKey(UUID scopeID, UUID accountID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                if (scopeID != UUID.Zero)
                {
                    using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM useraccounts WHERE ScopeID LIKE ?scopeid AND ID LIKE ?id", connection))
                    {
                        cmd.Parameters.AddParameter("?scopeid", scopeID);
                        cmd.Parameters.AddParameter("?id", accountID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM useraccounts WHERE ID LIKE ?id", connection))
                    {
                        cmd.Parameters.AddParameter("?id", accountID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public override bool TryGetValue(UUID scopeID, UUID accountID, out UserAccount account)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                if (scopeID != UUID.Zero)
                {
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM useraccounts WHERE ScopeID LIKE ?scopeid AND ID LIKE ?id", connection))
                    {
                        cmd.Parameters.AddParameter("?scopeid", scopeID);
                        cmd.Parameters.AddParameter("?id", accountID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                account = reader.ToUserAccount();
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM useraccounts WHERE ID LIKE ?id", connection))
                    {
                        cmd.Parameters.AddParameter("?id", accountID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                account = reader.ToUserAccount();
                                return true;
                            }
                        }
                    }
                }
            }

            account = default(UserAccount);
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override UserAccount this[UUID scopeID, UUID accountID]
        {
            get
            {
                UserAccount account;
                if (!TryGetValue(scopeID, accountID, out account))
                {
                    throw new UserAccountNotFoundException();
                }
                return account;
            }
        }

        public override bool ContainsKey(UUID scopeID, string email)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT ScopeID FROM useraccounts WHERE ScopeID LIKE ?scopeid AND Email LIKE ?email", connection))
                {
                    cmd.Parameters.AddParameter("?scopeid", scopeID);
                    cmd.Parameters.AddParameter("?email", email);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override bool TryGetValue(UUID scopeID, string email, out UserAccount account)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM useraccounts WHERE ScopeID LIKE ?scopeid AND Email LIKE ?email", connection))
                {
                    cmd.Parameters.AddParameter("?scopeid", scopeID);
                    cmd.Parameters.AddParameter("?email", email);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            account = reader.ToUserAccount();
                            return true;
                        }
                    }
                }
            }

            account = default(UserAccount);
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override UserAccount this[UUID scopeID, string email]
        {
            get 
            {
                UserAccount account;
                if(!TryGetValue(scopeID, email, out account))
                {
                    throw new UserAccountNotFoundException();
                }
                return account;
            }
        }

        public override bool ContainsKey(UUID scopeID, string firstName, string lastName)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                if (scopeID != UUID.Zero)
                {
                    using (MySqlCommand cmd = new MySqlCommand("SELECT ScopeID FROM useraccounts WHERE ScopeID LIKE ?scopeid AND FirstName LIKE ?firstname AND LastName LIKE ?lastname", connection))
                    {
                        cmd.Parameters.AddParameter("?scopeid", scopeID);
                        cmd.Parameters.AddParameter("?firstname", firstName);
                        cmd.Parameters.AddParameter("?lastname", lastName);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    using (MySqlCommand cmd = new MySqlCommand("SELECT ScopeID FROM useraccounts WHERE FirstName LIKE ?firstname AND LastName LIKE ?lastname", connection))
                    {
                        cmd.Parameters.AddParameter("?firstname", firstName);
                        cmd.Parameters.AddParameter("?lastname", lastName);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public override bool TryGetValue(UUID scopeID, string firstName, string lastName, out UserAccount account)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                if (scopeID != UUID.Zero)
                {
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM useraccounts WHERE ScopeID LIKE ?scopeid AND FirstName LIKE ?firstname AND LastName LIKE ?lastname", connection))
                    {
                        cmd.Parameters.AddParameter("?scopeid", scopeID);
                        cmd.Parameters.AddParameter("?firstname", firstName);
                        cmd.Parameters.AddParameter("?lastname", lastName);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                account = reader.ToUserAccount();
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM useraccounts WHERE FirstName LIKE ?firstname AND LastName LIKE ?lastname", connection))
                    {
                        cmd.Parameters.AddParameter("?firstname", firstName);
                        cmd.Parameters.AddParameter("?lastname", lastName);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                account = reader.ToUserAccount();
                                return true;
                            }
                        }
                    }
                }
            }

            account = default(UserAccount);
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override UserAccount this[UUID scopeID, string firstName, string lastName]
        {
            get 
            {
                UserAccount account;
                if(!TryGetValue(scopeID, firstName, lastName, out account))
                {
                    throw new UserAccountNotFoundException();
                }
                return account;
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
                    cmd.Parameters.AddParameter("?ScopeID", scopeID);
                    for (int i = 0; i < words.Length; ++i)
                    {
                        cmd.Parameters.AddParameter("?word" + i.ToString(), words[i]);
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
            data["ID"] = userAccount.Principal.ID;
            data["ScopeID"] = userAccount.ScopeID;
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
            w["ScopeID"] = userAccount.ScopeID;
            w["ID"] = userAccount.Principal.ID;

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
                    cmd.Parameters.AddParameter("?id", accountID);
                    cmd.Parameters.AddParameter("?scopeid", scopeID);
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
