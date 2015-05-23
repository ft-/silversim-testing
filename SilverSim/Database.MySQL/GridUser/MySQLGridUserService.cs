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
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.Types;
using SilverSim.Types.GridUser;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.GridUser
{
    #region Service Implementation
    class MySQLGridUserService : GridUserServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        string m_ConnectionString;
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
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "griduser", Migrations, m_Log);
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "ID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "HomeRegionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "HomePosition CHAR(64) NOT NULL DEFAULT '<0,0,0>'," +
                "HomeLookAt CHAR(64) NOT NULL DEFAULT '<0,0,0>'," +
                "LastRegionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "LastPosition CHAR(64) NOT NULL DEFAULT '<0,0,0>'," +
                "LastLookAt CHAR(64) NOT NULL DEFAULT '<0,0,0>'," +
                "IsOnline TINYINT(1) NOT NULL DEFAULT '0'," +
                "LastLogin BIGINT(20) NOT NULL DEFAULT '0'," +
                "LastLogout BIGINT(20) NOT NULL DEFAULT '0'," +
                "PRIMARY KEY(ID)," +
                "KEY LastRegionID (LastRegionID))"
        };

        #region GridUserServiceInterface
        public override GridUserInfo this[UUID userID]
        {
            get 
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM griduser WHERE ID LIKE ?id", conn))
                    {
                        cmd.Parameters.AddWithValue("?id", userID);
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
                    cmd.Parameters.AddWithValue("?id", userID.ID);
                    cmd.Parameters.AddWithValue("?curtime", Date.GetUnixTime());
                    if (cmd.ExecuteNonQuery() >= 1)
                    {
                        return;
                    }
                }

                Dictionary<string, object> param = new Dictionary<string,object>();
                param["ID"] = userID.ID;
                param["LastLogin"] = Date.GetUnixTime();
                param["IsOnline"] = 1;
                conn.ReplaceInsertInto("griduser", param);
            }

        }

        public override void LoggedIn(UUI userID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("UPDATE griduser SET IsOnline = 1, LastLogin = ?curtime WHERE ID LIKE ?id", conn))
                {
                    cmd.Parameters.AddWithValue("?id", userID.ID);
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
                using (MySqlCommand cmd = new MySqlCommand("UPDATE griduser SET IsOnline = 0, LastLogout = ?curtime, LastRegionID = ?regionID, LastPosition = ?position, LastLookAt = ?lookAt WHERE ID LIKE ?id", conn))
                {
                    cmd.Parameters.AddWithValue("?id", userID.ID);
                    cmd.Parameters.AddWithValue("?curtime", Date.GetUnixTime());
                    cmd.Parameters.AddWithValue("?regionID", lastRegionID);
                    cmd.Parameters.AddWithValue("?position", lastPosition.ToString());
                    cmd.Parameters.AddWithValue("?lookAt", lastLookAt.ToString());
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new GridUserUpdateFailedException();
                    }
                }
            }
        }

        public override void SetHome(UUI userID, UUID homeRegionID, Vector3 homePosition, Vector3 homeLookAt)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("UPDATE griduser SET HomeRegionID = ?regionID, HomePosition = ?position, HomeLookAt = ?lookAt WHERE ID LIKE ?id", conn))
                {
                    cmd.Parameters.AddWithValue("?id", userID.ID);
                    cmd.Parameters.AddWithValue("?regionID", homeRegionID);
                    cmd.Parameters.AddWithValue("?position", homePosition.ToString());
                    cmd.Parameters.AddWithValue("?lookAt", homeLookAt.ToString());
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new GridUserUpdateFailedException();
                    }
                }
            }
        }

        public override void SetPosition(UUI userID, UUID lastRegionID, Vector3 lastPosition, Vector3 lastLookAt)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("UPDATE griduser SET LastRegionID = ?regionID, LastPosition = ?position, LastLookAt = ?lookAt WHERE ID LIKE ?id", conn))
                {
                    cmd.Parameters.AddWithValue("?id", userID.ID);
                    cmd.Parameters.AddWithValue("?regionID", lastRegionID);
                    cmd.Parameters.AddWithValue("?position", lastPosition.ToString());
                    cmd.Parameters.AddWithValue("?lookAt", lastLookAt.ToString());
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new GridUserUpdateFailedException();
                    }
                }
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
                    cmd.Parameters.AddWithValue("?id", userAccount);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("GridUser")]
    class MySQLGridUserServiceFactory : IPluginFactory
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
