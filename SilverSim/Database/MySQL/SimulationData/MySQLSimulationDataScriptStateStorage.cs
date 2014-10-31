/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

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
        public override string this[UUID regionID, UUID primID, UUID itemID] 
        {
            get
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM scriptstates WHERE RegionID LIKE ?regionID AND PrimID LIKE ?primID AND ItemID LIKE ?itemID", connection))
                    {
                        cmd.Parameters.AddWithValue("?regionID", regionID);
                        cmd.Parameters.AddWithValue("?primID", primID);
                        cmd.Parameters.AddWithValue("?itemID", itemID);
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
                            cmd.Parameters.AddWithValue("?regionID", regionID);
                            cmd.Parameters.AddWithValue("?primID", primID);
                            cmd.Parameters.AddWithValue("?itemID", itemID);
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
