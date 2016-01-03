// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using log4net;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataParcelAccessListStorage : SimulationDataParcelAccessListStorageInterface, IDBServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL PARCEL ACCESS LIST STORAGE");
        readonly string m_ConnectionString;
        readonly string m_TableName;

        public MySQLSimulationDataParcelAccessListStorage(string connectionString, string tableName)
        {
            m_ConnectionString = connectionString;
            m_TableName = tableName;
        }

        public override bool this[UUID parcelID, UUI accessor]
        {
            get
            {
                List<ParcelAccessEntry> result = new List<ParcelAccessEntry>();

                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM ?table WHERE ExpiresAt <= ?unixtime AND ExpiresAt <> 0", connection))
                    {
                        cmd.Parameters.AddWithValue("?table", m_TableName);
                        cmd.Parameters.AddWithValue("?unixtime", Date.GetUnixTime());
                        cmd.ExecuteNonQuery();
                    }

                    /* we use a specific implementation to reduce the result set here */
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM ?table WHERE ParcelID LIKE ?parcelid AND Accessor LIKE \"" +accessor.ID.ToString() + "\"%", connection))
                    {
                        cmd.Parameters.AddWithValue("?table", m_TableName);
                        cmd.Parameters.AddWithValue("?parcelid", parcelID.ToString());
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            ParcelAccessEntry entry = new ParcelAccessEntry();
                            entry.ParcelID = reader.GetUUID("ParcelID");
                            entry.Accessor = reader.GetUUI("Accessor");
                            ulong val = (ulong)reader["ExpiresAt"];
                            if (val != 0)
                            {
                                entry.ExpiresAt = Date.UnixTimeToDateTime(val);
                            }
                            result.Add(entry);
                        }
                    }
                }

                /* the prefiltered set reduces the amount of checks we have to do here */
                IEnumerable<ParcelAccessEntry> en = from entry in result where entry.Accessor.EqualsGrid(accessor) select entry;
                return en.GetEnumerator().MoveNext();
            }
        }

        public override List<ParcelAccessEntry> this[UUID parcelID]
        {
            get
            {
                List<ParcelAccessEntry> result = new List<ParcelAccessEntry>();
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM ?table WHERE ExpiresAt <= ?unixtime AND ExpiresAt <> 0", connection))
                    {
                        cmd.Parameters.AddWithValue("?table", m_TableName);
                        cmd.Parameters.AddWithValue("?unixtime", Date.GetUnixTime());
                        cmd.ExecuteNonQuery();
                    }

                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM ?table WHERE ParcelID LIKE ?parcelid", connection))
                    {
                        cmd.Parameters.AddWithValue("?table", m_TableName);
                        cmd.Parameters.AddWithValue("?parcelid", parcelID.ToString());
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            ParcelAccessEntry entry = new ParcelAccessEntry();
                            entry.ParcelID = reader.GetUUID("ParcelID");
                            entry.Accessor = reader.GetUUI("Accessor");
                            ulong val = (ulong)reader["ExpiresAt"];
                            if (val != 0)
                            {
                                entry.ExpiresAt = Date.UnixTimeToDateTime(val);
                            }
                            result.Add(entry);
                        }
                    }
                }
                return result;
            }
        }

        public override void Store(ParcelAccessEntry entry)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM ?table WHERE ExpiresAt <= ?unixtime AND ExpiresAt <> 0", connection))
                {
                    cmd.Parameters.AddWithValue("?table", m_TableName);
                    cmd.Parameters.AddWithValue("?unixtime", Date.GetUnixTime());
                    cmd.ExecuteNonQuery();
                }

                Dictionary<string, object> data = new Dictionary<string, object>();
                data["ParcelID"] = entry.ParcelID;
                data["Accessor"] = entry.Accessor;
                if(entry.ExpiresAt != null)
                {
                    data["ExpiresAt"] = entry.ExpiresAt.AsULong;
                }
                connection.ReplaceInto(m_TableName, data);
            }
        }

        public override bool RemoveAll(UUID parcelID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM ?table WHERE RegionID LIKE ?regionid", connection))
                {
                    cmd.Parameters.AddWithValue("?table", m_TableName);
                    cmd.Parameters.AddWithValue("?regionid", parcelID.ToString());
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public override bool Remove(UUID parcelID, UUI accessor)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM ?table WHERE RegionID LIKE ?regionid AND Accessor LIKE ?accessor", connection))
                {
                    cmd.Parameters.AddWithValue("?table", m_TableName);
                    cmd.Parameters.AddWithValue("?regionid", parcelID.ToString());
                    cmd.Parameters.AddWithValue("?accessor", accessor.ToString());
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public void VerifyConnection()
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
            }
        }

        public void ProcessMigrations()
        {
            MySQLUtilities.ProcessMigrations(m_ConnectionString, m_TableName, Migrations, m_Log);
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "ParcelID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "Accessor VARCHAR(255) NOT NULL," +
                "ExpiresAt BIGINT(20) NOT NULL)"
        };
    }
}
