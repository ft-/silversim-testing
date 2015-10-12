// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
                            cmd.Parameters.AddWithValue("?uuid", user.ID.ToString());
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
                }
            }
            public ProfileProperties this[UUI user, PropertiesUpdateFlags flags]
            {
                set
                {
                    Dictionary<string, object> replaceVals = new Dictionary<string, object>();
                    replaceVals["useruuid"] = user.ID.ToString();
                    if ((flags & PropertiesUpdateFlags.Properties) != 0)
                    {
                        replaceVals["profileAllowPublish"] = value.PublishProfile ? 1 : 0;
                        replaceVals["profileMaturePublish"] = value.PublishMature ? 1 : 0;
                        replaceVals["profileURL"] = value.WebUrl;
                        replaceVals["profileImage"] = value.ImageID.ToString();
                        replaceVals["profileAboutText"] = value.AboutText;
                        replaceVals["profileFirstImage"] = value.FirstLifeImageID.ToString();
                        replaceVals["profileFirstText"] = value.FirstLifeText;
                    }
                    if((flags & PropertiesUpdateFlags.Interests) != 0)
                    {
                        replaceVals["profileWantToMask"] = value.WantToMask;
                        replaceVals["profileWantToText"] = value.WantToText;
                        replaceVals["profileSkillsMask"] = value.SkillsMask;
                        replaceVals["profileSkillsText"] = value.SkillsText;
                        replaceVals["profileLanguages"] = value.Language;
                    }

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
