// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.SimulationData
{
    public partial class MySQLSimulationDataStorage : ISimulationDataEnvControllerStorageInterface
    {

        bool ISimulationDataEnvControllerStorageInterface.TryGetValue(UUID regionID, out byte[] settings)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT SerializedData FROM environmentcontroller WHERE RegionID LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddParameter("?regionid", regionID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            settings = reader.GetBytes("SerializedData");
                            return true;
                        }
                    }
                }
            }
            settings = null;
            return false;
        }

        /* setting value to null will delete the entry */
        byte[] ISimulationDataEnvControllerStorageInterface.this[UUID regionID]
        {
            get
            {
                byte[] settings;
                if (!EnvironmentController.TryGetValue(regionID, out settings))
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
                    if (value == null)
                    {
#if DEBUG
                        m_Log.DebugFormat("Removing environment controller settings for {0}", regionID.ToString());
#endif
                        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM environmentcontroller WHERE RegionID LIKE ?regionid", conn))
                        {
                            cmd.Parameters.AddParameter("?regionid", regionID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
#if DEBUG
                        m_Log.DebugFormat("Storing new environment controller settings for {0}", regionID.ToString());
#endif
                        Dictionary<string, object> param = new Dictionary<string, object>();
                        param["RegionID"] = regionID;
                        param["SerializedData"] = value;
                        conn.ReplaceInto("environmentcontroller", param);
                    }
                }
            }
        }

        bool ISimulationDataEnvControllerStorageInterface.Remove(UUID regionID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM environmentcontroller WHERE RegionID LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddParameter("?regionid", regionID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
