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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.ServiceInterfaces.Estate;
using MySql.Data.MySqlClient;

namespace SilverSim.Database.MySQL.Estate
{
    public class MySQLEstateRegionMapInterface : EstateRegionMapServiceInterface
    {
        string m_ConnectionString;

        public MySQLEstateRegionMapInterface(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public List<UUID> this[uint EstateID]
        {
            get 
            {
                List<UUID> regionList = new List<UUID>();
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT RegionID FROM estate_regionmap WHERE EstateID = ?estateid", conn))
                    {
                        cmd.Parameters.AddWithValue("?estateid", EstateID);
                        using(MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                regionList.Add(reader.GetUUID("RegionID"));
                            }
                        }
                    }
                }
                return regionList;
            }
        }

        public uint this[UUID regionID]
        {
            get
            {
                List<UUID> regionList = new List<UUID>();
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT EstateID FROM estate_regionmap WHERE RegionID = ?regionid", conn))
                    {
                        cmd.Parameters.AddWithValue("?regionid", regionID);
                        using(MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if(reader.Read())
                            {
                                return (uint)reader["EstateID"];
                            }
                        }
                    }
                }
                throw new KeyNotFoundException();
            }
            set
            {
                Dictionary<string, object> vals = new Dictionary<string,object>();
                vals["EstateID"] = value;
                vals["RegionID"] = regionID;
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    conn.ReplaceInsertInto("estate_regionmap", vals);
                }
            }
        }
    }
}
