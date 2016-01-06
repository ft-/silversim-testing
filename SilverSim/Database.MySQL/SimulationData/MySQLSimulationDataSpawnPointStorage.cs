﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataSpawnPointStorage : SimulationDataSpawnPointStorageInterface, IDBServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SIMULATION STORAGE");

        readonly string m_ConnectionString;
        public MySQLSimulationDataSpawnPointStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public override List<Vector3> this[UUID regionID]
        {
            get
            {
                List<Vector3> res = new List<Vector3>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT DistanceX, DistanceY, DistanceZ FROM spawnpoints WHERE RegionID LIKE ?regionid", conn))
                    {
                        cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                res.Add(reader.GetVector("Distance"));
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
                        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM spawnpoints WHERE RegionID LIKE ?regionid", conn))
                        {
                            cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                            cmd.ExecuteNonQuery();
                        }

                        Dictionary<string, object> data = new Dictionary<string, object>();
                        data.Add("RegionID", regionID.ToString());

                        foreach (Vector3 v in value)
                        {
                            data.Add("Distance", v);
                            conn.InsertInto("spawnpoints", data);
                        }
                    });
                }
            }
        }

        public override bool Remove(UUID regionID)
        {
            List<Vector3> res = new List<Vector3>();
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM spawnpoints WHERE RegionID LIKE ?regionid", conn))
                {
                    cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "spawnpoints", Migrations, m_Log);
        }

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "RegionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "DistanceX DOUBLE NOT NULL," +
                "DistanceY DOUBLE NOT NULL," +
                "DistanceZ DOUBLE NOT NULL," +
                "KEY RegionID (RegionID))"
        };

    }
}
