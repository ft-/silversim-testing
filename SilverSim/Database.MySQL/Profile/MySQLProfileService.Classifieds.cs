// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Profile
{
    public sealed partial class MySQLProfileService
    {
        public sealed class MySQLClassifieds : IClassifiedsInterface
        {
            readonly string m_ConnectionString;

            public MySQLClassifieds(string connectionString)
            {
                m_ConnectionString = connectionString;
            }

            public Dictionary<UUID, string> GetClassifieds(UUI user)
            {
                Dictionary<UUID, string> res = new Dictionary<UUID, string>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT classifieduuid, `name` FROM classifieds WHERE creatoruuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?uuid", user.ID.ToString());
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                res.Add(reader.GetUUID("classifieduuid"), reader.GetString("name"));
                            }
                            return res;
                        }
                    }
                }
            }

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            public bool TryGetValue(UUI user, UUID id, out ProfileClassified classified)
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM classifieds WHERE classifieduuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?uuid", id.ToString());
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                classified = new ProfileClassified();
                                classified.ClassifiedID = reader.GetUUID("classifieduuid");
                                classified.Category = reader.GetInt32("category");
                                classified.CreationDate = reader.GetDate("creationdate");
                                classified.Creator.ID = reader.GetUUID("creatoruuid");
                                classified.Description = reader.GetString("description");
                                classified.ExpirationDate = reader.GetDate("expirationdate");
                                classified.Flags = reader.GetByte("classifiedflags");
                                classified.GlobalPos = reader.GetVector("posglobal");
                                classified.Name = reader.GetString("name");
                                classified.ParcelID = reader.GetUUID("parceluuid");
                                classified.ParcelName = reader.GetString("parcelname");
                                classified.ParentEstate = reader.GetInt32("parentestate");
                                classified.Price = reader.GetInt32("priceforlisting");
                                classified.SimName = reader.GetString("simname");
                                classified.SnapshotID = reader.GetUUID("snapshotuuid");
                                return true;
                            }
                        }
                    }
                }
                classified = default(ProfileClassified);
                return false;
            }

            public bool ContainsKey(UUI user, UUID id)
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT classifieduuid FROM classifieds WHERE classifieduuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?uuid", id.ToString());
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

            public ProfileClassified this[UUI user, UUID id]
            {
                get 
                {
                    ProfileClassified classified;
                    if (!TryGetValue(user, id, out classified))
                    {
                        throw new KeyNotFoundException();
                    }
                    return classified;
                }
            }

            public void Update(ProfileClassified c)
            {
                Dictionary<string, object> replaceVals = new Dictionary<string, object>();
                replaceVals["classifieduuid"] = c.ClassifiedID.ToString();
                replaceVals["creatoruuid"] = c.Creator.ID.ToString();
                replaceVals["creationdate"] = c.CreationDate.AsULong;
                replaceVals["expirationdate"] = c.ExpirationDate.AsULong;
                replaceVals["category"] = c.Category;
                replaceVals["name"] = c.Name;
                replaceVals["description"] = c.Description;
                replaceVals["parceluuid"] = c.ParcelID.ToString();
                replaceVals["parentestate"] = c.ParentEstate;
                replaceVals["snapshotuuid"] = c.SnapshotID.ToString();
                replaceVals["simname"] = c.SimName;
                replaceVals["posglobal"] = c.GlobalPos.ToString();
                replaceVals["parcelname"] = c.ParcelName;
                replaceVals["classifiedflags"] = c.Flags;
                replaceVals["priceforlisting"] = c.Price;
                using(MySqlConnection conn = new MySqlConnection())
                {
                    conn.Open();
                    conn.ReplaceInsertInto("classifieds", replaceVals);
                }
            }

            public void Delete(UUID id)
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM classifieds WHERE classifieduuid LIKE ?classifieduuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?classifieduuid", id.ToString());
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
