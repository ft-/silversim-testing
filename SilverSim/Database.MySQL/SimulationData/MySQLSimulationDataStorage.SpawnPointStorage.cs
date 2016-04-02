// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.SimulationData
{
    public partial class MySQLSimulationDataStorage : ISimulationDataSpawnPointStorageInterface
    {
        List<Vector3> ISimulationDataSpawnPointStorageInterface.this[UUID regionID]
        {
            get
            {
                List<Vector3> res = new List<Vector3>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT DistanceX, DistanceY, DistanceZ FROM spawnpoints WHERE RegionID LIKE '" + regionID.ToString() + "'", conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                res.Add(reader.GetVector3("Distance"));
                            }
                        }
                    }
                }
                return res;
            }
            set
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    conn.InsideTransaction(delegate ()
                    {
                        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM spawnpoints WHERE RegionID LIKE '" + regionID.ToString() + "'", conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        Dictionary<string, object> data = new Dictionary<string, object>();
                        data.Add("RegionID", regionID.ToString());

                        foreach (Vector3 v in value)
                        {
                            data["Distance"] = v;
                            conn.InsertInto("spawnpoints", data);
                        }
                    });
                }
            }
        }

        bool ISimulationDataSpawnPointStorageInterface.Remove(UUID regionID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM spawnpoints WHERE RegionID LIKE '" + regionID.ToString() + "'", conn))
                {
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
