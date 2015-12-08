// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types.Estate;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Estate
{
    #region Service Implementation
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public sealed class MySQLEstateService : EstateServiceInterface, IDBServiceInterface, IPlugin
    {
        readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL ESTATE SERVICE");

        readonly MySQLEstateOwnerService m_EstateOwnerService;
        readonly MySQLEstateManagerService m_EstateManagerService;
        readonly MySQLEstateAccessInterface m_EstateAccessService;
        readonly MySQLEstateGroupsService m_EstateGroupsService;
        readonly MySQLEstateRegionMapInterface m_EstateRegionMapService;

        #region Constructor
        public MySQLEstateService(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_EstateOwnerService = new MySQLEstateOwnerService(connectionString);
            m_EstateManagerService = new MySQLEstateManagerService(connectionString);
            m_EstateAccessService = new MySQLEstateAccessInterface(connectionString);
            m_EstateGroupsService = new MySQLEstateGroupsService(connectionString);
            m_EstateRegionMapService = new MySQLEstateRegionMapInterface(connectionString);
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
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "estate_managers", Migrations_estatemanagers, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "estate_groups", Migrations_estategroups, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "estate_users", Migrations_estateusers, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "estates", Migrations_estates, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "estate_regionmap", Migrations_estateregionmap, m_Log);
        }

        private static readonly string[] Migrations_estateregionmap = new string[]{
            "CREATE TABLE %tablename% (" +
                "EstateID INT(10) UNSIGNED NOT NULL," +
                "RegionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',"+
                "KEY EstateID (EstateID)," +
                "PRIMARY KEY (RegionID))"
        };

        private static readonly string[] Migrations_estatemanagers = new string[]{
            "CREATE TABLE %tablename% (" +
                "EstateID INT(10) UNSIGNED NOT NULL," +
                "UserID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "PRIMARY KEY(EstateID, UserID)," +
                "KEY UserID (UserID)," +
                "KEY EstateID (EstateID))"
        };

        private static readonly string[] Migrations_estategroups = new string[]{
            "CREATE TABLE %tablename% (" +
                "EstateID INT(10) UNSIGNED NOT NULL," +
                "GroupID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "PRIMARY KEY(EstateID, GroupID)," +
                "KEY EstateID (EstateID)," +
                "KEY GroupID (GroupID))"
        };

        private static readonly string[] Migrations_estateusers = new string[]{
            "CREATE TABLE %tablename% (" +
                "EstateID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "UserID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "PRIMARY KEY(EstateID, UserID)," +
                "KEY UserID (UserID)," +
                "KEY EstateID (EstateID))"
        };

        private static readonly string[] Migrations_estates = new string[]{
            "CREATE TABLE %tablename% (" +
                "ID INT(11) UNSIGNED NOT NULL AUTO_INCREMENT," + 
                "Name VARCHAR(64) NOT NULL," +
                "OwnerID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "Flags INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "PricePerMeter INT(11) NOT NULL DEFAULT '0'," +
                "BillableFactor double not null default '1'," +
                "SunPosition double not null default '1'," +
                "AbuseEmail VARCHAR(255) NOT NULL DEFAULT ''," +
                "PRIMARY KEY(ID)," +
                "UNIQUE KEY Name (Name)," +
                "KEY Owner (OwnerID)," +
                "KEY ID_OwnerID (ID, OwnerID)) AUTO_INCREMENT=100 ",
            "ALTER TABLE %tablename% ADD COLUMN (CovenantID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                                            "CovenantTimestamp BIGINT(20) UNSIGNED NOT NULL DEFAULT '0'),"
        };

        public override bool TryGetValue(uint estateID, out EstateInfo estateInfo)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM estates WHERE ID LIKE ?id", conn))
                {
                    cmd.Parameters.AddWithValue("?id", estateID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            estateInfo = reader.ToEstateInfo();
                            return true;
                        }
                    }
                }
            }
            estateInfo = default(EstateInfo);
            return false;
        }

        public override bool ContainsKey(uint estateID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT id FROM estates WHERE ID LIKE ?id", conn))
                {
                    cmd.Parameters.AddWithValue("?id", estateID);
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

        public override EstateInfo this[uint estateID]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM estates WHERE ID LIKE ?id", conn))
                    {
                        cmd.Parameters.AddWithValue("?id", estateID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if(reader.Read())
                            {
                                return reader.ToEstateInfo();
                            }
                        }
                    }
                }
                throw new KeyNotFoundException();
            }
            set
            {
                if (value != null)
                {
                    Dictionary<string, object> dict = new Dictionary<string, object>();
                    dict["ID"] = value.ID;
                    dict["Name"] = value.Name;
                    dict["OwnerID"] = value.Owner.ID.ToString();
                    dict["Flags"] = (uint)value.Flags;
                    dict["PricePerMeter"] = value.PricePerMeter;
                    dict["BillableFactor"] = value.BillableFactor;
                    dict["SunPosition"] = value.SunPosition;
                    dict["AbuseEmail"] = value.AbuseEmail;
                    dict["CovenantID"] = value.CovenantID.ToString();
                    dict["CovenantTimestamp"] = value.CovenantTimestamp.DateTimeToUnixTime();
                    using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        conn.ReplaceInsertInto("estates", dict);
                    }
                }
                else
                {
                    using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using(MySqlCommand cmd = new MySqlCommand("DELETE FROM estates WHERE ID LIKE ?id", conn))
                        {
                            cmd.Parameters.AddWithValue("?id", estateID);
                            if(cmd.ExecuteNonQuery() < 1)
                            {
                                throw new EstateUpdateFailedException();
                            }
                        }
                    }
                }
            }
        }

        public override List<EstateInfo> All
        {
            get 
            {
                List<EstateInfo> list = new List<EstateInfo>();

                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using(MySqlCommand cmd = new MySqlCommand("SELECT * FROM estates", conn))
                    {
                        using(MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                list.Add(reader.ToEstateInfo());
                            }
                        }
                    }
                }
                return list;
            }
        }

        public override List<uint> AllIDs
        {
            get 
            {
                List<uint> list = new List<uint>();

                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM estates", conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                list.Add((uint)reader["ID"]);
                            }
                        }
                    }
                }
                return list;
            }
        }

        public override EstateManagerServiceInterface EstateManager
        {
            get
            {
                return m_EstateManagerService;
            }
        }

        public override IEstateOwnerServiceInterface EstateOwner
        {
            get 
            {
                return m_EstateOwnerService;
            }
        }

        public override EstateAccessServiceInterface EstateAccess
        {
            get 
            {
                return m_EstateAccessService;
            }
        }

        public override EstateGroupsServiceInterface EstateGroup
        {
            get 
            {
                return m_EstateGroupsService;
            }
        }

        public override IEstateRegionMapServiceInterface RegionMap
        {
            get 
            {
                return m_EstateRegionMapService;
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("Estate")]
    public class MySQLEstateServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL ESTATE SERVICE");
        public MySQLEstateServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLEstateService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion

}
