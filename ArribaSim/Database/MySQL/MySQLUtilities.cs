/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using ArribaSim.StructuredData.LLSD;
using ArribaSim.Types;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.IO;

namespace ArribaSim.Database.MySQL
{
    public static class MySQLUtilities
    {
        public static void ReplaceInsertInto(MySqlConnection connection, string tablename, Dictionary<string, object> vals)
        {
            string q1 = "REPLACE INSERT INTO ?tablename (";
            string q2 = ") VALUES (";
            bool first = true;
            foreach(KeyValuePair<string, object> kvp in vals)
            {
                if (!first)
                {
                    q1 += ",";
                    q2 += ",";
                }
                first = false;

                if (kvp.Value is Vector3)
                {
                    q1 += kvp.Key.ToString() + "X";
                    q2 += "?" + kvp.Key.ToString() + "X";
                    q1 += kvp.Key.ToString() + "Y";
                    q2 += "?" + kvp.Key.ToString() + "Y";
                    q1 += kvp.Key.ToString() + "Z";
                    q2 += "?" + kvp.Key.ToString() + "Z";
                }
                else if (kvp.Value is Quaternion)
                {
                    q1 += kvp.Key.ToString() + "X";
                    q2 += "?" + kvp.Key.ToString() + "X";
                    q1 += kvp.Key.ToString() + "Y";
                    q2 += "?" + kvp.Key.ToString() + "Y";
                    q1 += kvp.Key.ToString() + "Z";
                    q2 += "?" + kvp.Key.ToString() + "Z";
                    q1 += kvp.Key.ToString() + "W";
                    q2 += "?" + kvp.Key.ToString() + "W";
                }
                else if(kvp.Value is Color)
                {
                    q1 += kvp.Key.ToString() + "Red";
                    q2 += "?" + kvp.Key.ToString() + "Red";
                    q1 += kvp.Key.ToString() + "Green";
                    q2 += "?" + kvp.Key.ToString() + "Green";
                    q1 += kvp.Key.ToString() + "Blue";
                    q2 += "?" + kvp.Key.ToString() + "Blue";
                }
                else if (kvp.Value is ColorAlpha)
                {
                    q1 += kvp.Key.ToString() + "Red";
                    q2 += "?" + kvp.Key.ToString() + "Red";
                    q1 += kvp.Key.ToString() + "Green";
                    q2 += "?" + kvp.Key.ToString() + "Green";
                    q1 += kvp.Key.ToString() + "Blue";
                    q2 += "?" + kvp.Key.ToString() + "Blue";
                    q1 += kvp.Key.ToString() + "Alpha";
                    q2 += "?" + kvp.Key.ToString() + "Alpha";
                }
                else
                {
                    q1 += kvp.Key.ToString();
                    q2 += "?" + kvp.Key.ToString();
                }
            }

            using(MySqlCommand command = new MySqlCommand(q1 + q2 + ")", connection))
            {
                foreach (KeyValuePair<string, object> kvp in vals)
                {
                    if (kvp.Value is Vector3)
                    {
                        command.Parameters.AddWithValue("?" + kvp.Key + "X", ((Vector3)kvp.Value).X);
                        command.Parameters.AddWithValue("?" + kvp.Key + "Y", ((Vector3)kvp.Value).Y);
                        command.Parameters.AddWithValue("?" + kvp.Key + "Z", ((Vector3)kvp.Value).Z);
                    }
                    else if (kvp.Value is Quaternion)
                    {
                        command.Parameters.AddWithValue("?" + kvp.Key + "X", ((Quaternion)kvp.Value).X);
                        command.Parameters.AddWithValue("?" + kvp.Key + "Y", ((Quaternion)kvp.Value).Y);
                        command.Parameters.AddWithValue("?" + kvp.Key + "Z", ((Quaternion)kvp.Value).Z);
                        command.Parameters.AddWithValue("?" + kvp.Key + "W", ((Quaternion)kvp.Value).W);
                    }
                    else if (kvp.Value is Color)
                    {
                        command.Parameters.AddWithValue("?" + kvp.Key + "Red", ((Color)kvp.Value).R);
                        command.Parameters.AddWithValue("?" + kvp.Key + "Green", ((Color)kvp.Value).G);
                        command.Parameters.AddWithValue("?" + kvp.Key + "Blue", ((Color)kvp.Value).B);
                    }
                    else if (kvp.Value is ColorAlpha)
                    {
                        command.Parameters.AddWithValue("?" + kvp.Key + "Red", ((ColorAlpha)kvp.Value).R);
                        command.Parameters.AddWithValue("?" + kvp.Key + "Green", ((ColorAlpha)kvp.Value).G);
                        command.Parameters.AddWithValue("?" + kvp.Key + "Blue", ((ColorAlpha)kvp.Value).B);
                        command.Parameters.AddWithValue("?" + kvp.Key + "Alpha", ((ColorAlpha)kvp.Value).A);
                    }
                    else if(kvp.Value is bool)
                    {
                        command.Parameters.AddWithValue("?" + kvp.Key, (bool)kvp.Value ? 1 : 0);
                    }
                    else if(kvp.Value is AnArray)
                    {
                        using(MemoryStream stream = new MemoryStream())
                        {
                            LLSD_Binary.Serialize((AnArray)kvp.Value, stream);
                            command.Parameters.AddWithValue("?" + kvp.Key, stream.GetBuffer());
                        }
                    }
                    else if(kvp.Value is Date)
                    {
                        command.Parameters.AddWithValue("?" + kvp.Key, ((Date)kvp.Value).AsULong);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("?" + kvp.Key, kvp.Value);
                    }
                }
                command.ExecuteNonQuery();
            }
        }

        public static Date GetDate(MySqlDataReader dbReader, string prefix)
        {
            return Date.UnixTimeToDateTime((ulong)dbReader[prefix]);
        }

        public static Vector3 GetVector(MySqlDataReader dbReader, string prefix)
        {
            return new Vector3(
                (double)dbReader[prefix + "X"],
                (double)dbReader[prefix + "Y"],
                (double)dbReader[prefix + "Z"]);
        }

        public static Quaternion GetQuaternion(MySqlDataReader dbReader, string prefix)
        {
            return new Quaternion(
                (double)dbReader[prefix + "X"],
                (double)dbReader[prefix + "Y"],
                (double)dbReader[prefix + "Z"],
                (double)dbReader[prefix + "W"]);
        }

        public static Color GetColor(MySqlDataReader dbReader, string prefix)
        {
            return new Color(
                (double)dbReader[prefix + "Red"],
                (double)dbReader[prefix + "Green"],
                (double)dbReader[prefix + "Blue"]);
        }

        public static ColorAlpha GetColorAlpha(MySqlDataReader dbReader, string prefix)
        {
            return new ColorAlpha(
                (double)dbReader[prefix + "Red"],
                (double)dbReader[prefix + "Green"],
                (double)dbReader[prefix + "Blue"],
                (double)dbReader[prefix + "Alpha"]);
        }

        public static bool GetBoolean(MySqlDataReader dbReader, string prefix)
        {
            return (int)dbReader[prefix] != 0;
        }

        public static AnArray GetArray(MySqlDataReader dbReader, string prefix)
        {
            byte[] data = (byte[])dbReader[prefix];
            using(MemoryStream stream = new MemoryStream(data))
            {
                IValue val = LLSD_Binary.Deserialize(stream);
                if(!(val is AnArray))
                {
                    throw new InvalidDataException("Storage data is broken");
                }
                return (AnArray)val;
            }
        }
    }
}
