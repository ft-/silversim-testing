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
