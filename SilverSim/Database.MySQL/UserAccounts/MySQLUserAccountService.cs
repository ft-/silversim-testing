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
    class MySQLUserAccountService :UserAccountServiceInterface, IDBServiceInterface, IPlugin
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
                            cmd.Parameters.AddWithValue("?scopeid", scopeID);
                            cmd.Parameters.AddWithValue("?id", accountID);
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
                            cmd.Parameters.AddWithValue("?id", accountID);
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
                        cmd.Parameters.AddWithValue("?scopeid", scopeID);
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
                            cmd.Parameters.AddWithValue("?scopeid", scopeID);
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

        public override List<UserAccount> GetAccounts(UUID ScopeID, string query)
        {
            throw new System.NotImplementedException();
        }
    }
    #endregion

    #region Factory
    [PluginName("UserAccounts")]
    class MySQLUserAccountServiceFactory : IPluginFactory
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
