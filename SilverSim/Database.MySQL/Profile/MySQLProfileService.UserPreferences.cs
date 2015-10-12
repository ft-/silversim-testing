﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Profile
{
    public partial class MySQLProfileService
    {
        class MySQLUserPreferences : IUserPreferencesInterface
        {
            string m_ConnectionString;

            public MySQLUserPreferences(string connectionString)
            {
                m_ConnectionString = connectionString;
            }

            public ProfilePreferences this[UUI user]
            {
                get
                {
                    using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using(MySqlCommand cmd = new MySqlCommand("SELECT * FROM usersettings where useruuid LIKE ?uuid", conn))
                        {
                            cmd.Parameters.AddWithValue("?uuid", user.ID.ToString());
                            using(MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                if(reader.Read())
                                {
                                    ProfilePreferences prefs = new ProfilePreferences();
                                    prefs.User = user;
                                    prefs.IMviaEmail = reader.GetBoolean("imviaemail");
                                    prefs.Visible = reader.GetBoolean("visible");
                                    return prefs;
                                }
                                else
                                {
                                    ProfilePreferences prefs = new ProfilePreferences();
                                    prefs.User = user;
                                    prefs.IMviaEmail = false;
                                    prefs.Visible = false;
                                    return prefs;
                                }
                            }
                        }
                    }
                }
                set
                {
                    Dictionary<string, object> replaceVals = new Dictionary<string, object>();
                    replaceVals["useruuid"] = user.ID.ToString();
                    replaceVals["imviaemail"] = value.IMviaEmail ? 1 : 0;
                    replaceVals["visible"] = value.Visible ? 1 : 0;
                    using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        conn.ReplaceInsertInto("usersettings", replaceVals);
                    }
                }
            }
        }
    }
}
