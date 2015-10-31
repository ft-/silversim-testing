// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Object;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using SilverSim.ServiceInterfaces.Database;
using MySql.Data.MySqlClient;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataScriptStateStorage : SimulationDataScriptStateStorageInterface, IDBServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SCRIPT STATE SERVICE");

        public string m_ConnectionString;
        public MySQLSimulationDataScriptStateStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        /* setting value to null will delete the entry */
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override string this[UUID regionID, UUID primID, UUID itemID] 
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM scriptstates WHERE RegionID LIKE ?regionID AND PrimID LIKE ?primID AND ItemID LIKE ?itemID", connection))
                    {
                        cmd.Parameters.AddWithValue("?regionID", regionID.ToString());
                        cmd.Parameters.AddWithValue("?primID", primID.ToString());
                        cmd.Parameters.AddWithValue("?itemID", itemID.ToString());
                        using (MySqlDataReader dbReader = cmd.ExecuteReader())
                        {
                            if (dbReader.Read())
                            {
                                return (string)dbReader["ScriptState"];
                            }
                        }
                    }
                }
                throw new KeyNotFoundException();
            }
            set
            {
                using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    if(String.IsNullOrEmpty(value))
                    {
                        using(MySqlCommand cmd = new MySqlCommand("DELETE FROM scriptstates WHERE RegionID LIKE ?regionID AND PrimID LIKE ?primID AND ItemID LIKE ?itemID", connection))
                        {
                            cmd.Parameters.AddWithValue("?regionID", regionID.ToString());
                            cmd.Parameters.AddWithValue("?primID", primID.ToString());
                            cmd.Parameters.AddWithValue("?itemID", itemID.ToString());
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        Dictionary<string, object> p = new Dictionary<string, object>();
                        p["RegionID"] = regionID;
                        p["PrimID"] = primID;
                        p["ItemID"] = itemID;
                        p["ScriptState"] = value;
                        MySQLUtilities.ReplaceInsertInto(connection, "scriptstates", p);
                    }
                }
            }
        }

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        #region Migrations
        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "scriptstates", Migrations, m_Log);
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "RegionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "PrimID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "ItemID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "ScriptState LONGTEXT," +
                "PRIMARY KEY(RegionID, PrimID, ItemID))"
        };
        #endregion
    }
}
