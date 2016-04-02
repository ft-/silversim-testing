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
    public sealed partial class MySQLEstateService : EstateServiceInterface, IDBServiceInterface, IPlugin
    {
        readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL ESTATE SERVICE");

        #region Constructor
        public MySQLEstateService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
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
            new PrimaryKeyInfo("RegionID"),
            new NamedKeyInfo("EstateID", "EstateID"),
            #endregion

            #region estate_managers
            new SqlTable("estate_managers"),
            new AddColumn<uint>("EstateID") { IsNullAllowed = false },
            new AddColumn<UUID>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo(new string[] { "EstateID", "UserID" }),
            new NamedKeyInfo("UserID", "UserID"),
            new NamedKeyInfo("EstateID", "EstateID"),
            new TableRevision(2),
            new ChangeColumn<UUI>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            #endregion

            #region estate_groups
            new SqlTable("estate_groups"),
            new AddColumn<uint>("EstateID") { IsNullAllowed = false },
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo(new string[] { "EstateID", "GroupID" }),
            new NamedKeyInfo("EstateID", "EstateID"),
            new NamedKeyInfo("GroupID", "GroupID"),
            #endregion

            #region estate_users
            new SqlTable("estate_users"),
            new AddColumn<uint>("EstateID") { IsNullAllowed = false },
            new AddColumn<UUI>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo(new string[] { "EstateID", "UserID" }),
            new NamedKeyInfo("EstateID", "EstateID"),
            new NamedKeyInfo("UserID", "UserID"),
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
            new NamedKeyInfo("EstateID", "EstateID"),
            new NamedKeyInfo("UserID", "UserID"),
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
            new AddColumn<int>("PricePerMeter") { IsNullAllowed = false, Default = 0 },
            new AddColumn<double>("BillableFactor") { IsNullAllowed = false, Default = (double)1 },
            new AddColumn<double>("SunPosition") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<string>("AbuseEmail") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new PrimaryKeyInfo("ID"),
            new NamedKeyInfo("Name", "Name") { IsUnique = true },
            new NamedKeyInfo("Owner", "Owner"),
            new NamedKeyInfo("ID_Owner", "ID", "Owner"),
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
                    cmd.Parameters.AddParameter("?id", estateID);
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
                    cmd.Parameters.AddParameter("?name", estateName);
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
                    cmd.Parameters.AddParameter("?id", estateID);
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
                    cmd.Parameters.AddParameter("?name", estateName);
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
            dict["Owner"] = estateInfo.Owner;
            dict["Flags"] = estateInfo.Flags;
            dict["PricePerMeter"] = estateInfo.PricePerMeter;
            dict["BillableFactor"] = estateInfo.BillableFactor;
            dict["SunPosition"] = estateInfo.SunPosition;
            dict["AbuseEmail"] = estateInfo.AbuseEmail;
            dict["CovenantID"] = estateInfo.CovenantID;
            dict["CovenantTimestamp"] = estateInfo.CovenantTimestamp;
            dict["UseGlobalTime"] = estateInfo.UseGlobalTime;
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
                        cmd.Parameters.AddParameter("?id", estateID);
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
                    dict["Owner"] = value.Owner;
                    dict["Flags"] = (uint)value.Flags;
                    dict["PricePerMeter"] = value.PricePerMeter;
                    dict["BillableFactor"] = value.BillableFactor;
                    dict["SunPosition"] = value.SunPosition;
                    dict["AbuseEmail"] = value.AbuseEmail;
                    dict["CovenantID"] = value.CovenantID;
                    dict["CovenantTimestamp"] = value.CovenantTimestamp;
                    dict["UseGlobalTime"] = value.UseGlobalTime;
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
                            cmd.Parameters.AddParameter("?id", estateID);
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
                                list.Add(reader.GetUInt32("ID"));
                            }
                        }
                    }
                }
                return list;
            }
        }

        public override IEstateManagerServiceInterface EstateManager
        {
            get
            {
                return this;
            }
        }

        public override IEstateOwnerServiceInterface EstateOwner
        {
            get 
            {
                return this;
            }
        }

        public override IEstateAccessServiceInterface EstateAccess
        {
            get 
            {
                return this;
            }
        }

        public override IEstateBanServiceInterface EstateBans
        {
            get
            {
                return this;
            }
        }

        public override IEstateGroupsServiceInterface EstateGroup
        {
            get 
            {
                return this;
            }
        }

        public override IEstateRegionMapServiceInterface RegionMap
        {
            get 
            {
                return this;
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
