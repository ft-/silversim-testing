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

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types;
using SilverSim.Types.Parcel;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Database.MySQL.SimulationData
{
    public class MySQLSimulationDataParcelAccessListStorage : ISimulationDataParcelAccessListStorageInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL PARCEL ACCESS LIST STORAGE");
        readonly string m_ConnectionString;
        readonly string m_TableName;

        public MySQLSimulationDataParcelAccessListStorage(string connectionString, string tableName)
        {
            m_ConnectionString = connectionString;
            m_TableName = tableName;
        }

        public bool this[UUID regionID, UUID parcelID, UUI accessor]
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

        public List<ParcelAccessEntry> this[UUID regionID, UUID parcelID]
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

        public void Store(ParcelAccessEntry entry)
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

        public bool RemoveAllFromRegion(UUID regionID)
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

        public bool Remove(UUID regionID, UUID parcelID)
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

        public bool Remove(UUID regionID, UUID parcelID, UUI accessor)
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
