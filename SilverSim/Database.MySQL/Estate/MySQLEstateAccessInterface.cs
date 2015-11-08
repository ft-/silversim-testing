// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SilverSim.Database.MySQL.Estate
{
    public sealed class MySQLEstateAccessInterface : EstateAccessServiceInterface
    {
        public sealed class MySQLListAccess : IListAccess
        {
            readonly string m_ConnectionString;
            public MySQLListAccess(string connectionString)
            {
                m_ConnectionString = connectionString;
            }


            public List<UUI> this[uint estateID]
            {
                get 
                {
                    List<UUI> estateusers = new List<UUI>();
                    using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand("SELECT UserID FROM estate_users WHERE EstateID = ?estateid", conn))
                        {
                            cmd.Parameters.AddWithValue("?estateid", estateID);
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    UUI uui = new UUI();
                                    uui.ID = reader.GetUUID("UserID");
                                    estateusers.Add(uui);
                                }
                            }
                        }
                    }
                    return estateusers;
                }
            }
        }

        readonly MySQLListAccess m_ListAccess;
        readonly string m_ConnectionString;

        public MySQLEstateAccessInterface(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_ListAccess = new MySQLListAccess(connectionString);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override bool this[uint estateID, UUI agent]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT UserID FROM estate_users WHERE EstateID = ?estateid AND UserID LIKE ?userid", conn))
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
                string query = value ?
                    query = "REPLACE INTO estate_users (EstateID, UserID) VALUES (?estateid, ?userid)" :
                    query = "DELETE FROM estate_users WHERE EstateID = ?estateid AND UserID LIKE ?userid";

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

        public override IListAccess All
        {
            get 
            {
                return m_ListAccess;
            }
        }
    }
}
