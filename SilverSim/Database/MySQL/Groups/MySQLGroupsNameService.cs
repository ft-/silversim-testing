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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.ServiceInterfaces.ServerParam;
using log4net;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Groups;
using MySql.Data.MySqlClient;
using SilverSim.Types;
using ThreadedClasses;
using Nini.Config;

namespace SilverSim.Database.MySQL.Groups
{
    #region Service Implementation
    public class MySQLGroupsNameService : GroupsNameServiceInterface, IDBServiceInterface, IPlugin
    {
        string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL GROUP NAMES SERVICE");

        #region Constructor
        public MySQLGroupsNameService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
        #endregion

        #region Accessors
        public override UGI this[UUID groupID]
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM groupnames WHERE GroupID LIKE '" + groupID.ToString() + "'", connection))
                    {
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                return new UGI(new UUID((string)dbReader["GroupID"]), (string)dbReader["GroupName"], new Uri((string)dbReader["HomeURI"]));
                            }
                        }
                    }
                }
                throw new KeyNotFoundException();
            }
        }

        public override List<UGI> GetGroupsByName(string groupName, int limit)
        {
            List<UGI> groups = new List<UGI>();
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();

                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM groupnames WHERE GroupName LIKE ?groupName LIMIT ?limit", connection))
                {
                    cmd.Parameters.AddWithValue("?groupName", groupName);
                    cmd.Parameters.AddWithValue("?limit", limit);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while(dbReader.Read())
                        {
                            groups.Add(new UGI(new UUID((string)dbReader["GroupID"]), (string)dbReader["GroupName"], new Uri((string)dbReader["HomeURI"])));
                        }
                    }
                }
            }
            return groups;
        }

        public override void Store(UGI group)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();

                using (MySqlCommand cmd = new MySqlCommand("REPLACE INTO groupnames (GroupID, HomeURI, GroupName) VALUES (?groupID, ?homeURI, ?groupName)", connection))
                {
                    cmd.Parameters.AddWithValue("?groupID", group.ID);
                    cmd.Parameters.AddWithValue("?homeURI", group.HomeURI);
                    cmd.Parameters.AddWithValue("?groupName", group.GroupName);
                    cmd.ExecuteNonQuery();
                }
            }
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
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "groupnames", Migrations, m_Log);
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "GroupID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "HomeURI VARCHAR(255)," +
                "GroupName VARCHAR(255)," +
                "PRIMARY KEY(groupID, homeURI))"
        };
    }
    #endregion

    #region Factory
    public class MySQLGroupsNameServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL GROUP NAMES SERVICE");
        public MySQLGroupsNameServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLGroupsNameService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
