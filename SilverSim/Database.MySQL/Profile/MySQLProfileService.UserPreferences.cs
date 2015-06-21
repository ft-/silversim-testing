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
                            cmd.Parameters.AddWithValue("?uuid", user.ID);
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
                    throw new KeyNotFoundException();
                }
                set
                {
                    Dictionary<string, object> replaceVals = new Dictionary<string, object>();
                    replaceVals["useruuid"] = user.ID;
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
