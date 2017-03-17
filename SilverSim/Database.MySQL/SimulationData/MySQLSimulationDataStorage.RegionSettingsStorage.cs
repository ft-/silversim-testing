// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.SimulationData
{
    public partial class MySQLSimulationDataStorage : ISimulationDataRegionSettingsStorageInterface
    {
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

        RegionSettings ISimulationDataRegionSettingsStorageInterface.this[UUID regionID]
        {
            get
            {
                RegionSettings settings;
                if (!RegionSettings.TryGetValue(regionID, out settings))
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

        bool ISimulationDataRegionSettingsStorageInterface.TryGetValue(UUID regionID, out RegionSettings settings)
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

        bool ISimulationDataRegionSettingsStorageInterface.ContainsKey(UUID regionID)
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

        bool ISimulationDataRegionSettingsStorageInterface.Remove(UUID regionID)
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
