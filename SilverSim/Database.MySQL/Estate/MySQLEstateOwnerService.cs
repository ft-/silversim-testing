// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Estate
{
    public sealed class MySQLEstateOwnerService : IEstateOwnerServiceInterface
    {
        readonly string m_ConnectionString;

        public MySQLEstateOwnerService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public bool TryGetValue(uint estateID, out UUI uui)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT Owner FROM estates WHERE ID = ?id", conn))
                {
                    cmd.Parameters.AddParameter("?id", estateID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            uui = reader.GetUUI("Owner");
                            return true;
                        }
                    }
                }
            }
            uui = default(UUI);
            return false;
        }

        public List<uint> this[UUI owner]
        {
            get
            {
                List<uint> estates = new List<uint>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT ID, Owner FROM estates WHERE Owner LIKE \"" + owner.ID.ToString() + "%\"", conn))
                    {
                        cmd.Parameters.AddParameter("?id", owner.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                UUI uui = reader.GetUUI("Owner");
                                if (uui.EqualsGrid(owner))
                                {
                                    estates.Add(reader.GetUInt32("ID"));
                                }
                            }
                            return estates;
                        }
                    }
                }

            }
        }

        public UUI this[uint estateID]
        {
            get
            {
                UUI uui;
                if(!TryGetValue(estateID, out uui))
                {
                    throw new KeyNotFoundException();
                }
                return uui;
            }
            set
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("UPDATE estates SET Owner = ?ownerid WHERE ID = ?id", conn))
                    {
                        cmd.Parameters.AddParameter("?id", estateID);
                        cmd.Parameters.AddParameter("?ownerid", value);
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
