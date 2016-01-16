// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataRegionSettingsStorage : SimulationDataRegionSettingsStorageInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL REGION SETTINGS SERVICE");

        readonly string m_ConnectionString;
        public MySQLSimulationDataRegionSettingsStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        RegionSettings ToRegionSettings(MySqlDataReader reader)
        {
            RegionSettings settings = new RegionSettings();
            settings.BlockTerraform = reader.GetBool("BlockTerraform");
            settings.BlockFly = reader.GetBool("BlockFly");
            settings.AllowDamage = reader.GetBool("AllowDamage");
            settings.RestrictPushing = reader.GetBool("RestrictPushing");
            settings.AllowLandResell = reader.GetBool("AllowLandResell");
            settings.AllowLandJoinDivide = reader.GetBool("AllowLandJoinDivide");
            settings.BlockShowInSearch = reader.GetBool("BlockShowInSearch");
            settings.AgentLimit = reader.GetInt32("AgentLimit");
            settings.ObjectBonus = reader.GetDouble("ObjectBonus");
            settings.DisableScripts = reader.GetBool("DisableScripts");
            settings.DisableCollisions = reader.GetBool("DisableCollisions");
            settings.BlockFlyOver = reader.GetBool("BlockFlyOver");
            settings.Sandbox = reader.GetBool("Sandbox");
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
            settings.UseEstateSun = reader.GetBool("UseEstateSun");
            settings.BlockDwell = reader.GetBool("BlockDwell");
            settings.ResetHomeOnTeleport = reader.GetBool("ResetHomeOnTeleport");
            settings.AllowLandmark = reader.GetBool("AllowLandmark");
            settings.AllowDirectTeleport = reader.GetBool("AllowDirectTeleport");

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
                    data["RegionID"] = regionID;
                    data["BlockTerraform"] = value.BlockTerraform;
                    data["BlockFly"] = value.BlockFly;
                    data["AllowDamage"] = value.AllowDamage;
                    data["RestrictPushing"] = value.RestrictPushing;
                    data["AllowLandResell"] = value.AllowLandResell;
                    data["AllowLandJoinDivide"] = value.AllowLandJoinDivide;
                    data["BlockShowInSearch"] = value.BlockShowInSearch;
                    data["AgentLimit"] = value.AgentLimit;
                    data["ObjectBonus"] = value.ObjectBonus;
                    data["DisableScripts"] = value.DisableScripts;
                    data["DisableCollisions"] = value.DisableCollisions;
                    data["BlockFlyOver"] = value.BlockFlyOver;
                    data["Sandbox"] = value.Sandbox;
                    data["TerrainTexture1"] = value.TerrainTexture1;
                    data["TerrainTexture2"] = value.TerrainTexture2;
                    data["TerrainTexture3"] = value.TerrainTexture3;
                    data["TerrainTexture4"] = value.TerrainTexture4;
                    data["TelehubObject"] = value.TelehubObject;
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
                    data["IsSunFixed"] = value.IsSunFixed;
                    data["UseEstateSun"] = value.UseEstateSun;
                    data["BlockDwell"] = value.BlockDwell;
                    data["ResetHomeOnTeleport"] = value.ResetHomeOnTeleport;
                    data["AllowLandmark"] = value.AllowLandmark;
                    data["AllowDirectTeleport"] = value.AllowDirectTeleport;

                    conn.ReplaceInto("regionsettings", data);
                }
            }
        }

        public override bool TryGetValue(UUID regionID, out RegionSettings settings)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM regionsettings WHERE RegionID LIKE '" + regionID.ToString() + "'", conn))
                {
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
                using (MySqlCommand cmd = new MySqlCommand("SELECT RegionID FROM regionsettings WHERE RegionID LIKE '" + regionID.ToString() + "'", conn))
                {
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
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM regionsettings WHERE RegionID LIKE '" + regionID.ToString() + "'", conn))
                {
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
