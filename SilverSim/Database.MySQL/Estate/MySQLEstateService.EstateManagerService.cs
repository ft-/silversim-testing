// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Estate
{
    public partial class MySQLEstateService : IEstateManagerServiceInterface, IEstateManagerServiceListAccessInterface
    {
        List<UUI> IEstateManagerServiceListAccessInterface.this[uint estateID]
        {
            get 
            {
                List<UUI> estatemanagers = new List<UUI>();
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using(MySqlCommand cmd = new MySqlCommand("SELECT UserID FROM estate_managers WHERE EstateID = ?estateid", conn))
                    {
                        cmd.Parameters.AddParameter("?estateid", estateID);
                        using(MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                estatemanagers.Add(reader.GetUUI("UserID"));
                            }
                        }
                    }
                }
                return estatemanagers;
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        bool IEstateManagerServiceInterface.this[uint estateID, UUI agent]
        {
            get
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT UserID FROM estate_managers WHERE EstateID = ?estateid AND UserID LIKE \"" + agent.ID.ToString() + "%\"", conn))
                    {
                        cmd.Parameters.AddParameter("?estateid", estateID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                UUI uui = reader.GetUUI("UserID");
                                if(uui.EqualsGrid(agent))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                    }
                }
            }
            set
            {
                string query = value ? 
                    "REPLACE INTO estate_managers (EstateID, UserID) VALUES (?estateid, ?userid)" :
                    "DELETE FROM estate_managers WHERE EstateID = ?estateid AND UserID LIKE \"" + agent.ID.ToString() + "%\"";

                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddParameter("?estateid", estateID);
                        if (value)
                        {
                            cmd.Parameters.AddParameter("?userid", agent.ID);
                        }
                        if (cmd.ExecuteNonQuery() < 1 && value)
                        {
                            throw new EstateUpdateFailedException();
                        }
                    }
                }
            }
        }

        IEstateManagerServiceListAccessInterface IEstateManagerServiceInterface.All
        {
            get
            {
                return this;
            }
        }
    }
}
