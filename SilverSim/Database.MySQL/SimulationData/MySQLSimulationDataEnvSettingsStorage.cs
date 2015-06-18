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
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.WindLight;
using SilverSim.Types;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataEnvSettingsStorage : SimulationDataEnvSettingsStorageInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL ENVIRONMENT SETTINGS SERVICE");

        string m_ConnectionString;

        public MySQLSimulationDataEnvSettingsStorage(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        /* setting value to null will delete the entry */
        public override EnvironmentSettings this[UUID regionID]
        {
            get
            {
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using(MySqlCommand cmd = new MySqlCommand("SELECT EnvironmentSettings FROM environmentsettings WHERE RegionID LIKE ?regionid", conn))
                    {
                        cmd.Parameters.AddWithValue("?regionid", regionID);
                        using(MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                using (MemoryStream ms = new MemoryStream((byte[])reader["EnvironmentSettings"]))
                                {
                                    return EnvironmentSettings.Deserialize(ms);
                                }
                            }
                        }
                    }
                }
                throw new KeyNotFoundException();
            }
            set
            {
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    if(value == null)
                    {
                        using(MySqlCommand cmd = new MySqlCommand("DELETE FROM environmentsettings WHERE RegionID LIKE ?regionid", conn))
                        {
                            cmd.Parameters.AddWithValue("?regionid", regionID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        Dictionary<string, object> param = new Dictionary<string,object>();
                        param["RegionID"] = regionID;
                        using(MemoryStream ms = new MemoryStream())
                        {
                            value.Serialize(ms, regionID);
                            param["EnvironmentSettings"] = ms.GetBuffer();
                        }
                        conn.ReplaceInsertInto("environmentsettings", param);
                    }
                }
            }
        }

        #region Migrations
        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "environmentsettings", Migrations, m_Log);
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "RegionID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "EnvironmentSettings LONGBLOB," +
                "PRIMARY KEY(RegionID))"
        };
        #endregion
    }
}
