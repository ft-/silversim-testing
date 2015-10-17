// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Estate
{
    class MySQLEstateManagerService : EstateManagerServiceInterface
    {
        string m_ConnectionString;

        class MySQLListAccess : IListAccess
        {
            string m_ConnectionString;

            public MySQLListAccess(string connectionString)
            {
                m_ConnectionString = connectionString;
            }

            public List<UUI> this[uint estateID]
            {
                get 
                {
                    List<UUI> estatemanagers = new List<UUI>();
                    using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using(MySqlCommand cmd = new MySqlCommand("SELECT UserID FROM estate_managers WHERE EstateID = ?estateid", conn))
                        {
                            cmd.Parameters.AddWithValue("?estateid", estateID);
                            using(MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                while(reader.Read())
                                {
                                    UUI uui = new UUI();
                                    uui.ID = reader.GetUUID("UserID");
                                    estatemanagers.Add(uui);
                                }
                            }
                        }
                    }
                    return estatemanagers;
                }
            }
        }

        MySQLListAccess m_ListAccess;

        public MySQLEstateManagerService(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_ListAccess = new MySQLListAccess(connectionString);
        }

        public override bool this[uint estateID, UUI agent]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT UserID FROM estate_managers WHERE EstateID = ?estateid AND UserID LIKE ?userid", conn))
                    {
                        cmd.Parameters.AddWithValue("?estateid", estateID);
                        cmd.Parameters.AddWithValue("?userid", agent.ID.ToString());
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            return reader.Read();
                        }
                    }
                }
            }
            set
            {
                string query;
                if(value)
                {
                    query = "REPLACE INTO estate_managers (EstateID, UserID) VALUES (?estateid, ?userid)";
                }
                else
                {
                    query = "DELETE FROM estate_managers WHERE EstateID = ?estateid AND UserID LIKE ?userid";
                }

                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("?estateid", estateID);
                        cmd.Parameters.AddWithValue("?userid", agent.ID.ToString());
                        if (cmd.ExecuteNonQuery() < 1)
                        {
                            throw new EstateUpdateFailedException();
                        }
                    }
                }
            }
        }

        public override EstateManagerServiceInterface.IListAccess All
        {
            get
            {
                return m_ListAccess;
            }
        }
    }
}
