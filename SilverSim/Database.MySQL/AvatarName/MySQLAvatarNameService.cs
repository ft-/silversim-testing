// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.AvatarName
{
    #region Service Implementation
    [Description("MySQL AvatarName Backend")]
    public sealed class MySQLAvatarNameService : AvatarNameServiceInterface, IDBServiceInterface, IPlugin
    {
        readonly string m_ConnectionString;
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL AVATAR NAMES SERVICE");

        #region Constructor
        public MySQLAvatarNameService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
        #endregion

        #region Accessors
        public override bool TryGetValue(string firstName, string lastName, out UUI uui)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();

                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM avatarnames WHERE FirstName LIKE ?firstName AND LastName LIKE ?lastName", connection))
                {
                    cmd.Parameters.AddWithValue("?firstName", firstName);
                    cmd.Parameters.AddWithValue("?lastName", lastName);
                    using (MySqlDataReader dbreader = cmd.ExecuteReader())
                    {
                        if (!dbreader.Read())
                        {
                            uui = default(UUI);
                            return false;
                        }
                        uui = new UUI();
                        uui.ID = dbreader.GetUUID("AvatarID");
                        uui.HomeURI = new Uri((string)dbreader["HomeURI"]);
                        uui.FirstName = (string)dbreader["FirstName"];
                        uui.LastName = (string)dbreader["LastName"];
                        uui.IsAuthoritative = true;
                        return true;
                    }
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override UUI this[string firstName, string lastName]
        {
            get
            {
                UUI uui;
                if(!TryGetValue(firstName, lastName, out uui))
                {
                    throw new KeyNotFoundException();
                }
                return uui;
            }
        }

        public override bool TryGetValue(UUID key, out UUI uui)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();

                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM avatarnames WHERE AvatarID LIKE ?avatarid", connection))
                {
                    cmd.Parameters.AddWithValue("?avatarid", key.ToString());
                    using (MySqlDataReader dbreader = cmd.ExecuteReader())
                    {
                        if (!dbreader.Read())
                        {
                            uui = default(UUI);
                            return false;
                        }
                        uui = new UUI();
                        uui.ID = dbreader.GetUUID("AvatarID");
                        uui.HomeURI = new Uri((string)dbreader["HomeURI"]);
                        uui.FirstName = (string)dbreader["FirstName"];
                        uui.LastName = (string)dbreader["LastName"];
                        uui.IsAuthoritative = true;
                        return true;
                    }
                }
            }
        }

        public override UUI this[UUID key]
        {
            get
            {
                UUI uui;
                if(!TryGetValue(key, out uui))
                {
                    throw new KeyNotFoundException();
                }
                return uui;
            }
            set
            {
                if(value == null)
                {
                    using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                    {
                        connection.Open();

                        using (MySqlCommand cmd = new MySqlCommand("DELETE FROM avatarnames WHERE AvatarID LIKE ?id", connection))
                        {
                            cmd.Parameters.AddWithValue("?id", key.ToString());
                            if(cmd.ExecuteNonQuery() < 1)
                            {
                                throw new KeyNotFoundException();
                            }
                        }
                    }

                }
                else if(value.IsAuthoritative) /* do not store non-authoritative entries */
                {
                    Dictionary<string, object> data = new Dictionary<string, object>();
                    data["AvatarID"] = value.ID.ToString();
                    data["HomeURI"] = value.HomeURI.ToString();
                    data["FirstName"] = value.FirstName;
                    data["LastName"] = value.LastName;
                    using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                    {
                        connection.Open();

                        connection.ReplaceInto("avatarnames", data);
                    }
                }
            }
        }
        #endregion

        public override List<UUI> Search(string[] names)
        {
            if(names.Length < 1 || names.Length > 2)
            {
                return new List<UUI>();
            }

            if(names.Length == 1)
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM avatarnames WHERE FirstName LIKE ?name OR LastName LIKE ?name", connection))
                    {
                        cmd.Parameters.AddWithValue("?name", "%" + names[0] + "%");

                        return GetSearchResults(cmd);
                    }
                }
            }
            else
            {
                using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM avatarnames WHERE FirstName LIKE ?firstname AND LastName LIKE ?lastname", connection))
                    {
                        cmd.Parameters.AddWithValue("?firstname", "%" + names[0] + "%");
                        cmd.Parameters.AddWithValue("?lastname", "%" + names[1] + "%");

                        return GetSearchResults(cmd);
                    }
                }
            }
        }

        List<UUI> GetSearchResults(MySqlCommand cmd)
        {
            List<UUI> results = new List<UUI>();
            using(MySqlDataReader dbreader = cmd.ExecuteReader())
            {
                while(dbreader.Read())
                {
                    UUI nd = new UUI();
                    nd.ID = dbreader.GetUUID("AvatarID");
                    nd.HomeURI = new Uri((string)dbreader["HomeURI"]);
                    nd.FirstName = (string)dbreader["FirstName"];
                    nd.LastName = (string)dbreader["LastName"];
                    nd.IsAuthoritative = true;
                    results.Add(nd);
                }
                return results;
            }

        }

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        public void ProcessMigrations()
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.MigrateTables(Migrations, m_Log);
            }
        }

        private static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("avatarnames"),
            new AddColumn<UUID>("AvatarID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("HomeURI") { Cardinality = 255 },
            new AddColumn<string>("FirstName") { Cardinality = 255 },
            new AddColumn<string>("LastName") { Cardinality = 255 },
            new PrimaryKeyInfo(new string[] {"AvatarID", "HomeURI" })
        };
    }
    #endregion

    #region Factory
    [PluginName("AvatarNames")]
    public class MySQLAvatarNameServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL AVATAR NAMES SERVICE");
        public MySQLAvatarNameServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLAvatarNameService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
