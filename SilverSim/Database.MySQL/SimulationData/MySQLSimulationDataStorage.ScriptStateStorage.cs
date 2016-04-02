// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.SimulationData
{
    public partial class MySQLSimulationDataStorage : IISimulationDataScriptStateStorageInterface
    {
        bool IISimulationDataScriptStateStorageInterface.TryGetValue(UUID regionID, UUID primID, UUID itemID, out byte[] state)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();

                using (MySqlCommand cmd = new MySqlCommand("SELECT ScriptState FROM scriptstates WHERE RegionID LIKE '" + regionID.ToString() + "' AND PrimID LIKE '" + primID.ToString() + "' AND ItemID LIKE '" + itemID.ToString() + "'", connection))
                {
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        if (dbReader.Read())
                        {
                            state = dbReader.GetBytes("ScriptState");
                            return true;
                        }
                    }
                }
            }
            state = null;
            return false;
        }

        /* setting value to null will delete the entry */
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        byte[] IISimulationDataScriptStateStorageInterface.this[UUID regionID, UUID primID, UUID itemID] 
        {
            get
            {
                byte[] state;
                if(!ScriptStates.TryGetValue(regionID, primID, itemID, out state))
                {
                    throw new KeyNotFoundException();
                }

                return state;
            }
            set
            {
                using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    Dictionary<string, object> p = new Dictionary<string, object>();
                    p["RegionID"] = regionID;
                    p["PrimID"] = primID;
                    p["ItemID"] = itemID;
                    p["ScriptState"] = value;
                    MySQLUtilities.ReplaceInto(connection, "scriptstates", p);
                }
            }
        }

        bool IISimulationDataScriptStateStorageInterface.Remove(UUID regionID, UUID primID, UUID itemID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM scriptstates WHERE RegionID LIKE '" + regionID.ToString() + "' AND PrimID LIKE '" + primID.ToString() + "' AND ItemID LIKE '" + itemID.ToString() + "'", connection))
                {
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
