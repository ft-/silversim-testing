/*

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.Main.Common;
using SilverSim.Types;
using Nini.Config;
using log4net;
using MySql.Data.MySqlClient;

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
        public override NameData this[string firstName, string lastName]
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
                            NameData nd = new NameData();
                            nd.ID.ID = (string)dbreader["AvatarID"];
                            nd.ID.HomeURI = new Uri((string)dbreader["HomeURI"]);
                            nd.ID.FirstName = (string)dbreader["FirstName"];
                            nd.ID.LastName = (string)dbreader["LastName"];
                            nd.Authoritative = true;
                            return nd;
                        }
                    }
                }
            }
        }


        public override NameData this[UUID key]
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
                            NameData nd = new NameData();
                            nd.ID.ID = (string)dbreader["AvatarID"];
                            nd.ID.HomeURI = new Uri((string)dbreader["HomeURI"]);
                            nd.ID.FirstName = (string)dbreader["FirstName"];
                            nd.ID.LastName = (string)dbreader["LastName"];
                            nd.Authoritative = true;
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
                            cmd.ExecuteNonQuery();
                        }
                    }

                }
                else if(value.Authoritative) /* do not store non-authoritative entries */
                {
                    using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
                    {
                        connection.Open();

                        using (MySqlCommand cmd = new MySqlCommand("REPLACE INTO groupnames (AvatarID, HomeURI, FirstName, LastName) VALUES (?avatarID, ?homeURI, ?firstName, ?lastName)"))
                        {
                            cmd.Parameters.AddWithValue("?avatarID", value.ID.ID);
                            cmd.Parameters.AddWithValue("?homeURI", value.ID.HomeURI);
                            cmd.Parameters.AddWithValue("?firstName", value.ID.FirstName);
                            cmd.Parameters.AddWithValue("?lastName", value.ID.LastName);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
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
