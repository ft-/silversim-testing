// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Types;
using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.IO;
using SilverSim.Types.Asset;

namespace SilverSim.Database.MySQL
{
    public static class MySQLUtilities
    {
        #region Connection String Creator
        public static string BuildConnectionString(IConfig config, ILog log)
        {
            if (!(config.Contains("Server") && config.Contains("Username") && config.Contains("Password") && config.Contains("Database")))
            {
                if (!config.Contains("Server"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Server' missing in [{0}]", config.Name);
                }
                if (!config.Contains("Username"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Username' missing in [{0}]", config.Name);
                }
                if (!config.Contains("Password"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Password' missing in [{0}]", config.Name);
                }
                if (!config.Contains("Database"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Database' missing in [{0}]", config.Name);
                }
                throw new ConfigurationLoader.ConfigurationErrorException();
            }
            return String.Format("Server={0};Uid={1};Pwd={2};Database={3};", 
                config.GetString("Server"),
                config.GetString("Username"),
                config.GetString("Password"),
                config.GetString("Database"));
        }
        #endregion

        public class MySQLInsertException : Exception
        {
            public MySQLInsertException()
            {

            }
        }

        public class MySQLMigrationException : Exception
        {
            public MySQLMigrationException(string msg)
                : base(msg)
            {

            }
        }

        public class MySQLTransactionException : Exception
        {
            public MySQLTransactionException(string msg)
                : base(msg)
            {

            }

            public MySQLTransactionException(string msg, Exception inner)
                : base(msg, inner)
            {

            }
        }

        #region Transaction Helper
        public delegate void TransactionDelegate();
        public static void InsideTransaction(this MySqlConnection connection, TransactionDelegate del)
        {
            using (MySqlCommand cmd = new MySqlCommand("BEGIN", connection))
            {
                cmd.ExecuteNonQuery();
            }
            try
            {
                del();
            }
            catch(Exception e)
            {
                using (MySqlCommand cmd = new MySqlCommand("ROLLBACK", connection))
                {
                    cmd.ExecuteNonQuery();
                }
                throw new MySQLTransactionException("Transaction failed", e);
            }
            using (MySqlCommand cmd = new MySqlCommand("COMMIT", connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
        #endregion

        #region Push parameters
        static void AddParameters(MySqlParameterCollection mysqlparam, Dictionary<string, object> vals)
        {
            foreach (KeyValuePair<string, object> kvp in vals)
            {
                if (kvp.Value is Vector3)
                {
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "X", ((Vector3)kvp.Value).X);
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "Y", ((Vector3)kvp.Value).Y);
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "Z", ((Vector3)kvp.Value).Z);
                }
                else if (kvp.Value is GridVector)
                {
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "X", ((GridVector)kvp.Value).X);
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "Y", ((GridVector)kvp.Value).Y);
                }
                else if (kvp.Value is Quaternion)
                {
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "X", ((Quaternion)kvp.Value).X);
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "Y", ((Quaternion)kvp.Value).Y);
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "Z", ((Quaternion)kvp.Value).Z);
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "W", ((Quaternion)kvp.Value).W);
                }
                else if (kvp.Value is Color)
                {
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "Red", ((Color)kvp.Value).R);
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "Green", ((Color)kvp.Value).G);
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "Blue", ((Color)kvp.Value).B);
                }
                else if (kvp.Value is ColorAlpha)
                {
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "Red", ((ColorAlpha)kvp.Value).R);
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "Green", ((ColorAlpha)kvp.Value).G);
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "Blue", ((ColorAlpha)kvp.Value).B);
                    mysqlparam.AddWithValue("?v_" + kvp.Key + "Alpha", ((ColorAlpha)kvp.Value).A);
                }
                else if (kvp.Value is bool)
                {
                    mysqlparam.AddWithValue("?v_" + kvp.Key, (bool)kvp.Value ? 1 : 0);
                }
                else if (kvp.Value is AnArray)
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        LlsdBinary.Serialize((AnArray)kvp.Value, stream);
                        mysqlparam.AddWithValue("?v_" + kvp.Key, stream.GetBuffer());
                    }
                }
                else if (kvp.Value is Date)
                {
                    mysqlparam.AddWithValue("?v_" + kvp.Key, ((Date)kvp.Value).AsULong);
                }
                else if (kvp.Value == null)
                {
                    /* skip */
                }
                else
                {
                    mysqlparam.AddWithValue("?v_" + kvp.Key, kvp.Value);
                }
            }
        }
        #endregion

        #region REPLACE INSERT INTO helper
        public static void ReplaceInsertInto(this MySqlConnection connection, string tablename, Dictionary<string, object> vals)
        {
            string q1 = "REPLACE INTO " + tablename + " (";
            string q2 = ") VALUES (";
            bool first = true;
            foreach(KeyValuePair<string, object> kvp in vals)
            {
                if (kvp.Value != null)
                {
                    if (!first)
                    {
                        q1 += ",";
                        q2 += ",";
                    }
                    first = false;
                }

                if (kvp.Value is Vector3)
                {
                    q1 += "`" + kvp.Key.ToString() + "X`,";
                    q2 += "?v_" + kvp.Key.ToString() + "X,";
                    q1 += "`" + kvp.Key.ToString() + "Y`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Y,";
                    q1 += "`" + kvp.Key.ToString() + "Z`";
                    q2 += "?v_" + kvp.Key.ToString() + "Z";
                }
                else if(kvp.Value is GridVector)
                {
                    q1 += "`" + kvp.Key.ToString() + "X`,";
                    q2 += "?v_" + kvp.Key.ToString() + "X,";
                    q1 += "`" + kvp.Key.ToString() + "Y`";
                    q2 += "?v_" + kvp.Key.ToString() + "Y";
                }
                else if (kvp.Value is Quaternion)
                {
                    q1 += "`" + kvp.Key.ToString() + "X`,";
                    q2 += "?v_" + kvp.Key.ToString() + "X,";
                    q1 += "`" + kvp.Key.ToString() + "Y`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Y,";
                    q1 += "`" + kvp.Key.ToString() + "Z`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Z,";
                    q1 += "`" + kvp.Key.ToString() + "W`";
                    q2 += "?v_" + kvp.Key.ToString() + "W";
                }
                else if(kvp.Value is Color)
                {
                    q1 += "`" + kvp.Key.ToString() + "Red`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Red,";
                    q1 += "`" + kvp.Key.ToString() + "Green`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Green,";
                    q1 += "`" + kvp.Key.ToString() + "Blue`";
                    q2 += "?v_" + kvp.Key.ToString() + "Blue";
                }
                else if (kvp.Value is ColorAlpha)
                {
                    q1 += "`" + kvp.Key.ToString() + "Red`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Red,";
                    q1 += "`" + kvp.Key.ToString() + "Green`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Green,";
                    q1 += "`" + kvp.Key.ToString() + "Blue`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Blue,";
                    q1 += "`" + kvp.Key.ToString() + "Alpha`";
                    q2 += "?v_" + kvp.Key.ToString() + "Alpha";
                }
                else if(kvp.Value == null)
                {
                    /* skip */
                }
                else
                {
                    q1 += "`" + kvp.Key.ToString() + "`";
                    q2 += "?v_" + kvp.Key.ToString();
                }
            }

            string query = q1 + q2 + ")";
            using(MySqlCommand command = new MySqlCommand(query, connection))
            {
                AddParameters(command.Parameters, vals);
                if(command.ExecuteNonQuery() < 1)
                {
                    throw new MySQLInsertException();
                }
            }
        }
        #endregion

        #region INSERT INTO helper
        public static void InsertInto(this MySqlConnection connection, string tablename, Dictionary<string, object> vals)
        {
            string q1 = "INSERT INTO " + tablename + " (";
            string q2 = ") VALUES (";
            bool first = true;
            foreach (KeyValuePair<string, object> kvp in vals)
            {
                if (kvp.Value != null)
                {
                    if (!first)
                    {
                        q1 += ",";
                        q2 += ",";
                    }
                    first = false;
                }

                if (kvp.Value is Vector3)
                {
                    q1 += "`" + kvp.Key.ToString() + "X`,";
                    q2 += "?v_" + kvp.Key.ToString() + "X,";
                    q1 += "`" + kvp.Key.ToString() + "Y`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Y,";
                    q1 += "`" + kvp.Key.ToString() + "Z`";
                    q2 += "?v_" + kvp.Key.ToString() + "Z";
                }
                else if (kvp.Value is GridVector)
                {
                    q1 += "`" + kvp.Key.ToString() + "X`,";
                    q2 += "?v_" + kvp.Key.ToString() + "X,";
                    q1 += "`" + kvp.Key.ToString() + "Y`";
                    q2 += "?v_" + kvp.Key.ToString() + "Y";
                }
                else if (kvp.Value is Quaternion)
                {
                    q1 += "`" + kvp.Key.ToString() + "X`,";
                    q2 += "?v_" + kvp.Key.ToString() + "X,";
                    q1 += "`" + kvp.Key.ToString() + "Y`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Y,";
                    q1 += "`" + kvp.Key.ToString() + "Z`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Z,";
                    q1 += "`" + kvp.Key.ToString() + "W`";
                    q2 += "?v_" + kvp.Key.ToString() + "W";
                }
                else if (kvp.Value is Color)
                {
                    q1 += "`" + kvp.Key.ToString() + "Red`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Red,";
                    q1 += "`" + kvp.Key.ToString() + "Green`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Green,";
                    q1 += "`" + kvp.Key.ToString() + "Blue`";
                    q2 += "?v_" + kvp.Key.ToString() + "Blue";
                }
                else if (kvp.Value is ColorAlpha)
                {
                    q1 += "`" + kvp.Key.ToString() + "Red`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Red,";
                    q1 += "`" + kvp.Key.ToString() + "Green`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Green,";
                    q1 += "`" + kvp.Key.ToString() + "Blue`,";
                    q2 += "?v_" + kvp.Key.ToString() + "Blue,";
                    q1 += "`" + kvp.Key.ToString() + "Alpha`";
                    q2 += "?v_" + kvp.Key.ToString() + "Alpha";
                }
                else if (kvp.Value == null)
                {
                    /* skip */
                }
                else
                {
                    q1 += "`" + kvp.Key.ToString() + "`";
                    q2 += "?v_" + kvp.Key.ToString();
                }
            }

            string query = q1 + q2 + ")";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                AddParameters(command.Parameters, vals);
                if (command.ExecuteNonQuery() < 1)
                {
                    throw new MySQLInsertException();
                }
            }
        }
        #endregion

        #region UPDATE SET helper
        public static void UpdateSet(this MySqlConnection connection, string tablename, Dictionary<string, object> vals, string where)
        {
            string q1 = "UPDATE " + tablename + " SET ";
            bool first = true;

            foreach (KeyValuePair<string, object> kvp in vals)
            {
                if (kvp.Value != null)
                {
                    if (!first)
                    {
                        q1 += ",";
                    }
                    first = false;
                }

                if (kvp.Value is Vector3)
                {
                    q1 += "`" + kvp.Key.ToString() + "X` = ?v_" + kvp.Key.ToString() + "X,";
                    q1 += "`" + kvp.Key.ToString() + "Y` = ?v_" + kvp.Key.ToString() + "Y,";
                    q1 += "`" + kvp.Key.ToString() + "Z` = ?v_" + kvp.Key.ToString() + "Z";
                }
                else if (kvp.Value is GridVector)
                {
                    q1 += "`" + kvp.Key.ToString() + "X` = ?v_" + kvp.Key.ToString() + "X,";
                    q1 += "`" + kvp.Key.ToString() + "Y` = ?v_" + kvp.Key.ToString() + "Y";
                }
                else if (kvp.Value is Quaternion)
                {
                    q1 += "`" + kvp.Key.ToString() + "X` = ?v_" + kvp.Key.ToString() + "X,";
                    q1 += "`" + kvp.Key.ToString() + "Y` = ?v_" + kvp.Key.ToString() + "Y,";
                    q1 += "`" + kvp.Key.ToString() + "Z` = ?v_" + kvp.Key.ToString() + "Z,";
                    q1 += "`" + kvp.Key.ToString() + "W` = ?v_" + kvp.Key.ToString() + "W";
                }
                else if (kvp.Value is Color)
                {
                    q1 += "`" + kvp.Key.ToString() + "Red` = ?v_" + kvp.Key.ToString() + "Red,";
                    q1 += "`" + kvp.Key.ToString() + "Green` = ?v_" + kvp.Key.ToString() + "Green,";
                    q1 += "`" + kvp.Key.ToString() + "Blue` = ?v_" + kvp.Key.ToString() + "Blue";
                }
                else if (kvp.Value is ColorAlpha)
                {
                    q1 += "`" + kvp.Key.ToString() + "Red` = ?v_" + kvp.Key.ToString() + "Red,";
                    q1 += "`" + kvp.Key.ToString() + "Green` = ?v_" + kvp.Key.ToString() + "Green,";
                    q1 += "`" + kvp.Key.ToString() + "Blue` = ?v_" + kvp.Key.ToString() + "Blue,";
                    q1 += "`" + kvp.Key.ToString() + "Alpha` = ?v_" + kvp.Key.ToString() + "Alpha";
                }
                else if (kvp.Value == null)
                {
                    /* skip */
                }
                else
                {
                    q1 += "`" + kvp.Key.ToString() + "` = ?v_" + kvp.Key.ToString();
                }
            }

            using (MySqlCommand command = new MySqlCommand(q1 + " WHERE " + where, connection))
            {
                AddParameters(command.Parameters, vals);
                if (command.ExecuteNonQuery() < 1)
                {
                    throw new MySQLInsertException();
                }
            }
        }

        public static void UpdateSet(this MySqlConnection connection, string tablename, Dictionary<string, object> vals, Dictionary<string, object> where)
        {
            string q1 = "UPDATE " + tablename + " SET ";
            bool first = true;

            foreach (KeyValuePair<string, object> kvp in vals)
            {
                if (kvp.Value != null)
                {
                    if (!first)
                    {
                        q1 += ",";
                    }
                    first = false;
                }

                if (kvp.Value is Vector3)
                {
                    q1 += "`" + kvp.Key + "X` = ?v_" + kvp.Key + "X,";
                    q1 += "`" + kvp.Key + "Y` = ?v_" + kvp.Key + "Y,";
                    q1 += "`" + kvp.Key + "Z` = ?v_" + kvp.Key + "Z";
                }
                else if (kvp.Value is GridVector)
                {
                    q1 += "`" + kvp.Key + "X` = ?v_" + kvp.Key + "X,";
                    q1 += "`" + kvp.Key + "Y` = ?v_" + kvp.Key + "Y";
                }
                else if (kvp.Value is Quaternion)
                {
                    q1 += "`" + kvp.Key + "X` = ?v_" + kvp.Key + "X,";
                    q1 += "`" + kvp.Key + "Y` = ?v_" + kvp.Key + "Y,";
                    q1 += "`" + kvp.Key + "Z` = ?v_" + kvp.Key + "Z,";
                    q1 += "`" + kvp.Key + "W` = ?v_" + kvp.Key + "W";
                }
                else if (kvp.Value is Color)
                {
                    q1 += "`" + kvp.Key + "Red` = ?v_" + kvp.Key + "Red,";
                    q1 += "`" + kvp.Key + "Green` = ?v_" + kvp.Key + "Green,";
                    q1 += "`" + kvp.Key + "Blue` = ?v_" + kvp.Key + "Blue";
                }
                else if (kvp.Value is ColorAlpha)
                {
                    q1 += "`" + kvp.Key + "Red` = ?v_" + kvp.Key + "Red,";
                    q1 += "`" + kvp.Key + "Green` = ?v_" + kvp.Key + "Green,";
                    q1 += "`" + kvp.Key + "Blue` = ?v_" + kvp.Key + "Blue,";
                    q1 += "`" + kvp.Key + "Alpha` = ?v_" + kvp.Key + "Alpha";
                }
                else if (kvp.Value == null)
                {
                    /* skip */
                }
                else
                {
                    q1 += "`" + kvp.Key + "` = ?v_" + kvp.Key;
                }
            }

            string wherestr = string.Empty;
            foreach(KeyValuePair<string, object> w in where)
            {
                if(wherestr.Length != 0)
                {
                    wherestr += " AND ";
                }
                wherestr += string.Format("{0} LIKE ?w_{0}", w.Key);
            }

            using (MySqlCommand command = new MySqlCommand(q1 + " WHERE " + wherestr, connection))
            {
                AddParameters(command.Parameters, vals);
                foreach(KeyValuePair<string, object> w in where)
                {
                    command.Parameters.AddWithValue("?w_" + w.Key, w.Value);
                }
                if (command.ExecuteNonQuery() < 1)
                {
                    throw new MySQLInsertException();
                }
            }
        }
        #endregion

        #region Data parsers
        public static AssetFlags GetAssetFlags(this MySqlDataReader dbreader, string prefix)
        {
            uint assetFlags;
            if(!uint.TryParse(dbreader[prefix].ToString(), out assetFlags))
            {
                assetFlags = 0;
            }
            return (AssetFlags)assetFlags;
        }

        public static UUID GetUUID(this MySqlDataReader dbReader, string prefix)
        {
            object v = dbReader[prefix];
            string s;
            if(v is Guid)
            {
                return new UUID((Guid)v);
            }
            else if(null != (s = v as string))
            {
                return new UUID(s);
            }
            else
            {
                throw new InvalidCastException("GetUUID could not convert value for " + prefix);
            }
        }

        public static UUI GetUUI(this MySqlDataReader dbReader, string prefix)
        {
            object v = dbReader[prefix];
            string s;
            if (v is Guid)
            {
                return new UUI((Guid)v);
            }
            else if (null != (s = v as string))
            {
                return new UUI(s);
            }
            else
            {
                throw new InvalidCastException("GetUUI could not convert value for " + prefix);
            }
        }

        public static UGI GetUGI(this MySqlDataReader dbReader, string prefix)
        {
            object v = dbReader[prefix];
            string s;
            if (v is Guid)
            {
                return new UGI((Guid)v);
            }
            else if (null != (s = v as string))
            {
                return new UGI(s);
            }
            else
            {
                throw new InvalidCastException("GetUGI could not convert value for " + prefix);
            }
        }

        public static Date GetDate(this MySqlDataReader dbReader, string prefix)
        {
            return Date.UnixTimeToDateTime(ulong.Parse(dbReader[prefix].ToString()));
        }

        public static Vector3 GetVector(this MySqlDataReader dbReader, string prefix)
        {
            return new Vector3(
                (double)dbReader[prefix + "X"],
                (double)dbReader[prefix + "Y"],
                (double)dbReader[prefix + "Z"]);
        }

        public static Quaternion GetQuaternion(this MySqlDataReader dbReader, string prefix)
        {
            return new Quaternion(
                (double)dbReader[prefix + "X"],
                (double)dbReader[prefix + "Y"],
                (double)dbReader[prefix + "Z"],
                (double)dbReader[prefix + "W"]);
        }

        public static Color GetColor(this MySqlDataReader dbReader, string prefix)
        {
            return new Color(
                (double)dbReader[prefix + "Red"],
                (double)dbReader[prefix + "Green"],
                (double)dbReader[prefix + "Blue"]);
        }

        public static ColorAlpha GetColorAlpha(this MySqlDataReader dbReader, string prefix)
        {
            return new ColorAlpha(
                (double)dbReader[prefix + "Red"],
                (double)dbReader[prefix + "Green"],
                (double)dbReader[prefix + "Blue"],
                (double)dbReader[prefix + "Alpha"]);
        }

        public static bool GetBoolean(this MySqlDataReader dbReader, string prefix)
        {
            object o = dbReader[prefix];
            if(o is uint)
            {
                return (uint)o != 0;
            }
            else if(o is int)
            {
                return (int)o != 0;
            }
            else if(o is sbyte)
            {
                return (sbyte)o != 0;
            }
            else if (o is byte)
            {
                return (byte)o != 0;
            }
            else
            {
                throw new InvalidCastException("GetBoolean could not convert value for " + prefix + ": got type " + o.GetType().FullName);
            }
        }

        public static byte[] GetBytes(this MySqlDataReader dbReader, string prefix)
        {
            if(dbReader[prefix] is DBNull)
            {
                return new byte[0];
            }
            return (byte[])dbReader[prefix];
        }
        #endregion

        #region Migrations helper
        private static uint GetTableRevision(this MySqlConnection connection, string name)
        {
            using(MySqlCommand cmd = new MySqlCommand("SHOW TABLE STATUS WHERE name=?name", connection))
            {
                cmd.Parameters.AddWithValue("?name", name);
                using (MySqlDataReader dbReader = cmd.ExecuteReader())
                {
                    if (dbReader.Read())
                    {
                        return uint.Parse((string)dbReader["Comment"]);
                    }
                }
            }
            return 0;
        }

        public static void ProcessMigrations(string connectionString, string tablename, string[] migrations, ILog log)
        {

            using(MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                uint revision = connection.GetTableRevision(tablename);
                string sqlcmd;
                while(revision < migrations.Length)
                {
                    sqlcmd = migrations[revision];
                    sqlcmd = sqlcmd.Replace("%tablename%", tablename);
                    sqlcmd += String.Format(" COMMENT='{0}'", ++revision);
                    log.InfoFormat("[MYSQL MIGRATION]: Updating {0} to revision {1}", tablename, revision);
                    using(MySqlCommand cmd = new MySqlCommand(sqlcmd, connection))
                    {
                        cmd.CommandTimeout = 3600;
                        if(cmd.ExecuteNonQuery() < 0)
                        {
                            throw new MySQLMigrationException(string.Format("Failed to update {0} to revision {1}", tablename, revision));
                        }
                    }
                }
            }
        }
        #endregion
    }
}
