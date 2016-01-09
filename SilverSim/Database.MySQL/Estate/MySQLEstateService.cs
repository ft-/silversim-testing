// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using SilverSim.Types.Estate;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Estate
{
    #region Service Implementation
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [Description("MySQL Estate Backend")]
    public sealed class MySQLEstateService : EstateServiceInterface, IDBServiceInterface, IPlugin
    {
        readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL ESTATE SERVICE");

        readonly MySQLEstateOwnerService m_EstateOwnerService;
        readonly MySQLEstateManagerService m_EstateManagerService;
        readonly MySQLEstateAccessInterface m_EstateAccessService;
        readonly MySQLEstateBanServiceInterface m_EstateBanService;
        readonly MySQLEstateGroupsService m_EstateGroupsService;
        readonly MySQLEstateRegionMapInterface m_EstateRegionMapService;

        #region Constructor
        public MySQLEstateService(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_EstateOwnerService = new MySQLEstateOwnerService(connectionString);
            m_EstateManagerService = new MySQLEstateManagerService(connectionString);
            m_EstateAccessService = new MySQLEstateAccessInterface(connectionString);
            m_EstateBanService = new MySQLEstateBanServiceInterface(connectionString);
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
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.MigrateTables(Migrations, m_Log);
            }
        }

        private static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            #region estate_regionmap
            new SqlTable("estate_regionmap"),
            new AddColumn<uint>("EstateID") { IsNullAllowed = false },
            new AddColumn<UUID>("RegionID") { Default = UUID.Zero, IsNullAllowed = false },
            new PrimaryKeyInfo(new string[] { "RegionID" }),
            new NamedKeyInfo("EstateID", new string[] { "EstateID" }),
            #endregion

            #region estate_managers
            new SqlTable("estate_managers"),
            new AddColumn<uint>("EstateID") { IsNullAllowed = false },
            new AddColumn<UUID>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo(new string[] { "EstateID", "UserID" }),
            new NamedKeyInfo("UserID", new string[] { "UserID" }),
            new NamedKeyInfo("EstateID", new string[] { "EstateID" }),
            new TableRevision(2),
            new ChangeColumn<UUI>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            #endregion

            #region estate_groups
            new SqlTable("estate_groups"),
            new AddColumn<uint>("EstateID") { IsNullAllowed = false },
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo(new string[] { "EstateID", "GroupID" }),
            new NamedKeyInfo("EstateID", new string[] {"EstateID" }),
            new NamedKeyInfo("GroupID", new string[] { "GroupID" }),
            #endregion

            #region estate_users
            new SqlTable("estate_users"),
            new AddColumn<uint>("EstateID") { IsNullAllowed = false },
            new AddColumn<UUI>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo(new string[] { "EstateID", "UserID" }),
            new NamedKeyInfo("EstateID", new string[] { "EstateID" }),
            new NamedKeyInfo("UserID", new string[] { "UserID" }),
            new TableRevision(2),
            /* following two entries are not produced as change lines when not finding a revision 1 table */
            new ChangeColumn<uint>("EstateID") { IsNullAllowed = false },
            new ChangeColumn<UUI>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            #endregion

            #region estate_bans
            new SqlTable("estate_bans"),
            new AddColumn<uint>("EstateID") { IsNullAllowed = false },
            new AddColumn<UUI>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo(new string[] { "EstateID", "UserID" }),
            new NamedKeyInfo("EstateID", new string[] { "EstateID" }),
            new NamedKeyInfo("UserID", new string[] { "UserID" }),
            new TableRevision(2),
            /* following two entries are not produced as change lines when not finding a revision 1 table */
            new ChangeColumn<uint>("EstateID") { IsNullAllowed = false },
            new ChangeColumn<UUI>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            #endregion

            #region estates
            new SqlTable("estates"),
            new AddColumn<uint>("ID") { IsNullAllowed = false },
            new AddColumn<string>("Name") { Cardinality = 64, IsNullAllowed = false },
            new AddColumn<UUI>("Owner") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<uint>("Flags") { IsNullAllowed = false, Default = (uint)0 },
            new AddColumn<int>("PricePerMeter") { IsNullAllowed = false, Default = (int)0 },
            new AddColumn<double>("BillableFactor") { IsNullAllowed = false, Default = (double)1 },
            new AddColumn<double>("SunPosition") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<string>("AbuseEmail") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new PrimaryKeyInfo(new string[] { "ID" }),
            new NamedKeyInfo("Name", new string[] { "Name" }) { IsUnique = true },
            new NamedKeyInfo("Owner", new string[] { "Owner" }),
            new NamedKeyInfo("ID_Owner", new string[] { "ID", "Owner" }),
            new TableRevision(2),
            new AddColumn<UUID>("CovenantID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<ulong>("CovenantTimestamp") { IsNullAllowed = false, Default = (ulong)0 },
            new TableRevision(3),
            new AddColumn<bool>("UseGlobalTime") { IsNullAllowed = false, Default = true },
            new TableRevision(4),
            new ChangeColumn<UUI>("Owner") { IsNullAllowed = false, Default = UUID.Zero, OldName = "OwnerID" } 
            /* ^^ this is for compatibility our list generator actually skips this field when not finding the revision 3 table */
            #endregion
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

        public override bool TryGetValue(string estateName, out EstateInfo estateInfo)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM estates WHERE Name LIKE ?name", conn))
                {
                    cmd.Parameters.AddWithValue("?name", estateName);
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

        public override bool ContainsKey(string estateName)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT id FROM estates WHERE Name LIKE ?name", conn))
                {
                    cmd.Parameters.AddWithValue("?name", estateName);
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

        public override void Add(EstateInfo estateInfo)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict["ID"] = estateInfo.ID;
            dict["Name"] = estateInfo.Name;
            dict["Owner"] = estateInfo.Owner.ToString();
            dict["Flags"] = (uint)estateInfo.Flags;
            dict["PricePerMeter"] = estateInfo.PricePerMeter;
            dict["BillableFactor"] = estateInfo.BillableFactor;
            dict["SunPosition"] = estateInfo.SunPosition;
            dict["AbuseEmail"] = estateInfo.AbuseEmail;
            dict["CovenantID"] = estateInfo.CovenantID.ToString();
            dict["CovenantTimestamp"] = estateInfo.CovenantTimestamp.DateTimeToUnixTime();
            dict["UseGlobalTime"] = estateInfo.UseGlobalTime ? 1 : 0;
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsertInto("estates", dict);
            }
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
                    dict["Owner"] = value.Owner.ToString();
                    dict["Flags"] = (uint)value.Flags;
                    dict["PricePerMeter"] = value.PricePerMeter;
                    dict["BillableFactor"] = value.BillableFactor;
                    dict["SunPosition"] = value.SunPosition;
                    dict["AbuseEmail"] = value.AbuseEmail;
                    dict["CovenantID"] = value.CovenantID.ToString();
                    dict["CovenantTimestamp"] = value.CovenantTimestamp.DateTimeToUnixTime();
                    dict["UseGlobalTime"] = value.UseGlobalTime ? 1 : 0;
                    using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        conn.ReplaceInto("estates", dict);
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

        public override EstateBanServiceInterface EstateBans
        {
            get
            {
                return m_EstateBanService;
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
