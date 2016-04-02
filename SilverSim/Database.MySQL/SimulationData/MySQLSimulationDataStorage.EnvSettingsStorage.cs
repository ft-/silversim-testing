// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using WindLightSettings = SilverSim.Scene.Types.WindLight.EnvironmentSettings;
using SilverSim.Types;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Database.MySQL.SimulationData
{
    public partial class MySQLSimulationDataStorage : ISimulationDataEnvSettingsStorageInterface
    {
        bool ISimulationDataEnvSettingsStorageInterface.TryGetValue(UUID regionID, out WindLightSettings settings)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT EnvironmentSettings FROM environmentsettings WHERE RegionID LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddParameter("?regionid", regionID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            using (MemoryStream ms = new MemoryStream(reader.GetBytes("EnvironmentSettings")))
                            {
                                settings = WindLightSettings.Deserialize(ms);
                                return true;
                            }
                        }
                    }
                }
            }
            settings = null;
            return false;
        }

        /* setting value to null will delete the entry */
        WindLightSettings ISimulationDataEnvSettingsStorageInterface.this[UUID regionID]
        {
            get
            {
                WindLightSettings settings;
                if (!EnvironmentSettings.TryGetValue(regionID, out settings))
                {
                    throw new KeyNotFoundException();
                }
                return settings;
            }
            set
            {
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    if(value == null)
                    {
#if DEBUG
                        m_Log.DebugFormat("Removing environment settings for {0}", regionID.ToString());
#endif
                        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM environmentsettings WHERE RegionID LIKE ?regionid", conn))
                        {
                            cmd.Parameters.AddParameter("?regionid", regionID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
#if DEBUG
                        m_Log.DebugFormat("Storing new environment settings for {0}", regionID.ToString());
#endif
                        Dictionary<string, object> param = new Dictionary<string,object>();
                        param["RegionID"] = regionID;
                        using(MemoryStream ms = new MemoryStream())
                        {
                            value.Serialize(ms, regionID);
                            param["EnvironmentSettings"] = ms.GetBuffer();
                        }
                        conn.ReplaceInto("environmentsettings", param);
                    }
                }
            }
        }

        bool ISimulationDataEnvSettingsStorageInterface.Remove(UUID regionID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM environmentsettings WHERE RegionID LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddParameter("?regionid", regionID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
