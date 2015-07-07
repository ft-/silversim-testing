﻿/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.AvatarName
{
    #region Service Implementation
    class MySQLAvatarNameService : AvatarNameServiceInterface, IDBServiceInterface, IPlugin
    {
        string m_ConnectionString;
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
        public override UUI this[string firstName, string lastName]
        {
            get
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
                                throw new KeyNotFoundException();
                            }
                            UUI nd = new UUI();
                            nd.ID = dbreader.GetUUID("AvatarID");
                            nd.HomeURI = new Uri((string)dbreader["HomeURI"]);
                            nd.FirstName = (string)dbreader["FirstName"];
                            nd.LastName = (string)dbreader["LastName"];
                            nd.IsAuthoritative = true;
                            return nd;
                        }
                    }
                }
            }
        }


        public override UUI this[UUID key]
        {
            get
            {
                using(MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                {
                    connection.Open();

                    using(MySqlCommand cmd = new MySqlCommand("SELECT * FROM avatarnames WHERE AvatarID LIKE ?avatarid", connection))
                    {
                        cmd.Parameters.AddWithValue("?avatarid", key);
                        using(MySqlDataReader dbreader = cmd.ExecuteReader())
                        {
                            if(!dbreader.Read())
                            {
                                throw new KeyNotFoundException();
                            }
                            UUI nd = new UUI();
                            nd.ID = dbreader.GetUUID("AvatarID");
                            nd.HomeURI = new Uri((string)dbreader["HomeURI"]);
                            nd.FirstName = (string)dbreader["FirstName"];
                            nd.LastName = (string)dbreader["LastName"];
                            nd.IsAuthoritative = true;
                            return nd;
                        }
                    }
                }
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
                            cmd.Parameters.AddWithValue("?id", key);
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
                    data["AvatarID"] = value.ID;
                    data["HomeURI"] = value.HomeURI;
                    data["FirstName"] = value.FirstName;
                    data["LastName"] = value.LastName;
                    using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                    {
                        connection.Open();

                        connection.ReplaceInsertInto("avatarnames", data);
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
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "avatarnames", Migrations, m_Log);
        }

        private static readonly string[] Migrations = new string[]{
            "CREATE TABLE %tablename% (" +
                "AvatarID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "HomeURI VARCHAR(255)," +
                "FirstName VARCHAR(255)," +
                "LastName VARCHAR(255)," +
                "PRIMARY KEY(AvatarID, HomeURI))"
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
