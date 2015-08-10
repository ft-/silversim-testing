// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Estate
{
    class MySQLEstateOwnerService : IEstateOwnerServiceInterface
    {
        string m_ConnectionString;

        public MySQLEstateOwnerService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public UUI this[uint EstateID]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT OwnerID FROM estates WHERE ID = ?id", conn))
                    {
                        cmd.Parameters.AddWithValue("?id", EstateID);
                        using(MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if(reader.Read())
                            {
                                UUI uui = new UUI();
                                uui.ID = reader.GetUUID("OwnerID");
                                return uui;
                            }
                        }
                    }
                }
                throw new KeyNotFoundException();
            }
            set
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("UPDATE estates SET OwnerID = ?ownerid WHERE ID = ?id", conn))
                    {
                        cmd.Parameters.AddWithValue("?id", EstateID);
                        cmd.Parameters.AddWithValue("?ownerid", value.ID);
                        if(cmd.ExecuteNonQuery() < 1)
                        {
                            throw new EstateUpdateFailedException();
                        }
                    }
                }
            }
        }
    }
}
