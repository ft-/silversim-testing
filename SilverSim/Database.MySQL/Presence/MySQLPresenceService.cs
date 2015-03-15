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
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using SilverSim.Types.GridUser;
using SilverSim.Types.Presence;
using System;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Presence
{
    #region Service Implementation
    class MySQLPresenceService : PresenceServiceInterface, IDBServiceInterface, IPlugin
    {
        string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL PRESENCE SERVICE");

        #region Constructor
        public MySQLPresenceService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
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
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "presence", Migrations, m_Log);
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "UserID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "RegionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "SessionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "SecureSessionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "LastSeen BIGINT(10) NOT NULL DEFAULT '0'," +
                "PRIMARY KEY(SessionID)," +
                "KEY UserID (UserID)," +
                "KEY SecureSessionID (SecureSessionID)," +
                "KEY RegionID (RegionID))"
        };

        #region PresenceServiceInterface
        public override PresenceInfo this[UUID sessionID, UUID userID]
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set /* setting null means logout, != null not allowed */
            {
                if(value != null)
                {
                    throw new ArgumentException("setting value != null is not allowed without reportType");
                }
                this[sessionID, userID, SetType.Report] = null;
            }
        }

        public override PresenceInfo this[UUID sessionID, UUID userID, PresenceServiceInterface.SetType reportType]
        { 
            /* setting null means logout, != null login message */
            set 
            {
                if (value == null)
                {
                    using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using(MySqlCommand cmd = new MySqlCommand("DELETE FROM presence WHERE SessionID LIKE ?sessionID", conn))
                        {
                            cmd.Parameters.AddWithValue("?sessionID", sessionID);
                            if(cmd.ExecuteNonQuery() < 1)
                            {
                                throw new PresenceUpdateFailedException();
                            }
                        }
                    }
                }
                else if (reportType == SetType.Login)
                {
                    Dictionary<string, object> post = new Dictionary<string, object>();
                    post["UserID"] = value.UserID;
                    post["SessionID"] = value.SessionID;
                    post["SecureSessionID"] = value.SecureSessionID;
                    post["RegionID"] = UUID.Zero;
                    post["LastSeen"] = Date.GetUnixTime().ToString();

                    using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            conn.InsertInto("presence", post);
                        }
                        catch
                        {
                            throw new PresenceUpdateFailedException();
                        }
                    }
                }
                else if (reportType == SetType.Report)
                {
                    using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand("UPDATE presence SET RegionID = ?regionID WHERE SessionID LIKE ?sessionID", conn))
                        {
                            cmd.Parameters.AddWithValue("?regionID", value.RegionID);
                            cmd.Parameters.AddWithValue("?sessionID", value.SessionID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid reportType specified");
                }
            }
        }

        public override void logoutRegion(UUID regionID)
        {
            using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using(MySqlCommand cmd = new MySqlCommand("DELETE FROM presence WHERE RegionID LIKE ?regionid", connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
        #endregion
    }
    #endregion

    #region Factory
    [PluginName("Presence")]
    class MySQLPresenceServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL PRESENCE SERVICE");
        public MySQLPresenceServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLPresenceService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion

}
