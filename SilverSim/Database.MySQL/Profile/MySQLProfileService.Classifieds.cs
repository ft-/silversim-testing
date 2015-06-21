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
        class MySQLClassifieds : IClassifiedsInterface
        {
            string m_ConnectionString;

            public MySQLClassifieds(string connectionString)
            {
                m_ConnectionString = connectionString;
            }

            public List<UUID> getClassifieds(UUI user)
            {
                List<UUID> res = new List<UUID>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT classifieduuid FROM classifieds WHERE creatoruuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?uuid", user.ID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                res.Add(reader.GetUUID("classifieduuid"));
                            }
                            return res;
                        }
                    }
                }
                throw new KeyNotFoundException();
            }

            public ProfileClassified this[UUI user, UUID id]
            {
                get 
                {
                    using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM classifieds WHERE classifieduuid LIKE ?uuid", conn))
                        {
                            cmd.Parameters.AddWithValue("?uuid", id);
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    ProfileClassified classified = new ProfileClassified();
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
                                    return classified;
                                }
                            }
                        }
                    }
                    throw new KeyNotFoundException();
                }
            }

            public void Update(ProfileClassified c)
            {
                Dictionary<string, object> replaceVals = new Dictionary<string, object>();
                replaceVals["classifieduuid"] = c.ClassifiedID;
                replaceVals["creatoruuid"] = c.Creator.ID;
                replaceVals["creationdate"] = c.CreationDate.AsULong;
                replaceVals["expirationdate"] = c.ExpirationDate.AsULong;
                replaceVals["category"] = c.Category;
                replaceVals["name"] = c.Name;
                replaceVals["description"] = c.Description;
                replaceVals["parceluuid"] = c.ParcelID;
                replaceVals["parentestate"] = c.ParentEstate;
                replaceVals["snapshotuuid"] = c.SnapshotID;
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

            public void Delete(UUI user, UUID id)
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM classifieds WHERE creatoruuid LIKE ?user AND classifieduuid LIKE ?pickuuid", conn))
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
