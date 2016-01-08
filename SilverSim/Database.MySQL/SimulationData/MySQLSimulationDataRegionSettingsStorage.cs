// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataRegionSettingsStorage : SimulationDataRegionSettingsStorageInterface, IDBServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL REGION SETTINGS SERVICE");

        readonly string m_ConnectionString;
        public MySQLSimulationDataRegionSettingsStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        RegionSettings ToRegionSettings(MySqlDataReader reader)
        {
            RegionSettings settings = new RegionSettings();
            settings.BlockTerraform = reader.GetBoolean("BlockTerraform");
            settings.BlockFly = reader.GetBoolean("BlockFly");
            settings.AllowDamage = reader.GetBoolean("AllowDamage");
            settings.RestrictPushing = reader.GetBoolean("RestrictPushing");
            settings.AllowLandResell = reader.GetBoolean("AllowLandResell");
            settings.AllowLandJoinDivide = reader.GetBoolean("AllowLandJoinDivide");
            settings.BlockShowInSearch = reader.GetBoolean("BlockShowInSearch");
            settings.AgentLimit = reader.GetInt32("AgentLimit");
            settings.ObjectBonus = reader.GetDouble("ObjectBonus");
            settings.DisableScripts = reader.GetBoolean("DisableScripts");
            settings.DisableCollisions = reader.GetBoolean("DisableCollisions");
            settings.BlockFlyOver = reader.GetBoolean("BlockFlyOver");
            settings.Sandbox = reader.GetBoolean("Sandbox");
            settings.TerrainTexture1 = reader.GetUUID("TerrainTexture1");
            settings.TerrainTexture2 = reader.GetUUID("TerrainTexture2");
            settings.TerrainTexture3 = reader.GetUUID("TerrainTexture3");
            settings.TerrainTexture4 = reader.GetUUID("TerrainTexture4");
            settings.TelehubObject = reader.GetUUID("TelehubObject");
            settings.Elevation1NW = reader.GetDouble("Elevation1NW");
            settings.Elevation2NW = reader.GetDouble("Elevation2NW");
            settings.Elevation1NE = reader.GetDouble("Elevation1NE");
            settings.Elevation2NE = reader.GetDouble("Elevation2NE");
            settings.Elevation1SE = reader.GetDouble("Elevation1SE");
            settings.Elevation2SE = reader.GetDouble("Elevation2SE");
            settings.Elevation1SW = reader.GetDouble("Elevation1SW");
            settings.Elevation2SW = reader.GetDouble("Elevation2SW");
            settings.WaterHeight = reader.GetDouble("WaterHeight");
            settings.TerrainRaiseLimit = reader.GetDouble("TerrainRaiseLimit");
            settings.TerrainLowerLimit = reader.GetDouble("TerrainLowerLimit");
            settings.SunPosition = reader.GetDouble("SunPosition");
            settings.IsSunFixed = reader.GetBoolean("IsSunFixed");
            settings.UseEstateSun = reader.GetBoolean("UseEstateSun");

            return settings;
        }

        public override RegionSettings this[UUID regionID]
        {
            get
            {
                RegionSettings settings;
                if (!TryGetValue(regionID, out settings))
                {
                    throw new KeyNotFoundException();
                }
                return settings;
            }
            set
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    Dictionary<string, object> data = new Dictionary<string, object>();
                    data["RegionID"] = regionID.ToString();
                    data["BlockTerraform"] = value.BlockTerraform ? 1 : 0;
                    data["BlockFly"] = value.BlockFly ? 1 : 0;
                    data["AllowDamage"] = value.AllowDamage ? 1 : 0;
                    data["RestrictPushing"] = value.RestrictPushing ? 1 : 0;
                    data["AllowLandResell"] = value.AllowLandResell ? 1 : 0;
                    data["AllowLandJoinDivide"] = value.AllowLandJoinDivide ? 1 : 0;
                    data["BlockShowInSearch"] = value.BlockShowInSearch ? 1 : 0;
                    data["AgentLimit"] = value.AgentLimit;
                    data["ObjectBonus"] = value.ObjectBonus;
                    data["DisableScripts"] = value.DisableScripts;
                    data["DisableCollisions"] = value.DisableCollisions;
                    data["BlockFlyOver"] = value.BlockFlyOver;
                    data["Sandbox"] = value.Sandbox;
                    data["TerrainTexture1"] = value.TerrainTexture1.ToString();
                    data["TerrainTexture2"] = value.TerrainTexture2.ToString();
                    data["TerrainTexture3"] = value.TerrainTexture3.ToString();
                    data["TerrainTexture4"] = value.TerrainTexture4.ToString();
                    data["TelehubObject"] = value.TelehubObject.ToString();
                    data["Elevation1NW"] = value.Elevation1NW;
                    data["Elevation2NW"] = value.Elevation2NW;
                    data["Elevation1NE"] = value.Elevation1NE;
                    data["Elevation2NE"] = value.Elevation2NE;
                    data["Elevation1SE"] = value.Elevation1SE;
                    data["Elevation2SE"] = value.Elevation2SE;
                    data["Elevation1SW"] = value.Elevation1SW;
                    data["Elevation2SW"] = value.Elevation2SW;
                    data["WaterHeight"] = value.WaterHeight;
                    data["TerrainRaiseLimit"] = value.TerrainRaiseLimit;
                    data["TerrainLowerLimit"] = value.TerrainLowerLimit;
                    data["SunPosition"] = value.SunPosition;
                    data["IsSunFixed"] = value.IsSunFixed ? 1 : 0;
                    data["UseEstateSun"] = value.UseEstateSun ? 1 : 0;

                    conn.ReplaceInto("regionsettings", data);
                }
            }
        }

        public override bool TryGetValue(UUID regionID, out RegionSettings settings)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regionsettings WHERE RegionID LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            settings = ToRegionSettings(reader);
                            return true;
                        }
                    }
                }
            }
            settings = null;
            return false;
        }

        public override bool ContainsKey(UUID regionID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT RegionID FROM regionsettings WHERE RegionID LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        return (reader.Read());
                    }
                }
            }
        }

        public override bool Remove(UUID regionID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM regionsettings WHERE RegionID LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        #region Migrations
        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "regionsettings", Migrations, m_Log);
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "RegionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "BlockTerraform INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "BlockFly INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "AllowDamage INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "RestrictPushing INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "AllowLandResell INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "AllowLandJoinDivide INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "BlockShowInSearch INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "AgentLimit INT(11) NOT NULL DEFAULT '0'," +
                "ObjectBonus DOUBLE NOT NULL DEFAULT '0'," +
                "DisableScripts INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "DisableCollisions INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "DisablePhysics INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "BlockFlyOver INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "Sandbox INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "TerrainTexture1 CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "TerrainTexture2 CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "TerrainTexture3 CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "TerrainTexture4 CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "TelehubObject CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "Elevation1NW DOUBLE NOT NULL DEFAULT '0'," +
                "Elevation2NW DOUBLE NOT NULL DEFAULT '0'," +
                "Elevation1NE DOUBLE NOT NULL DEFAULT '0'," +
                "Elevation2NE DOUBLE NOT NULL DEFAULT '0'," +
                "Elevation1SE DOUBLE NOT NULL DEFAULT '0'," +
                "Elevation2SE DOUBLE NOT NULL DEFAULT '0'," +
                "Elevation1SW DOUBLE NOT NULL DEFAULT '0'," +
                "Elevation2SW DOUBLE NOT NULL DEFAULT '0'," +
                "WaterHeight DOUBLE NOT NULL DEFAULT '0'," +
                "TerrainRaiseLimit DOUBLE NOT NULL DEFAULT '0'," +
                "TerrainLowerLimit DOUBLE NOT NULL DEFAULT '0'," +
                "PRIMARY KEY(RegionID))",
            "ALTER TABLE %tablename% ADD COLUMN (UseEstateSun INT(1) UNSIGNED NOT NULL DEFAULT '1', IsSunFixed INT(1) UNSIGNED NOT NULL DEFAULT '0'," +
                "SunPosition DOUBLE NOT NULL DEFAULT '0'),"
        };

        #endregion
    }
}
