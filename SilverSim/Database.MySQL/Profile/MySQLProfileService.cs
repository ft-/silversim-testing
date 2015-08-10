// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.Types;

namespace SilverSim.Database.MySQL.Profile
{
    public partial class MySQLProfileService : ProfileServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL PROFILE SERVICE");

        string m_ConnectionString;
        IClassifiedsInterface m_Classifieds;
        IPicksInterface m_Picks;
        INotesInterface m_Notes;
        IUserPreferencesInterface m_UserPreferences;
        IPropertiesInterface m_Properties;

        public MySQLProfileService(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_Classifieds = new MySQLClassifieds(connectionString);
            m_Picks = new MySQLPicks(connectionString);
            m_Notes = new MySQLNotes(connectionString);
            m_UserPreferences = new MySQLUserPreferences(connectionString);
            m_Properties = new MySQLProperties(connectionString);
        }

        public void Startup(ConfigurationLoader loader)
        {
        }

        public void Remove(UUID scopeID, UUID userAccount)
        {
            using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsideTransaction(delegate()
                {
                    using(MySqlCommand cmd = new MySqlCommand("DELETE FROM classifieds where creatoruuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM userpicks where creatoruuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM usernotes where useruuid LIKE ?uuid OR targetuuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM usersettings where useruuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM userprofile where useruuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand("UPDATE userprofile set profilePartner = '00000000-0000-0000-0000-000000000000' where profilePartner LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddWithValue("?uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                });
            }
        }

        public override IClassifiedsInterface Classifieds
        {
            get
            {
                return m_Classifieds;
            }
        }

        public override IPicksInterface Picks
        {
            get
            {
                return m_Picks;
            }
        }

        public override INotesInterface Notes
        {
            get 
            {
                return m_Notes;
            }
        }

        public override IUserPreferencesInterface Preferences
        {
            get 
            {
                return m_UserPreferences;
            }
        }

        public override IPropertiesInterface Properties
        {
            get
            {
                return m_Properties;
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
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "classifieds", m_ClassifiedsMigrations, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "usernotes", m_NotesMigrations, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "userpicks", m_PicksMigrations, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "userprofile", m_ProfileMigrations, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "usersettings", m_UserSettingsMigrations, m_Log);
            
        }

        static readonly string[] m_ClassifiedsMigrations = new string[]
        {
            "CREATE TABLE %tablename% (classifieduuid char(36) not null," +
                                    "creatoruuid char(36) not null," +
                                    "creationdate bigint(20) not null," +
                                    "expirationdate bigint(20) not null," +
                                    "category int(11) not null," +
                                    "`name` varchar(255) not null," +
                                    "description text not null," +
                                    "parceluuid char(36) not null," +
                                    "parentestate int(11) not null," +
                                    "snapshotuuid char(36) not null," +
                                    "simname varchar(255) not null," +
                                    "posglobal varchar(255) not null," +
                                    "parcelname varchar(255) not null," +
                                    "classifiedflags int(11) unsigned not null," +
                                    "priceforlisting int(11) not null," +
                                    "PRIMARY KEY(classifieduuid)," +
                                    "KEY creatoruuid_index (creatoruuid))"
        };

        static readonly string[] m_NotesMigrations = new string[]
        {
            "CREATE TABLE %tablename% (useruuid char(36) not null," +
                                        "targetuuid char(36) not null," +
                                        "notes text not null," +
                                        "primary key (useruuid, targetuuid)," +
                                        "key useruuid (useruuid))"
        };

        static readonly string[] m_PicksMigrations = new string[]
        {
            "CREATE TABLE %tablename% (pickuuid char(36) not null," +
                                    "creatoruuid char(36) not null," +
                                    "toppick tinyint(1) unsigned not null," +
                                    "parceluuid char(36) not null," +
                                    "`name` varchar(255) not null," +
                                    "description text not null," +
                                    "snapshotuuid char(36) not null," +
                                    "parcelname varchar(255) not null," +
                                    "originalname varchar(255) not null," +
                                    "simname varchar(255) not null," +
                                    "posglobal varchar(255) not null," +
                                    "sortorder int(2) not null," +
                                    "enabled tinyint(1) unsigned NOT NULL," +
                                    "PRIMARY KEY (pickuuid)," +
                                    "KEY creatoruuid (creatoruuid))"
        };

        static readonly string[] m_ProfileMigrations = new string[]
        {
            "CREATE TABLE %tablename% (useruuid char(36) not null," +
                                    "profilePartner char(36) not null default '00000000-0000-0000-0000-000000000000'," +
                                    "profileAllowPublish int(1) not null," +
                                    "profileMaturePublish int(1) not null," +
                                    "profileURL varchar(255) not null default ''," +
                                    "profileWantToMask unsigned int(11) not null",
                                    "profileWantToText text," +
                                    "profileSkillsMask unsigned int(11) not null," +
                                    "profileSkillsText text," +
                                    "profileLanguages text," +
                                    "profileImage char(36) not null default '00000000-0000-0000-0000-000000000000'," +
                                    "profileAboutText text," +
                                    "profileFirstImage char(36) not null default '00000000-0000-0000-0000-000000000000'," +
                                    "profileFirstText text," +
                                    "primary key (useruuid))"
        };

        static readonly string[] m_UserSettingsMigrations = new string[]
        {
            "CREATE TABLE %tablename% (useruuid char(36) not null default '00000000-0000-0000-0000-000000000000'," +
                                    "imviaemail tinyint(1) unsigned not null default '0'," +
                                    "visible tinyint(1) unsigned not null default '1'," +
                                    "primary key(useruuid))"
        };
    }

    #region Factory
    [PluginName("Profile")]
    class MySQLProfileServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL PROFILE SERVICE");
        public MySQLProfileServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLProfileService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
