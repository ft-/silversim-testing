// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Database.MySQL.Presence
{
    public class MySQLNpcPresenceService : NpcPresenceServiceInterface, IDBServiceInterface, IPlugin
    {
        readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL PRESENCE SERVICE");

        #region Constructor
        public MySQLNpcPresenceService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
        #endregion

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

        public override void Store(NpcPresenceInfo presenceInfo)
        {
            Dictionary<string, object> post = new Dictionary<string, object>();
            post["NpcID"] = presenceInfo.Npc.ID;
            post["FirstName"] = presenceInfo.Npc.FirstName;
            post["LastName"] = presenceInfo.Npc.LastName;
            post["Owner"] = presenceInfo.Owner;
            post["Group"] = presenceInfo.Group;
            post["Options"] = presenceInfo.Options;
            post["RegionID"] = presenceInfo.RegionID;
            post["Position"] = presenceInfo.Position;
            post["LookAt"] = presenceInfo.LookAt;
            post["SittingOnObjectID"] = presenceInfo.SittingOnObjectID;

            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                try
                {
                    conn.ReplaceInto("npcpresence", post);
                }
                catch
                {
                    throw new PresenceUpdateFailedException();
                }
            }
        }

        public override void Remove(UUID scopeID, UUID npcID)
        {
            throw new NotImplementedException();
        }

        static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("npcpresence"),
            new AddColumn<UUID>("NpcID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("FirstName") { IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("LastName") { IsNullAllowed = false, Default = string.Empty },
            new AddColumn<UUI>("Owner") { IsNullAllowed = false, Default = UUI.Unknown },
            new AddColumn<UGI>("Group") { IsNullAllowed = false, Default = UGI.Unknown },
            new AddColumn<NpcOptions>("Options") { IsNullAllowed = false, Default = NpcOptions.None },
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Vector3>("Position") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Vector3>("LookAt") { IsNullAllowed = false, Default = Vector3.UnitX },
            new AddColumn<UUID>("SittingOnObjectID") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo("NpcID"),
        };

        public override List<NpcPresenceInfo> this[UUID regionID]
        {
            get
            {
                List<NpcPresenceInfo> presences = new List<NpcPresenceInfo>();
                using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM npcpresence WHERE RegionID LIKE ?regionID", conn))
                    {
                        cmd.Parameters.AddParameter("?regionID", regionID);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            NpcPresenceInfo pi = new NpcPresenceInfo();
                            pi.Npc.ID = reader.GetUUID("NpcID");
                            pi.Npc.FirstName = reader.GetString("FirstName");
                            pi.Npc.LastName = reader.GetString("LastName");
                            pi.Owner = reader.GetUUI("Owner");
                            pi.Group = reader.GetUGI("Group");
                            pi.Options = reader.GetEnum<NpcOptions>("Options");
                            pi.RegionID = reader.GetUUID("RegionID");
                            pi.Position = reader.GetVector3("Position");
                            pi.LookAt = reader.GetVector3("LookAt");
                            pi.SittingOnObjectID = reader.GetUUID("SittingOnObjectID");
                            presences.Add(pi);
                        }
                    }
                }
                return presences;
            }
        }
    }
}