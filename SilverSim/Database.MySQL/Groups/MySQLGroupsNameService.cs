// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
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
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.MigrateTables(Migrations, m_Log);
            }
        }

        static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("groupnames"),
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("HomeURI") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("GroupName") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new PrimaryKeyInfo(new string[] { "GroupID", "HomeURI" }),
            new TableRevision(2),
            /* some corrections when revision 1 is found */
            new ChangeColumn<string>("HomeURI") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new ChangeColumn<string>("GroupName") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
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
