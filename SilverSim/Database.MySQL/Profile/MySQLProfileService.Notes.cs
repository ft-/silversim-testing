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

            public bool ContainsKey(UUI user, UUI target)
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT useruuid FROM usernotes WHERE useruuid LIKE ?user AND targetuuid LIKE ?target", conn))
                    {
                        cmd.Parameters.AddParameter("?user", user.ID);
                        cmd.Parameters.AddParameter("?target", target.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            public bool TryGetValue(UUI user, UUI target, out string notes)
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM usernotes WHERE useruuid LIKE ?user AND targetuuid LIKE ?target", conn))
                    {
                        cmd.Parameters.AddParameter("?user", user.ID);
                        cmd.Parameters.AddParameter("?target", target.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                notes = (string)reader["notes"];
                                return true;
                            }
                        }
                    }
                }

                notes = string.Empty;
                return false;
            }

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            public string this[UUI user, UUI target]
            {
                get
                {
                    string notes;
                    if(!TryGetValue(user, target, out notes))
                    {
                        throw new KeyNotFoundException();
                    }
                    return notes;
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
                        conn.ReplaceInto("usernotes", replaceVals);
                    }
                }
            }
        }
    }
}
