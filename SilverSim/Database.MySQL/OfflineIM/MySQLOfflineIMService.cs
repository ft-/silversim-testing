// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.IM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SilverSim.Types;
using SilverSim.Types.IM;
using MySql.Data.MySqlClient;
using log4net;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;

namespace SilverSim.Database.MySQL.OfflineIM
{
    #region Service implementation
    public class MySQLOfflineIMService : OfflineIMServiceInterface, IPlugin, IDBServiceInterface, IUserAccountDeleteServiceInterface
    {
        static readonly ILog m_Log = LogManager.GetLogger("MYSQL OFFLINEIM SERVICE");
        readonly string m_ConnectionString;

        public MySQLOfflineIMService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public override void DeleteOfflineIM(ulong offlineImID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand("DELETE FROM offlineim WHERE ID LIKE ?id", connection))
                {
                    command.Parameters.AddParameter("?id", offlineImID);
                    command.ExecuteNonQuery();
                }
            }
        }

        public override List<GridInstantMessage> GetOfflineIMs(UUID principalID)
        {
            List<GridInstantMessage> ims = new List<GridInstantMessage>();
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM offlineim WHERE ToAgentID LIKE ?id", connection))
                {
                    cmd.Parameters.AddParameter("?id", principalID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            GridInstantMessage im = new GridInstantMessage();
                            im.ID = reader.GetUInt64("ID");
                            im.FromAgent = reader.GetUUI("FromAgent");
                            im.FromGroup = reader.GetUGI("FromGroup");
                            im.ToAgent.ID = reader.GetUUID("ToAgentID");
                            im.Dialog = reader.GetEnum<GridInstantMessageDialog>("Dialog");
                            im.IsFromGroup = reader.GetBool("IsFromGroup");
                            im.Message = reader.GetString("Message");
                            im.IMSessionID = reader.GetUUID("IMSessionID");
                            im.Position = reader.GetVector3("Position");
                            im.BinaryBucket = reader.GetBytes("BinaryBucket");
                            im.ParentEstateID = reader.GetUInt32("ParentEstateID");
                            im.RegionID = reader.GetUUID("RegionID");
                            im.Timestamp = reader.GetDate("Timestamp");
                            im.IsOffline = true;
                            ims.Add(im);
                        }
                    }
                }
            }
            return ims;
        }

        public void ProcessMigrations()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.MigrateTables(Migrations, m_Log);
            }
        }

        static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("offlineim"),
            new AddColumn<ulong>("ID") { IsNullAllowed = false },
            new AddColumn<UUI>("FromAgent") { IsNullAllowed = false },
            new AddColumn<UGI>("FromGroup") { IsNullAllowed = false },
            new AddColumn<UUID>("ToAgentID") { IsNullAllowed = false },
            new AddColumn<GridInstantMessageDialog>("Dialog") { IsNullAllowed = false },
            new AddColumn<bool>("IsFromGroup") { IsNullAllowed = false },
            new AddColumn<string>("Message") { IsLong = true },
            new AddColumn<UUID>("IMSessionID") { IsNullAllowed = false },
            new AddColumn<Vector3>("Position") { IsNullAllowed = false },
            new AddColumn<byte[]>("BinaryBucket") { IsNullAllowed = false },
            new AddColumn<uint>("ParentEstateID") { IsNullAllowed = false },
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false },
            new AddColumn<Date>("Timestamp") {IsNullAllowed = false }
        };

        public void Remove(UUID scopeID, UUID accountID)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand("DELETE FROM offlineim WHERE ToAgentID LIKE ?id", connection))
                {
                    command.Parameters.AddParameter("?id", accountID);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override void StoreOfflineIM(GridInstantMessage im)
        {
            Dictionary<string, object> vals = new Dictionary<string, object>();
            vals.Add("ID", im.ID);
            vals.Add("FromAgent", im.FromAgent);
            vals.Add("FromGroup", im.FromGroup);
            vals.Add("ToAgentID", im.ToAgent.ID);
            vals.Add("Dialog", im.Dialog);
            vals.Add("IsFromGroup", im.IsFromGroup);
            vals.Add("Message", im.Message);
            vals.Add("IMSessionID", im.IMSessionID);
            vals.Add("Position", im.Position);
            vals.Add("BinaryBucket", im.BinaryBucket);
            vals.Add("ParentEstateID", im.ParentEstateID);
            vals.Add("RegionID", im.RegionID);
            vals.Add("Timestamp", im.Timestamp);
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsertInto("offlineim", vals);
            }
        }

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("OfflineIM")]
    public class MySQLOfflineIMServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL INVENTORY SERVICE");
        public MySQLOfflineIMServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLOfflineIMService(
                MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
