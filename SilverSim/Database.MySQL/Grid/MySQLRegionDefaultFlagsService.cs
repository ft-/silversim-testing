// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Database.MySQL.Grid
{
    [Description("MySQL RegionDefaultFlags Backend")]
    public class MySQLRegionDefaultFlagsService : RegionDefaultFlagsServiceInterface, IPlugin, IDBServiceInterface
    {
        readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL REGIONDEFAULTFLAGS SERVICE");

        public MySQLRegionDefaultFlagsService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void ProcessMigrations()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.MigrateTables(Migrations, m_Log);
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public override RegionFlags GetRegionDefaultFlags(UUID regionId)
        {
            RegionFlags flags = RegionFlags.None;
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT flags FROM regiondefaults WHERE uuid LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", regionId);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            flags = reader.GetEnum<RegionFlags>("flags");
                        }
                    }
                }
            }
            return flags;
        }

        public override void ChangeRegionDefaultFlags(UUID regionId, RegionFlags addFlags, RegionFlags removeFlags)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction(delegate ()
                {
                    bool haveEntry = false;
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regiondefaults WHERE uuid LIKE ?id", connection))
                    {
                        cmd.Parameters.AddParameter("?id", regionId);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            haveEntry = reader.Read();
                        }
                    }

                    if (haveEntry)
                    {
                        using (MySqlCommand cmd = new MySqlCommand("UPDATE regiondefaults SET flags = (flags & ?remove) | ?add WHERE uuid LIKE ?id", connection))
                        {
                            cmd.Parameters.AddParameter("?remove", ~removeFlags);
                            cmd.Parameters.AddParameter("?add", addFlags);
                            cmd.Parameters.AddParameter("?id", regionId);
                            cmd.ExecuteNonQuery();
                        }
                        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM regiondefaults WHERE flags = 0 AND uuid LIKE ?id", connection))
                        {
                            cmd.Parameters.AddParameter("?id", regionId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        Dictionary<string, object> vals = new Dictionary<string, object>();
                        vals.Add("uuid", regionId);
                        vals.Add("flags", addFlags);
                        connection.InsertInto("regiondefaults", vals);
                    }
                });
            }
        }

        public override Dictionary<UUID, RegionFlags> GetAllRegionDefaultFlags()
        {
            Dictionary<UUID, RegionFlags> result = new Dictionary<UUID, RegionFlags>();
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regiondefaults", connection))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            result.Add(reader.GetUUID("uuid"), reader.GetEnum<RegionFlags>("flags"));
                        }
                    }
                }
            }
            return result;
        }

        static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("regiondefaults"),
            new AddColumn<UUID>("uuid") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<RegionFlags>("flags") { IsNullAllowed = false, Default = RegionFlags.None },
            new PrimaryKeyInfo("uuid")
        };
    }

    [PluginName("RegionDefaultFlags")]
    public class MySQLRegionDefaultFlagsServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL REGIONDEFAULTFLAGS SERVICE");
        public MySQLRegionDefaultFlagsServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLRegionDefaultFlagsService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
}
