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
    public sealed class MySQLEstateGroupsService : EstateGroupsServiceInterface
    {
        readonly string m_ConnectionString;
        readonly MySQLListAccess m_ListAccess;

        public sealed class MySQLListAccess : IListAccess
        {
            readonly string m_ConnectionString;

            public MySQLListAccess(string connectionString)
            {
                m_ConnectionString = connectionString;
            }

            public List<UGI> this[uint estateID]
            {
                get
                {
                    List<UGI> estategroups = new List<UGI>();
                    using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand("SELECT GroupID FROM estate_groups WHERE EstateID = ?estateid", conn))
                        {
                            cmd.Parameters.AddWithValue("?estateid", estateID);
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    UGI ugi = new UGI();
                                    ugi.ID = reader.GetUUID("GroupID");
                                    estategroups.Add(ugi);
                                }
                            }
                        }
                    }
                    return estategroups;
                }
            }
        }

        public MySQLEstateGroupsService(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_ListAccess = new MySQLListAccess(connectionString);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override bool this[uint estateID, UGI group]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT GroupID FROM estate_groups WHERE EstateID = ?estateid AND GroupID LIKE ?groupid", conn))
                    {
                        cmd.Parameters.AddWithValue("?estateid", estateID);
                        cmd.Parameters.AddWithValue("?groupid", group.ID.ToString());
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
                    "REPLACE INTO estate_groups (EstateID, GroupID) VALUES (?estateid, ?groupid)" : 
                    "DELETE FROM estate_groups WHERE EstateID = ?estateid AND GroupID LIKE ?groupid";

                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("?estateid", estateID);
                        cmd.Parameters.AddWithValue("?groupid", group.ID.ToString());
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
