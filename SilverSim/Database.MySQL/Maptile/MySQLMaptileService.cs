﻿using System;
using System.Collections.Generic;
using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Maptile;
using SilverSim.Types;
using SilverSim.Types.Maptile;

namespace SilverSim.Database.MySQL.Maptile
{
    public class MySQLMaptileService : MaptileServiceInterface, IPlugin, IDBServiceInterface
    {
        readonly string m_ConnectionString;
        static readonly ILog m_Log = LogManager.GetLogger("MYSQL MAPTILE SERVICE");

        public MySQLMaptileService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("maptiles"),
            new AddColumn<uint>("LocX") { IsNullAllowed = false },
            new AddColumn<uint>("LocY") { IsNullAllowed = false },
            new AddColumn<UUID>("ScopeID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Date>("LastUpdate") { IsNullAllowed = false },
            new AddColumn<string>("ContentType") { Cardinality = 255 },
            new AddColumn<int>("ZoomLevel") { IsNullAllowed = false, Default = 1 },
            new AddColumn<byte[]>("Data") { IsLong = true },
            new PrimaryKeyInfo("LocX", "LocY", "ZoomLevel")
        };

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

        public override bool TryGetValue(GridVector location, int zoomlevel, out MaptileData data)
        {
            data = default(MaptileData);
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM maptiles WHERE LocX LIKE ?locx AND LocY LIKE ?locy AND ZoomLevel = ?zoomlevel", connection))
                {
                    cmd.Parameters.AddParameter("?locx", location.X);
                    cmd.Parameters.AddParameter("?locy", location.Y);
                    cmd.Parameters.AddParameter("?zoomlevel", zoomlevel);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            data = new MaptileData();
                            data.Location.X = reader.GetUInt32("LocX");
                            data.Location.Y = reader.GetUInt32("LocY");
                            data.ScopeID = reader.GetUUID("ScopeID");
                            data.LastUpdate = reader.GetDate("LastUpdate");
                            data.ContentType = reader.GetString("ContentType");
                            data.ZoomLevel = reader.GetInt32("ZoomLevel");
                            data.Data = reader.GetBytes("Data");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override void Store(MaptileData data)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                Dictionary<string, object> vals = new Dictionary<string, object>();
                vals.Add("LocX", data.Location.X);
                vals.Add("LocY", data.Location.Y);
                vals.Add("ScopeID", data.ScopeID);
                vals.Add("LastUpdate", Date.Now);
                vals.Add("ContentType", data.ContentType);
                vals.Add("ZoomLevel", data.ZoomLevel);
                vals.Add("Data", data.Data);
                connection.ReplaceInto("maptiles", vals);
            }
        }

        public override List<MaptileInfo> GetUpdateTimes(UUID scopeid, GridVector minloc, GridVector maxloc, int zoomlevel)
        {
            List<MaptileInfo> infos = new List<MaptileInfo>();

            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT LocX, LocY, LastUpdate WHERE ScopeID LIKE ?scopeid AND ZoomLevel = ?zoomlevel AND locX >= ?locxlow AND locY >= ?locylow AND locX <= ?locxhigh AND locY <= ?locyhigh", connection))
                {
                    cmd.Parameters.AddParameter("?scopeid", scopeid);
                    cmd.Parameters.AddParameter("?zoomlevel", zoomlevel);
                    cmd.Parameters.AddParameter("?locxlow", minloc.X);
                    cmd.Parameters.AddParameter("?locylow", minloc.Y);
                    cmd.Parameters.AddParameter("?locxhigh", maxloc.X);
                    cmd.Parameters.AddParameter("?locyhigh", maxloc.Y);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            MaptileInfo info = new MaptileInfo();
                            info.Location.X = reader.GetUInt32("LocX");
                            info.Location.Y = reader.GetUInt32("LocY");
                            info.LastUpdate = reader.GetDate("LastUpdate");
                            info.ScopeID = scopeid;
                            info.ZoomLevel = zoomlevel;
                            infos.Add(info);
                        }
                    }
                }
            }
            return infos;
        }
    }

    [PluginName("Maptile")]
    public class MySQLMaptileServiceFactory : IPluginFactory
    {
        static readonly ILog m_Log = LogManager.GetLogger("MYSQL MAPTILE SERVICE");
        public MySQLMaptileServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLMaptileService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
}
