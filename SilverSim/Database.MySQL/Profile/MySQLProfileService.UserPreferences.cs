// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Profile
{
    public sealed partial class MySQLProfileService : ProfileServiceInterface.IUserPreferencesInterface
    {
        bool IUserPreferencesInterface.TryGetValue(UUI user, out ProfilePreferences prefs)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM usersettings where useruuid LIKE ?uuid", conn))
                {
                    cmd.Parameters.AddParameter("?uuid", user.ID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            prefs = new ProfilePreferences();
                            prefs.User = user;
                            prefs.IMviaEmail = reader.GetBool("imviaemail");
                            prefs.Visible = reader.GetBool("visible");
                            return true;
                        }
                        else
                        {
                            prefs = new ProfilePreferences();
                            prefs.User = user;
                            prefs.IMviaEmail = false;
                            prefs.Visible = false;
                            return true;
                        }
                    }
                }
            }
        }

        ProfilePreferences IUserPreferencesInterface.this[UUI user]
        {
            get
            {
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using(MySqlCommand cmd = new MySqlCommand("SELECT * FROM usersettings where useruuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddParameter("?uuid", user.ID);
                        using(MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if(reader.Read())
                            {
                                ProfilePreferences prefs = new ProfilePreferences();
                                prefs.User = user;
                                prefs.IMviaEmail = reader.GetBool("imviaemail");
                                prefs.Visible = reader.GetBool("visible");
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
                replaceVals["useruuid"] = user.ID;
                replaceVals["imviaemail"] = value.IMviaEmail;
                replaceVals["visible"] = value.Visible;
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    conn.ReplaceInto("usersettings", replaceVals);
                }
            }
        }
    }
}
