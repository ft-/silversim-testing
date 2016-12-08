// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.AuthInfo;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using SilverSim.Types.AuthInfo;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.AuthInfo
{
    #region Service implementation
    public class MySQLAuthInfoService : AuthInfoServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL AUTHINFO SERVICE");
        readonly string m_ConnectionString;

        public MySQLAuthInfoService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }


        static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("auth"),
            new AddColumn<UUID>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("PasswordHash") { Cardinality = 32, IsFixed = true, IsNullAllowed = false },
            new AddColumn<string>("PasswordSalt") { Cardinality = 32, IsFixed = true, IsNullAllowed = false },
            new PrimaryKeyInfo("UserID"),
            new SqlTable("tokens"),
            new AddColumn<UUID>("UserID") { IsNullAllowed = false },
            new AddColumn<UUID>("Token") { IsNullAllowed = false },
            new AddColumn<UUID>("SessionID") { IsNullAllowed = false },
            new AddColumn<Date>("Validity") { IsNullAllowed = false },
            new PrimaryKeyInfo("UserID", "Token"),
            new NamedKeyInfo("TokenIndex", "Token"),
            new NamedKeyInfo("UserIDIndex", "UserID"),
            new NamedKeyInfo("UserIDSessionID", "UserID", "SessionID") { IsUnique = true },
            new NamedKeyInfo("SessionIDIndex", "SessionID") { IsUnique = true },
        };

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
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

        public void Remove(UUID scopeID, UUID accountID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction(delegate ()
                {
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM auth WHERE UserID LIKE ?id", connection))
                    {
                        cmd.Parameters.AddParameter("?id", accountID);
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM tokens WHERE UserID LIKE ?id", connection))
                    {
                        cmd.Parameters.AddParameter("?id", accountID);
                        cmd.ExecuteNonQuery();
                    }
                });
            }
        }

        public override UserAuthInfo this[UUID accountid]
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM auth WHERE UserID LIKE ?id", connection))
                    {
                        cmd.Parameters.AddParameter("?id", accountid);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                UserAuthInfo authInfo = new UserAuthInfo();
                                authInfo.ID = reader.GetUUID("UserID");
                                authInfo.PasswordHash = reader.GetString("PasswordHash");
                                authInfo.PasswordSalt = reader.GetString("PasswordSalt");
                                return authInfo;
                            }
                        }
                    }
                }
                throw new KeyNotFoundException();
            }
        }

        public override void Store(UserAuthInfo info)
        {
            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add("UserID", info.ID);
            vals.Add("PasswordHash", info.PasswordHash);
            vals.Add("PasswordSalt", info.PasswordSalt);
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.ReplaceInto("auth", vals);
            }
        }

        public override UUID AddToken(UUID principalId, UUID sessionid, int lifetime_in_minutes)
        {
            UUID secureSessionID = UUID.Random;
            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add("UserID", principalId);
            vals.Add("SessionID", sessionid);
            vals.Add("Token", secureSessionID);
            ulong d = Date.Now.AsULong + (ulong)lifetime_in_minutes * 30;
            vals.Add("Validity", Date.UnixTimeToDateTime(d));
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsertInto("tokens", vals);
            }
            return secureSessionID;
        }

        public override void VerifyToken(UUID principalId, UUID token, int lifetime_extension_in_minutes)
        {
            bool valid;
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("UPDATE tokens SET Validity=?validity WHERE UserID LIKE ?id AND Token LIKE ?token AND Validity >= ? current", connection))
                {
                    cmd.Parameters.AddParameter("?id", principalId);
                    cmd.Parameters.AddParameter("?validity", Date.UnixTimeToDateTime(Date.Now.AsULong + (ulong)lifetime_extension_in_minutes * 30));
                    cmd.Parameters.AddParameter("?token", token);
                    cmd.Parameters.AddParameter("?current", Date.Now);
                    valid = cmd.ExecuteNonQuery() > 0;
                }
                if (!valid)
                {
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM tokens WHERE Validity <= ? current", connection))
                    {
                        cmd.Parameters.AddParameter("?current", Date.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            if(!valid)
            {
                throw new VerifyTokenFailedException();
            }
        }

        public override void ReleaseToken(UUID accountId, UUID secureSessionId)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM tokens WHERE UserID LIKE ?id AND Token LIKE ?token", connection))
                {
                    cmd.Parameters.AddParameter("?id", accountId);
                    cmd.Parameters.AddParameter("?token", secureSessionId);
                    if(cmd.ExecuteNonQuery() < 1)
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        public override void ReleaseTokenBySession(UUID accountId, UUID sessionId)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM tokens WHERE UserID LIKE ?id AND SessionID LIKE ?sessionid", connection))
                {
                    cmd.Parameters.AddParameter("?id", accountId);
                    cmd.Parameters.AddParameter("?sessionid", sessionId);
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
    [PluginName("AuthInfo")]
    public class MySQLAuthInfoServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL AUTHINFO SERVICE");
        public MySQLAuthInfoServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLAuthInfoService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
