// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Estate
{
    public partial class MySQLEstateService : IEstateGroupsServiceInterface, IEstateGroupsServiceListAccessInterface
    {
        List<UGI> IEstateGroupsServiceListAccessInterface.this[uint estateID]
        {
            get
            {
                List<UGI> estategroups = new List<UGI>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT GroupID FROM estate_groups WHERE EstateID = ?estateid", conn))
                    {
                        cmd.Parameters.AddParameter("?estateid", estateID);
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

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        bool IEstateGroupsServiceInterface.this[uint estateID, UGI group]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT GroupID FROM estate_groups WHERE EstateID = ?estateid AND GroupID LIKE ?groupid", conn))
                    {
                        cmd.Parameters.AddParameter("?estateid", estateID);
                        cmd.Parameters.AddParameter("?groupid", group.ID);
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
                        cmd.Parameters.AddParameter("?estateid", estateID);
                        cmd.Parameters.AddParameter("?groupid", group.ID);
                        if (cmd.ExecuteNonQuery() < 1)
                        {
                            throw new EstateUpdateFailedException();
                        }
                    }
                }
            }
        }

        IEstateGroupsServiceListAccessInterface IEstateGroupsServiceInterface.All
        {
            get 
            {
                return this;
            }
        }
    }
}
