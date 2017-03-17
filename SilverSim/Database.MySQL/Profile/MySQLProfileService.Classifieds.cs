// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using MySql.Data.MySqlClient;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.Types;
using SilverSim.Types.Profile;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Profile
{
    public sealed partial class MySQLProfileService : ProfileServiceInterface.IClassifiedsInterface
    {
        Dictionary<UUID, string> IClassifiedsInterface.GetClassifieds(UUI user)
        {
            Dictionary<UUID, string> res = new Dictionary<UUID, string>();
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT classifieduuid, `name` FROM classifieds WHERE creatoruuid LIKE ?uuid", conn))
                {
                    cmd.Parameters.AddParameter("?uuid", user.ID);
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
        bool IClassifiedsInterface.TryGetValue(UUI user, UUID id, out ProfileClassified classified)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM classifieds WHERE classifieduuid LIKE ?uuid", conn))
                {
                    cmd.Parameters.AddParameter("?uuid", id);
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
                            classified.GlobalPos = reader.GetVector3("posglobal");
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

        bool IClassifiedsInterface.ContainsKey(UUI user, UUID id)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT classifieduuid FROM classifieds WHERE classifieduuid LIKE ?uuid", conn))
                {
                    cmd.Parameters.AddParameter("?uuid", id);
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

        ProfileClassified IClassifiedsInterface.this[UUI user, UUID id]
        {
            get 
            {
                ProfileClassified classified;
                if (!Classifieds.TryGetValue(user, id, out classified))
                {
                    throw new KeyNotFoundException();
                }
                return classified;
            }
        }

        void IClassifiedsInterface.Update(ProfileClassified c)
        {
            Dictionary<string, object> replaceVals = new Dictionary<string, object>();
            replaceVals["classifieduuid"] = c.ClassifiedID;
            replaceVals["creatoruuid"] = c.Creator.ID;
            replaceVals["creationdate"] = c.CreationDate;
            replaceVals["expirationdate"] = c.ExpirationDate;
            replaceVals["category"] = c.Category;
            replaceVals["name"] = c.Name;
            replaceVals["description"] = c.Description;
            replaceVals["parceluuid"] = c.ParcelID;
            replaceVals["parentestate"] = c.ParentEstate;
            replaceVals["snapshotuuid"] = c.SnapshotID;
            replaceVals["simname"] = c.SimName;
            replaceVals["posglobal"] = c.GlobalPos;
            replaceVals["parcelname"] = c.ParcelName;
            replaceVals["classifiedflags"] = c.Flags;
            replaceVals["priceforlisting"] = c.Price;
            using(MySqlConnection conn = new MySqlConnection())
            {
                conn.Open();
                conn.ReplaceInto("classifieds", replaceVals);
            }
        }

        void IClassifiedsInterface.Delete(UUID id)
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM classifieds WHERE classifieduuid LIKE ?classifieduuid", conn))
                {
                    cmd.Parameters.AddParameter("?classifieduuid", id);
                    if (1 > cmd.ExecuteNonQuery())
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }
    }
}
