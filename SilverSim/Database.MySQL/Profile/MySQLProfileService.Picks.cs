// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Types;
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

            public Dictionary<UUID, string> getPicks(UUI user)
            {
                Dictionary<UUID, string> res = new Dictionary<UUID, string>();
                using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using(MySqlCommand cmd = new MySqlCommand("SELECT pickuuid, `name` FROM userpicks WHERE creatoruuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?uuid", user.ID.ToString());
                        using(MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                res.Add(reader.GetUUID("pickuuid"), reader.GetString("name"));
                            }
                            return res;
                        }
                    }
                }
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
                            cmd.Parameters.AddWithValue("?uuid", id.ToString());
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
                                    pick.ParcelName = reader.GetString("parcelname");
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
                replaceVals["pickuuid"] = value.PickID.ToString();
                replaceVals["creatoruuid"] = value.Creator.ID.ToString();
                replaceVals["toppick"] = value.TopPick ? 1 : 0;
                replaceVals["parceluuid"] = value.ParcelID.ToString();
                replaceVals["name"] = value.Name;
                replaceVals["description"] = value.Description;
                replaceVals["snapshotuuid"] = value.SnapshotID.ToString();
                replaceVals["parcelname"] = value.ParcelName;
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

            public void Delete(UUID id)
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM userpicks WHERE pickuuid LIKE ?pickuuid", conn))
                    {
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
