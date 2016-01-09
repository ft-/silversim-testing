// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.Types;
using SilverSim.Types.GridUser;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Database.MySQL.GridUser
{
    #region Service Implementation
    [Description("MySQL GridUser Backend")]
    public sealed class MySQLGridUserService : GridUserServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL GRIDUSER SERVICE");

        #region Constructor
        public MySQLGridUserService(string connectionString)
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

        static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("griduser"),
            new AddColumn<UUID>("ID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("HomeRegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("HomePosition") { Cardinality = 64, IsNullAllowed = false, Default = "<0,0,0>" },
            new AddColumn<string>("HomeLookAt") { Cardinality = 64, IsNullAllowed = false, Default = "<0,0,0>" },
            new AddColumn<UUID>("LastRegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("LastPosition") { Cardinality = 64, IsNullAllowed = false, Default = "<0,0,0>" },
            new AddColumn<string>("LastLookAt") { Cardinality = 64, IsNullAllowed = false, Default = "<0,0,0>" },
            new AddColumn<bool>("IsOnline") { IsNullAllowed = false, Default = false },
            new AddColumn<Date>("LastLogin") {IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new AddColumn<Date>("LastLogout") {IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new PrimaryKeyInfo(new string[] { "ID" }),
            new NamedKeyInfo("LastRegionID", new string[] { "LastRegionID" }),
            new TableRevision(2),
            new ChangeColumn<Vector3>("HomePosition") { IsNullAllowed = false, Default = Vector3.Zero },
            new ChangeColumn<Vector3>("HomeLookAt") { IsNullAllowed = false, Default = Vector3.Zero },
            new ChangeColumn<Vector3>("LastPosition") { IsNullAllowed = false, Default = Vector3.Zero },
            new ChangeColumn<Vector3>("LastLookAt") { IsNullAllowed = false, Default = Vector3.Zero },
            new ChangeColumn<bool>("IsOnline") { IsNullAllowed = false, Default = false },
        };

        #region GridUserServiceInterface
        public override bool TryGetValue(UUID userID, out GridUserInfo userInfo)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM griduser WHERE ID LIKE ?id", conn))
                {
                    cmd.Parameters.AddWithValue("?id", userID.ToString());
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            userInfo = dbReader.ToGridUser();
                            return true;
                        }
                    }
                }
            }
            userInfo = default(GridUserInfo);
            return false;
        }

        public override GridUserInfo this[UUID userID]
        {
            get 
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM griduser WHERE ID LIKE ?id", conn))
                    {
                        cmd.Parameters.AddWithValue("?id", userID.ToString());
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                return dbReader.ToGridUser();
                            }
                        }
                    }
                }
                throw new GridUserNotFoundException();
            }
        }


        public override bool TryGetValue(UUI userID, out GridUserInfo gridUserInfo)
        {
            return TryGetValue(userID.ID, out gridUserInfo);
        }

        public override GridUserInfo this[UUI userID]
        {
            get 
            {
                return this[userID.ID];
            }
        }

        public override void LoggedInAdd(UUI userID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("UPDATE griduser SET IsOnline = 1, LastLogin = ?curtime WHERE ID LIKE ?id", conn))
                {
                    cmd.Parameters.AddWithValue("?id", userID.ID.ToString());
                    cmd.Parameters.AddWithValue("?curtime", Date.GetUnixTime());
                    if (cmd.ExecuteNonQuery() >= 1)
                    {
                        return;
                    }
                }

                Dictionary<string, object> param = new Dictionary<string,object>();
                param["ID"] = userID.ID.ToString();
                param["LastLogin"] = Date.GetUnixTime();
                param["IsOnline"] = 1;
                conn.ReplaceInto("griduser", param);
            }

        }

        public override void LoggedIn(UUI userID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("UPDATE griduser SET IsOnline = 1, LastLogin = ?curtime WHERE ID LIKE ?id", conn))
                {
                    cmd.Parameters.AddWithValue("?id", userID.ID.ToString());
                    cmd.Parameters.AddWithValue("?curtime", Date.GetUnixTime());
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new GridUserUpdateFailedException();
                    }
                }
            }
        }

        public override void LoggedOut(UUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("IsOnline", false);
                data.Add("LastLogout", Date.Now);
                data.Add("LastRegionID", lastRegionID);
                data.Add("LastPosition", lastPosition);
                data.Add("LastLookAt", lastLookAt);
                Dictionary<string, object> where = new Dictionary<string, object>();
                where.Add("ID", userID.ID);
                conn.UpdateSet("griduser", data, where);
            }
        }

        public override void SetHome(UUI userID, UUID homeRegionID, Vector3 homePosition, Vector3 homeLookAt)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("HomeRegionID", homeRegionID);
                data.Add("HomePosition", homePosition);
                data.Add("HomeLookAt", homeLookAt);
                Dictionary<string, object> where = new Dictionary<string, object>();
                where.Add("ID", userID.ID);
                conn.UpdateSet("griduser", data, where);
            }
        }

        public override void SetPosition(UUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("LastRegionID", lastRegionID);
                data.Add("LastPosition", lastPosition);
                data.Add("LastLookAt", lastLookAt);
                Dictionary<string, object> where = new Dictionary<string, object>();
                where.Add("ID", userID.ID);
                conn.UpdateSet("griduser", data, where);
            }
        }
        #endregion

        public void Remove(UUID scopeID, UUID userAccount)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM griduser WHERE ID LIKE ?id", conn))
                {
                    cmd.Parameters.AddWithValue("?id", userAccount.ToString());
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("GridUser")]
    public class MySQLGridUserServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL GRIDUSER SERVICE");
        public MySQLGridUserServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLGridUserService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion

}
