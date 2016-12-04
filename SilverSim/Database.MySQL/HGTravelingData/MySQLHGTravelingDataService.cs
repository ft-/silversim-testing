// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.HGTraveling;
using SilverSim.Types;
using SilverSim.Types.HGTraveling;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.HGTravelingData
{
    #region Service implementation
    static class MySQLHGTravelingDataExtensionMethods
    {
        public static HGTravelingDataInfo ToHGTravelingData(this MySqlDataReader reader)
        {
            HGTravelingDataInfo info = new HGTravelingDataInfo();
            info.SessionID = reader.GetUUID("SessionID");
            info.UserID = reader.GetUUID("UserID");
            info.GridExternalName = reader.GetString("GridExternalName");
            info.ServiceToken = reader.GetString("ServiceToken");
            info.ClientIPAddress = reader.GetString("ClientIPAddress");
            info.Timestamp = reader.GetDate("Timestamp");
            return info;
        }
    }

    public class MySQLHGTravelingDataService : HGTravelingDataServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL HGTRAVELINGDATA SERVICE");
        readonly string m_ConnectionString;

        public MySQLHGTravelingDataService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override HGTravelingDataInfo GetHGTravelingDatabyAgentUUIDAndNotHomeURI(UUID agentID, string homeURI)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM hgtravelingdata WHERE UserID LIKE ?id AND GridExternalName NOT LIKE ?homeuri LIMIT 1", connection))
                {
                    cmd.Parameters.AddParameter("?id", agentID);
                    cmd.Parameters.AddParameter("?homeuri", homeURI);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.ToHGTravelingData();
                        }
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        public override HGTravelingDataInfo GetHGTravelingData(UUID sessionID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM hgtravelingdata WHERE SessionID LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", sessionID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            return reader.ToHGTravelingData();
                        }
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        public override HGTravelingDataInfo GetHGTravelingDataByAgentUUIDAndIPAddress(UUID agentID, string ipAddress)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM hgtravelingdata WHERE UserID LIKE ?id AND ClientIPAddress LIKE ?ip", connection))
                {
                    cmd.Parameters.AddParameter("?id", agentID);
                    cmd.Parameters.AddParameter("?ip", ipAddress);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.ToHGTravelingData();
                        }
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        public override List<HGTravelingDataInfo> GetHGTravelingDatasByAgentUUID(UUID agentID)
        {
            List<HGTravelingDataInfo> infos = new List<HGTravelingDataInfo>();
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM hgtravelingdata WHERE UserID LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", agentID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            infos.Add(reader.ToHGTravelingData());
                        }
                    }
                }
            }
            return infos;
        }

        public override bool Remove(UUID sessionID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM hgtravelingdata WHERE SessionID LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", sessionID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public void Remove(UUID scopeID, UUID accountID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM hgtravelingdata WHERE UserID LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", accountID);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override bool RemoveByAgentUUID(UUID agentID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM hgtravelingdata WHERE UserID LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", agentID);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public override void Store(HGTravelingDataInfo data)
        {
            Dictionary<string, object> insertVals = new Dictionary<string, object>();
            insertVals.Add("SessionID", data.SessionID.ToString());
            insertVals.Add("UserID", data.UserID.ToString());
            insertVals.Add("GridExternalName", data.GridExternalName);
            insertVals.Add("ServiceToken", data.ServiceToken);
            insertVals.Add("ClientIPAddress", data.ClientIPAddress);
            insertVals.Add("Timestamp", Date.Now);
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.ReplaceInto("hgtravelingdata", insertVals);
            }
        }

        static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("hgtravelingdata"),
            new AddColumn<UUID>("SessionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("UserID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("GridExternalName") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("ServiceToken") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("ClientIPAddress") { IsNullAllowed = false },
            new AddColumn<Date>("Timestamp") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new PrimaryKeyInfo(new string[] {"SessionID"}),
            new NamedKeyInfo("UserIDSessionID", new string[] { "UserID", "SessionID" }) { IsUnique = true }
        };


        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public void ProcessMigrations()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.MigrateTables(Migrations, m_Log);
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("HGTravelingData")]
    public class MySQLHGTravelingDataServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL HGTRAVELINGDATA SERVICE");
        public MySQLHGTravelingDataServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLHGTravelingDataService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
