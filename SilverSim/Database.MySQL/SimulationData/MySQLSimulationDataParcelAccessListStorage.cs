// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataParcelAccessListStorage : SimulationDataParcelAccessListStorageInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL PARCEL ACCESS LIST STORAGE");
        readonly string m_ConnectionString;
        readonly string m_TableName;

        public MySQLSimulationDataParcelAccessListStorage(string connectionString, string tableName)
        {
            m_ConnectionString = connectionString;
            m_TableName = tableName;
        }

        public override bool this[UUID regionID, UUID parcelID, UUI accessor]
        {
            get
            {
                List<ParcelAccessEntry> result = new List<ParcelAccessEntry>();

                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM " + m_TableName + " WHERE ExpiresAt <= " + Date.GetUnixTime().ToString() + " AND ExpiresAt <> 0", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    /* we use a specific implementation to reduce the result set here */
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM " + m_TableName + " WHERE RegionID LIKE '" + regionID.ToString() + "' AND ParcelID LIKE '" + parcelID.ToString() + "' AND Accessor LIKE \"" + accessor.ID.ToString() + "\"%", connection))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            ParcelAccessEntry entry = new ParcelAccessEntry();
                            entry.ParcelID = reader.GetUUID("ParcelID");
                            entry.Accessor = reader.GetUUI("Accessor");
                            ulong val = reader.GetUInt64("ExpiresAt");
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

        public override List<ParcelAccessEntry> this[UUID regionID, UUID parcelID]
        {
            get
            {
                List<ParcelAccessEntry> result = new List<ParcelAccessEntry>();
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM " + m_TableName + " WHERE ExpiresAt <= " + Date.GetUnixTime().ToString() + " AND ExpiresAt > 0", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM " + m_TableName + " WHERE RegionID LIKE '" + regionID.ToString() + "' AND ParcelID LIKE '" + parcelID.ToString() + "'", connection))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ParcelAccessEntry entry = new ParcelAccessEntry();
                                entry.RegionID = reader.GetUUID("RegionID");
                                entry.ParcelID = reader.GetUUID("ParcelID");
                                entry.Accessor = reader.GetUUI("Accessor");
                                ulong val = reader.GetUInt64("ExpiresAt");
                                if (val != 0)
                                {
                                    entry.ExpiresAt = Date.UnixTimeToDateTime(val);
                                }
                                result.Add(entry);
                            }
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
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM " + m_TableName + " WHERE ExpiresAt <= " + Date.GetUnixTime().ToString() + " AND ExpiresAt > 0", connection))
                {
                    cmd.ExecuteNonQuery();
                }

                Dictionary<string, object> data = new Dictionary<string, object>();
                data["RegionID"] = entry.RegionID;
                data["ParcelID"] = entry.ParcelID;
                data["Accessor"] = entry.Accessor;
                data["ExpiresAt"] = entry.ExpiresAt != null ? entry.ExpiresAt.AsULong : (ulong)0;
                connection.ReplaceInto(m_TableName, data);
            }
        }

        public override bool RemoveAllFromRegion(UUID regionID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM " + m_TableName + " WHERE RegionID LIKE '" + regionID.ToString() + "'", connection))
                {
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public override bool Remove(UUID regionID, UUID parcelID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM " + m_TableName + " WHERE RegionID LIKE '" + regionID.ToString() + "' AND ParcelID LIKE '" + parcelID.ToString() + "'", connection))
                {
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public override bool Remove(UUID regionID, UUID parcelID, UUI accessor)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM " + m_TableName + " WHERE RegionID LIKE '" + regionID.ToString() + "' AND ParcelID LIKE '" + parcelID.ToString() + "' AND Accessor LIKE \"" + accessor.ID.ToString() + "%\"", connection))
                {
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
