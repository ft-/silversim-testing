﻿// SilverSim is distributed under the terms of the
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
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Scene.Types.SceneEnvironment;

namespace SilverSim.Database.MySQL
{
    public static class MySQLUtilities
    {
        #region Connection String Creator
        public static string BuildConnectionString(IConfig config, ILog log)
        {
            if (!(config.Contains("Server") && config.Contains("Username") && config.Contains("Password") && config.Contains("Database")))
            {
                string configName = config.Name;
                if (!config.Contains("Server"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Server' missing in [{0}]", configName);
                }
                if (!config.Contains("Username"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Username' missing in [{0}]", configName);
                }
                if (!config.Contains("Password"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Password' missing in [{0}]", configName);
                }
                if (!config.Contains("Database"))
                {
                    log.FatalFormat("[MYSQL CONFIG]: Parameter 'Database' missing in [{0}]", configName);
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

        [Serializable]
        public class MySQLInsertException : Exception
        {
            public MySQLInsertException()
            {

            }

            public MySQLInsertException(string msg)
                : base(msg)
            {

            }

            protected MySQLInsertException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public MySQLInsertException(string msg, Exception innerException)
                : base(msg, innerException)
            {

            }
        }

        [Serializable]
        public class MySQLMigrationException : Exception
        {
            public MySQLMigrationException()
            {

            }

            public MySQLMigrationException(string msg)
                : base(msg)
            {

            }

            protected MySQLMigrationException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public MySQLMigrationException(string msg, Exception innerException)
                : base(msg, innerException)
            {

            }
        }

        [Serializable]
        public class MySQLTransactionException : Exception
        {
            public MySQLTransactionException()
            {

            }

            public MySQLTransactionException(string msg)
                : base(msg)
            {

            }

            protected MySQLTransactionException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public MySQLTransactionException(string msg, Exception inner)
                : base(msg, inner)
            {

            }
        }

        #region Transaction Helper
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public static void InsideTransaction(this MySqlConnection connection, Action del)
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
                object value = kvp.Value;
                string key = kvp.Key;
                Type t = value.GetType();
                if(t == typeof(Vector3))
                {
                    Vector3 v = (Vector3)value;
                    mysqlparam.AddWithValue("?v_" + key + "X", v.X);
                    mysqlparam.AddWithValue("?v_" + key + "Y", v.Y);
                    mysqlparam.AddWithValue("?v_" + key + "Z", v.Z);
                }
                else if (t == typeof(GridVector))
                {
                    GridVector v = (GridVector)value;
                    mysqlparam.AddWithValue("?v_" + key + "X", v.X);
                    mysqlparam.AddWithValue("?v_" + key + "Y", v.Y);
                }
                else if (t == typeof(Quaternion))
                {
                    Quaternion v = (Quaternion)value;
                    mysqlparam.AddWithValue("?v_" + key + "X", v.X);
                    mysqlparam.AddWithValue("?v_" + key + "Y", v.Y);
                    mysqlparam.AddWithValue("?v_" + key + "Z", v.Z);
                    mysqlparam.AddWithValue("?v_" + key + "W", v.W);
                }
                else if (t == typeof(Color))
                {
                    Color v = (Color)value;
                    mysqlparam.AddWithValue("?v_" + key + "Red", v.R);
                    mysqlparam.AddWithValue("?v_" + key + "Green", v.G);
                    mysqlparam.AddWithValue("?v_" + key + "Blue", v.B);
                }
                else if (t == typeof(ColorAlpha))
                {
                    ColorAlpha v = (ColorAlpha)value;
                    mysqlparam.AddWithValue("?v_" + key + "Red", v.R);
                    mysqlparam.AddWithValue("?v_" + key + "Green", v.G);
                    mysqlparam.AddWithValue("?v_" + key + "Blue", v.B);
                    mysqlparam.AddWithValue("?v_" + key + "Alpha", v.A);
                }
                else if (t == typeof(EnvironmentController.WLVector4))
                {
                    EnvironmentController.WLVector4 vec = (EnvironmentController.WLVector4)value;
                    mysqlparam.AddWithValue("?v_" + key + "Red", vec.X);
                    mysqlparam.AddWithValue("?v_" + key + "Green", vec.Y);
                    mysqlparam.AddWithValue("?v_" + key + "Blue", vec.Z);
                    mysqlparam.AddWithValue("?v_" + key + "Value", vec.W);
                }
                else if (t == typeof(bool))
                {
                    mysqlparam.AddWithValue("?v_" + kvp.Key, (bool)kvp.Value ? 1 : 0);
                }
                else if (t == typeof(UUID) || t == typeof(UUI) || t == typeof(UGI))
                {
                    mysqlparam.AddWithValue("?v_" + key, value.ToString());
                }
                else if (t == typeof(AnArray))
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        LlsdBinary.Serialize((AnArray)value, stream);
                        mysqlparam.AddWithValue("?v_" + key, stream.GetBuffer());
                    }
                }
                else if (t == typeof(Date))
                {
                    mysqlparam.AddWithValue("?v_" + key, ((Date)value).AsULong);
                }
                else if (value == null)
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

        #region Common REPLACE INTO/INSERT INTO helper
        public static void AnyInto(this MySqlConnection connection, string cmd, string tablename, Dictionary<string, object> vals)
        {
            string q1 = cmd + " INTO " + tablename + " (";
            string q2 = ") VALUES (";
            bool first = true;
            foreach (KeyValuePair<string, object> kvp in vals)
            {
                object value = kvp.Value;
                if (value != null)
                {
                    if (!first)
                    {
                        q1 += ",";
                        q2 += ",";
                    }
                    first = false;
                }

                Type t = value.GetType();
                string key = kvp.Key;

                if (t == typeof(Vector3))
                {
                    q1 += "`" + key + "X`,";
                    q2 += "?v_" + key + "X,";
                    q1 += "`" + key + "Y`,";
                    q2 += "?v_" + key + "Y,";
                    q1 += "`" + key + "Z`";
                    q2 += "?v_" + key + "Z";
                }
                else if (t == typeof(GridVector))
                {
                    q1 += "`" + key + "X`,";
                    q2 += "?v_" + key + "X,";
                    q1 += "`" + key + "Y`";
                    q2 += "?v_" + key + "Y";
                }
                else if (t == typeof(Quaternion))
                {
                    q1 += "`" + key + "X`,";
                    q2 += "?v_" + key + "X,";
                    q1 += "`" + key + "Y`,";
                    q2 += "?v_" + key + "Y,";
                    q1 += "`" + key + "Z`,";
                    q2 += "?v_" + key + "Z,";
                    q1 += "`" + key + "W`";
                    q2 += "?v_" + key + "W";
                }
                else if (t == typeof(Color))
                {
                    q1 += "`" + key + "Red`,";
                    q2 += "?v_" + key + "Red,";
                    q1 += "`" + key + "Green`,";
                    q2 += "?v_" + key + "Green,";
                    q1 += "`" + key + "Blue`";
                    q2 += "?v_" + key + "Blue";
                }
                else if (t == typeof(ColorAlpha))
                {
                    q1 += "`" + key + "Red`,";
                    q2 += "?v_" + key + "Red,";
                    q1 += "`" + key + "Green`,";
                    q2 += "?v_" + key + "Green,";
                    q1 += "`" + key + "Blue`,";
                    q2 += "?v_" + key + "Blue,";
                    q1 += "`" + key + "Alpha`";
                    q2 += "?v_" + key + "Alpha";
                }
                else if (value == null)
                {
                    /* skip */
                }
                else
                {
                    q1 += "`" + key + "`";
                    q2 += "?v_" + key;
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

        #region REPLACE INSERT INTO helper
        public static void ReplaceInto(this MySqlConnection connection, string tablename, Dictionary<string, object> vals)
        {
            connection.AnyInto("REPLACE", tablename, vals);
        }
        #endregion

        #region INSERT INTO helper
        public static void InsertInto(this MySqlConnection connection, string tablename, Dictionary<string, object> vals)
        {
            connection.AnyInto("INSERT", tablename, vals);
        }
        #endregion

        #region UPDATE SET helper
        public static void UpdateSet(this MySqlConnection connection, string tablename, Dictionary<string, object> vals, string where)
        {
            string q1 = "UPDATE " + tablename + " SET ";
            bool first = true;

            foreach (KeyValuePair<string, object> kvp in vals)
            {
                object value = kvp.Value;
                Type t = value.GetType();
                string key = kvp.Key;

                if (kvp.Value != null)
                {
                    if (!first)
                    {
                        q1 += ",";
                    }
                    first = false;
                }

                if (t == typeof(Vector3))
                {
                    q1 += "`" + key + "X` = ?v_" + key + "X,";
                    q1 += "`" + key + "Y` = ?v_" + key + "Y,";
                    q1 += "`" + key + "Z` = ?v_" + key + "Z";
                }
                else if (t == typeof(GridVector))
                {
                    q1 += "`" + key + "X` = ?v_" + key + "X,";
                    q1 += "`" + key + "Y` = ?v_" + key + "Y";
                }
                else if (t == typeof(Quaternion))
                {
                    q1 += "`" + key + "X` = ?v_" + key + "X,";
                    q1 += "`" + key + "Y` = ?v_" + key + "Y,";
                    q1 += "`" + key + "Z` = ?v_" + key + "Z,";
                    q1 += "`" + key + "W` = ?v_" + key + "W";
                }
                else if (t == typeof(Color))
                {
                    q1 += "`" + key + "Red` = ?v_" + key + "Red,";
                    q1 += "`" + key + "Green` = ?v_" + key + "Green,";
                    q1 += "`" + key + "Blue` = ?v_" + key + "Blue";
                }
                else if (t == typeof(ColorAlpha))
                {
                    q1 += "`" + key + "Red` = ?v_" + key + "Red,";
                    q1 += "`" + key + "Green` = ?v_" + key + "Green,";
                    q1 += "`" + key + "Blue` = ?v_" + key + "Blue,";
                    q1 += "`" + key + "Alpha` = ?v_" + key + "Alpha";
                }
                else if (value == null)
                {
                    /* skip */
                }
                else
                {
                    q1 += "`" + key + "` = ?v_" + key;
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
                object value = kvp.Value;
                Type t = value.GetType();
                string key = kvp.Key;

                if (value != null)
                {
                    if (!first)
                    {
                        q1 += ",";
                    }
                    first = false;
                }

                if (t == typeof(Vector3))
                {
                    q1 += "`" + key + "X` = ?v_" + key + "X,";
                    q1 += "`" + key + "Y` = ?v_" + key + "Y,";
                    q1 += "`" + key + "Z` = ?v_" + key + "Z";
                }
                else if (t == typeof(GridVector))
                {
                    q1 += "`" + key + "X` = ?v_" + key + "X,";
                    q1 += "`" + key + "Y` = ?v_" + key + "Y";
                }
                else if (t == typeof(Quaternion))
                {
                    q1 += "`" + key + "X` = ?v_" + key + "X,";
                    q1 += "`" + key + "Y` = ?v_" + key + "Y,";
                    q1 += "`" + key + "Z` = ?v_" + key + "Z,";
                    q1 += "`" + key + "W` = ?v_" + key + "W";
                }
                else if (t == typeof(Color))
                {
                    q1 += "`" + key + "Red` = ?v_" + key + "Red,";
                    q1 += "`" + key + "Green` = ?v_" + key + "Green,";
                    q1 += "`" + key + "Blue` = ?v_" + key + "Blue";
                }
                else if (t == typeof(ColorAlpha))
                {
                    q1 += "`" + key + "Red` = ?v_" + key + "Red,";
                    q1 += "`" + key + "Green` = ?v_" + key + "Green,";
                    q1 += "`" + key + "Blue` = ?v_" + key + "Blue,";
                    q1 += "`" + key + "Alpha` = ?v_" + key + "Alpha";
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
        public static EnvironmentController.WLVector4 GetWLVector4(this MySqlDataReader dbReader, string prefix)
        {
            return new EnvironmentController.WLVector4(
                (double)dbReader[prefix + "R"],
                (double)dbReader[prefix + "G"],
                (double)dbReader[prefix + "B"],
                (double)dbReader[prefix + "Y"]);
        }


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
            Type t = v.GetType();
            if(t == typeof(Guid))
            {
                return new UUID((Guid)v);
            }
            
            if(t == typeof(string))
            {
                return new UUID((string)v);
            }

            throw new InvalidCastException("GetUUID could not convert value for " + prefix);
        }

        public static UUI GetUUI(this MySqlDataReader dbReader, string prefix)
        {
            object v = dbReader[prefix];
            Type t = v.GetType();
            if (t == typeof(Guid))
            {
                return new UUI((Guid)v);
            }

            if (t == typeof(string))
            {
                return new UUI((string)v);
            }

            throw new InvalidCastException("GetUUI could not convert value for " + prefix);
        }

        public static UGI GetUGI(this MySqlDataReader dbReader, string prefix)
        {
            object v = dbReader[prefix];
            Type t = v.GetType();
            if (t == typeof(Guid))
            {
                return new UGI((Guid)v);
            }

            if (t == typeof(string))
            {
                return new UGI((string)v);
            }

            throw new InvalidCastException("GetUGI could not convert value for " + prefix);
        }

        public static Date GetDate(this MySqlDataReader dbReader, string prefix)
        {
            ulong v;
            if (!ulong.TryParse(dbReader[prefix].ToString(), out v))
            {
                throw new InvalidCastException("GetDate could not convert value for "+ prefix);
            }
            return Date.UnixTimeToDateTime(v);
        }

        public static Vector3 GetStringFormattedVector(this MySqlDataReader dbReader, string prefix)
        {
            Vector3 v;
            if (!Vector3.TryParse((string)dbReader[prefix], out v))
            {
                throw new InvalidCastException("GetVectorFromString could not convert value for" + prefix);
            }
            return v;
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
            Type t = o.GetType();
            if(t == typeof(uint))
            {
                return (uint)o != 0;
            }
            else if(t == typeof(int))
            {
                return (int)o != 0;
            }
            else if(t == typeof(sbyte))
            {
                return (sbyte)o != 0;
            }
            else if (t == typeof(byte))
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
            object o = dbReader[prefix];
            Type t = o.GetType();
            if(t == typeof(DBNull))
            {
                return new byte[0];
            }
            return (byte[])o;
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
                        uint u;
                        if(!uint.TryParse((string)dbReader["Comment"], out u))
                        {
                            throw new InvalidDataException("Comment is not a parseable number");
                        }
                        return u;
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
