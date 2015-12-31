// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.ServiceInterfaces.Estate;
using MySql.Data.MySqlClient;

namespace SilverSim.Database.MySQL.Estate
{
    public sealed class MySQLEstateRegionMapInterface : IEstateRegionMapServiceInterface
    {
        readonly string m_ConnectionString;

        public MySQLEstateRegionMapInterface(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public List<UUID> this[uint estateID]
        {
            get 
            {
                List<UUID> regionList = new List<UUID>();
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT RegionID FROM estate_regionmap WHERE EstateID = ?estateid", conn))
                    {
                        cmd.Parameters.AddWithValue("?estateid", estateID);
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

        public bool TryGetValue(UUID regionID, out uint estateID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT EstateID FROM estate_regionmap WHERE RegionID = ?regionid", conn))
                {
                    cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            estateID = (uint)reader["EstateID"];
                            return true;
                        }
                    }
                }
            }

            estateID = 0;
            return false;
        }

        public bool Remove(UUID regionID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM estate_regionmap WHERE RegionID = ?regionid", conn))
                {
                    cmd.Parameters.AddWithValue("?regionid", regionID.ToString());
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public uint this[UUID regionID]
        {
            get
            {
                uint estateID;
                if(!TryGetValue(regionID, out estateID))
                {
                    throw new KeyNotFoundException();
                }
                return estateID;
            }
            set
            {
                Dictionary<string, object> vals = new Dictionary<string,object>();
                vals["EstateID"] = value;
                vals["RegionID"] = regionID.ToString();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    conn.ReplaceInto("estate_regionmap", vals);
                }
            }
        }
    }
}
