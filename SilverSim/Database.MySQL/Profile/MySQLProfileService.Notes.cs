// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Profile
{
    public sealed partial class MySQLProfileService
    {
        public sealed class MySQLNotes : INotesInterface
        {
            readonly string m_ConnectionString;

            public MySQLNotes(string connectionString)
            {
                m_ConnectionString = connectionString;
            }

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            public string this[UUI user, UUI target]
            {
                get
                {
                    using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using(MySqlCommand cmd = new MySqlCommand("SELECT * FROM usernotes WHERE useruuid LIKE ?user AND targetuuid LIKE ?target", conn))
                        {
                            cmd.Parameters.AddWithValue("?user", user.ID.ToString());
                            cmd.Parameters.AddWithValue("?target", target.ID.ToString());
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
                    replaceVals["user"] = user.ID.ToString();
                    replaceVals["target"] = target.ID.ToString();
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
