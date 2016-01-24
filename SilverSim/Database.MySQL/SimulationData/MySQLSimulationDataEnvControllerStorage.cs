// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataEnvControllerStorage : SimulationDataEnvControllerStorageInterface
    {
#if DEBUG
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL ENVIRONMENT CONTRLLER SETTINGS SERVICE");
#endif

        readonly string m_ConnectionString;

        public MySQLSimulationDataEnvControllerStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public override bool TryGetValue(UUID regionID, out byte[] settings)
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
        public override byte[] this[UUID regionID]
        {
            get
            {
                byte[] settings;
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

        public override bool Remove(UUID regionID)
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
