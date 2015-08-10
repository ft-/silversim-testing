// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Profile
{
    public partial class MySQLProfileService
    {
        class MySQLNotes : INotesInterface
        {
            string m_ConnectionString;

            public MySQLNotes(string connectionString)
            {
                m_ConnectionString = connectionString;
            }

            public string this[UUI user, UUI target]
            {
                get
                {
                    using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using(MySqlCommand cmd = new MySqlCommand("SELECT * FROM usernotes WHERE useruuid LIKE ?user AND targetuuid LIKE ?target", conn))
                        {
                            cmd.Parameters.AddWithValue("?user", user.ID);
                            cmd.Parameters.AddWithValue("?target", target.ID);
                            using(MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                if(reader.Read())
                                {
                                    return (string)reader["notes"];
                                }
                            }
                        }
                    }
                    throw new KeyNotFoundException();
                }
                set
                {
                    Dictionary<string, object> replaceVals = new Dictionary<string, object>();
                    replaceVals["user"] = user.ID;
                    replaceVals["target"] = target.ID;
                    replaceVals["notes"] = value;
                    using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        conn.ReplaceInsertInto("usernotes", replaceVals);
                    }
                }
            }
        }
    }
}
