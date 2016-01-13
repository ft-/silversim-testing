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
    public class MySQLSimulationDataScriptStateStorage : SimulationDataScriptStateStorageInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL SCRIPT STATE SERVICE");

        readonly string m_ConnectionString;
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

                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM scriptstates WHERE RegionID LIKE '" + regionID.ToString() + "' AND PrimID LIKE '" + primID.ToString() + "' AND ItemID LIKE '" + itemID.ToString() + "'", connection))
                    {
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
                        using(MySqlCommand cmd = new MySqlCommand("DELETE FROM scriptstates WHERE RegionID LIKE '" + regionID.ToString() + "' AND PrimID LIKE '" + primID.ToString() + "' AND ItemID LIKE '" + itemID.ToString() + "'", connection))
                        {
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
                        MySQLUtilities.ReplaceInto(connection, "scriptstates", p);
                    }
                }
            }
        }
    }
}
