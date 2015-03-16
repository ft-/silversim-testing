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

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Database.MySQL.Estate
{
    class MySQLEstateGroupsService : EstateGroupsServiceInterface
    {
        string m_ConnectionString;
        MySQLListAccess m_ListAccess;

        class MySQLListAccess : ListAccess
        {
            string m_ConnectionString;

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
                        cmd.Parameters.AddWithValue("?groupid", group.ID);
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
                if (value)
                {
                    query = "REPLACE INTO estate_groups (EstateID, GroupID) VALUES (?estateid, ?groupid)";
                }
                else
                {
                    query = "DELETE FROM estate_groups WHERE EstateID = ?estateid AND GroupID LIKE ?groupid";
                }

                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("?estateid", estateID);
                        cmd.Parameters.AddWithValue("?groupid", group.ID);
                        if (cmd.ExecuteNonQuery() < 1)
                        {
                            throw new EstateUpdateFailedException();
                        }
                    }
                }
            }
        }

        public override ListAccess All
        {
            get 
            {
                return m_ListAccess;
            }
        }
    }
}
