// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Estate
{
    public partial class MySQLEstateService : IEstateRegionMapServiceInterface
    {
        List<UUID> IEstateRegionMapServiceInterface.this[uint estateID]
        {
            get 
            {
                List<UUID> regionList = new List<UUID>();
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT RegionID FROM estate_regionmap WHERE EstateID = ?estateid", conn))
                    {
                        cmd.Parameters.AddParameter("?estateid", estateID);
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

        bool IEstateRegionMapServiceInterface.TryGetValue(UUID regionID, out uint estateID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT EstateID FROM estate_regionmap WHERE RegionID = ?regionid", conn))
                {
                    cmd.Parameters.AddParameter("?regionid", regionID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            estateID = reader.GetUInt32("EstateID");
                            return true;
                        }
                    }
                }
            }

            estateID = 0;
            return false;
        }

        bool IEstateRegionMapServiceInterface.Remove(UUID regionID)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM estate_regionmap WHERE RegionID = ?regionid", conn))
                {
                    cmd.Parameters.AddParameter("?regionid", regionID);
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

        uint IEstateRegionMapServiceInterface.this[UUID regionID]
        {
            get
            {
                uint estateID;
                if(!RegionMap.TryGetValue(regionID, out estateID))
                {
                    throw new KeyNotFoundException();
                }
                return estateID;
            }
            set
            {
                Dictionary<string, object> vals = new Dictionary<string,object>();
                vals["EstateID"] = value;
                vals["RegionID"] = regionID;
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    conn.ReplaceInto("estate_regionmap", vals);
                }
            }
        }
    }
}
