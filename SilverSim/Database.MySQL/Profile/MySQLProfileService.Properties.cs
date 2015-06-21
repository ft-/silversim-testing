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
        class MySQLProperties : IPropertiesInterface
        {
            string m_ConnectionString;

            public MySQLProperties(string connectionString)
            {
                m_ConnectionString = connectionString;
            }

            public ProfileProperties this[UUI user]
            {
                get
                {
                    using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM userprofile where useruuid LIKE ?uuid", conn))
                        {
                            cmd.Parameters.AddWithValue("?uuid", user.ID);
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    ProfileProperties props = new ProfileProperties();
                                    props.User = user;
                                    props.Partner.ID = reader.GetUUID("profilePartner");
                                    props.PublishProfile = reader.GetBoolean("profileAllowPublish");
                                    props.PublishMature = reader.GetBoolean("profileMaturePublish");
                                    props.WebUrl = reader.GetString("profileURL");
                                    props.WantToMask = reader.GetUInt32("profileWantToMask");
                                    props.WantToText = reader.GetString("profileWantToText");
                                    props.SkillsMask = reader.GetUInt32("profileSkillsMask");
                                    props.SkillsText = reader.GetString("profileSkillsText");
                                    props.Language = reader.GetString("profileLanguages");
                                    props.ImageID = reader.GetUUID("profileImage");
                                    props.AboutText = reader.GetString("profileAboutText");
                                    props.FirstLifeImageID = reader.GetString("profileFirstImage");
                                    props.FirstLifeText = reader.GetString("profileFirstText");
                                    return props;
                                }
                                else
                                {
                                    ProfileProperties props = new ProfileProperties();
                                    props.User = user;
                                    return props;
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
                    replaceVals["profileAllowPublish"] = value.PublishProfile ? 1 : 0;
                    replaceVals["profileMaturePublish"] = value.PublishMature ? 1 : 0;
                    replaceVals["profileURL"] = value.WebUrl;
                    replaceVals["profileWantToMask"] = value.WantToMask;
                    replaceVals["profileWantToText"] = value.WantToText;
                    replaceVals["profileSkillsMask"] = value.SkillsMask;
                    replaceVals["profileSkillsText"] = value.SkillsText;
                    replaceVals["profileLanguages"] = value.Language;
                    replaceVals["profileImage"] = value.ImageID;
                    replaceVals["profileAboutText"] = value.AboutText;
                    replaceVals["profileFirstImage"] = value.FirstLifeImageID;
                    replaceVals["profileFirstText"] = value.FirstLifeText;

                    using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            conn.InsertInto("userprofile", replaceVals);
                        }
                        catch
                        {
                            replaceVals.Remove("useruuid");
                            conn.UpdateSet("userprofile", replaceVals, "useruuid LIKE '" + user.ID + "'");
                        }
                    }
                }
            }
        }
    }
}
