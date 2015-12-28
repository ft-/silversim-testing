// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Database.MySQL.Groups
{
    #region Service Implementation
    [Description("MySQL GroupsName Backend")]
    public sealed class MySQLGroupsNameService : GroupsNameServiceInterface, IDBServiceInterface, IPlugin
    {
        readonly string m_ConnectionString;
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
                UGI ugi;
                if(!TryGetValue(groupID, out ugi))
                {
                    throw new KeyNotFoundException();
                }
                return ugi;
            }
        }

        public override bool TryGetValue(UUID groupID, out UGI ugi)
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
                            ugi = new UGI(new UUID((string)dbReader["GroupID"]), (string)dbReader["GroupName"], new Uri((string)dbReader["HomeURI"]));
                            return true;
                        }
                    }
                }
            }
            ugi = default(UGI);
            return false;
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
                    cmd.Parameters.AddWithValue("?groupID", group.ID.ToString());
                    cmd.Parameters.AddWithValue("?homeURI", group.HomeURI.ToString());
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
    [PluginName("GroupNames")]
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
