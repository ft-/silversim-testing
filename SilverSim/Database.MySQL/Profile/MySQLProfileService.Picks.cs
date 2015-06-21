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
        class MySQLPicks : IPicksInterface
        {
            string m_ConnectionString;

            public MySQLPicks(string connectionString)
            {
                m_ConnectionString = connectionString;
            }

            public List<UUID> getPicks(UUI user)
            {
                List<UUID> res = new List<UUID>();
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using(MySqlCommand cmd = new MySqlCommand("SELECT pickuuid FROM userpicks WHERE creatoruuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?uuid", user.ID);
                        using(MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                res.Add(reader.GetUUID("pickuuid"));
                            }
                            return res;
                        }
                    }
                }
                throw new KeyNotFoundException();
            }

            public ProfilePick this[UUI user, UUID id]
            {
                get 
                { 
                    using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using(MySqlCommand cmd = new MySqlCommand("SELECT * FROM userpicks WHERE pickuuid LIKE ?uuid", conn))
                        {
                            cmd.Parameters.AddWithValue("?uuid", id);
                            using(MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                if(reader.Read())
                                {
                                    ProfilePick pick = new ProfilePick();
                                    pick.Creator.ID = reader.GetUUID("creatoruuid");
                                    pick.Description = reader.GetString("description");
                                    pick.Enabled = reader.GetBoolean("enabled");
                                    pick.Name = reader.GetString("name");
                                    pick.OriginalName = reader.GetString("originalname");
                                    pick.ParcelID = reader.GetUUID("parceluuid");
                                    pick.PickID = reader.GetUUID("pickuuid");
                                    pick.SimName = reader.GetString("simname");
                                    pick.SnapshotID = reader.GetUUID("snapshotuuid");
                                    pick.SortOrder = reader.GetInt32("sortorder");
                                    pick.TopPick = reader.GetBoolean("toppick");
                                    pick.GlobalPosition = reader.GetVector("posglobal");
                                    pick.User = reader.GetString("User");
                                    return pick;
                                }
                            }
                        }
                    }
                    throw new KeyNotFoundException();
                }
            }

            public void Update(ProfilePick value)
            {
                Dictionary<string, object> replaceVals = new Dictionary<string, object>();
                replaceVals["pickuuid"] = value.PickID;
                replaceVals["creatoruuid"] = value.Creator.ID;
                replaceVals["toppick"] = value.TopPick ? 1 : 0;
                replaceVals["parceluuid"] = value.ParcelID;
                replaceVals["name"] = value.Name;
                replaceVals["description"] = value.Description;
                replaceVals["snapshotuuid"] = value.SnapshotID;
                replaceVals["user"] = value.User;
                replaceVals["originalname"] = value.OriginalName;
                replaceVals["simname"] = value.SimName;
                replaceVals["posglobal"] = value.GlobalPosition.ToString();
                replaceVals["sortorder"] = value.SortOrder;
                replaceVals["enabled"] = value.Enabled ? 1 : 0;
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    conn.ReplaceInsertInto("userpicks", replaceVals);
                }
            }

            public void Delete(UUI user, UUID id)
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM userpicks WHERE creatoruuid LIKE ?user AND pickuuid LIKE ?pickuuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?user", user.ID);
                        cmd.Parameters.AddWithValue("?pickuuid", id);
                        if (1 > cmd.ExecuteNonQuery())
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }
            }
        }
    }
}
