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

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Types;
using SilverSim.Types.Presence;
using SilverSim.Types.Profile;
using System;
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

            public ProfileNotes this[UUI user, UUI target]
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
                                    ProfileNotes notes = new ProfileNotes();
                                    notes.User = user;
                                    notes.Target = target;
                                    notes.Notes = (string)reader["notes"];
                                    return notes;
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
                    replaceVals["notes"] = value.Notes;
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
