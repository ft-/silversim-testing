// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.Types;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Profile
{
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [Description("MySQL Profile Backend")]
    public sealed partial class MySQLProfileService : ProfileServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL PROFILE SERVICE");

        readonly string m_ConnectionString;

        public MySQLProfileService(string connectionString)
        {
            m_ConnectionString = connectionString;
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public override void Remove(UUID scopeID, UUID userAccount)
        {
            using(MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsideTransaction(delegate()
                {
                    using(MySqlCommand cmd = new MySqlCommand("DELETE FROM classifieds where creatoruuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddParameter("?uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM userpicks where creatoruuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddParameter("?uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM usernotes where useruuid LIKE ?uuid OR targetuuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddParameter("?uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM usersettings where useruuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddParameter("?uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM userprofile where useruuid LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddParameter("?uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand("UPDATE userprofile set profilePartner = '00000000-0000-0000-0000-000000000000' where profilePartner LIKE ?uuid", conn))
                    {
                        cmd.Parameters.AddParameter("?uuid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                });
            }
        }

        public override IClassifiedsInterface Classifieds
        {
            get
            {
                return this;
            }
        }

        public override IPicksInterface Picks
        {
            get
            {
                return this;
            }
        }

        public override INotesInterface Notes
        {
            get 
            {
                return this;
            }
        }

        public override IUserPreferencesInterface Preferences
        {
            get 
            {
                return this;
            }
        }

        public override IPropertiesInterface Properties
        {
            get
            {
                return this;
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
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.MigrateTables(Migrations, m_Log);
            }
        }

        static IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("classifieds"),
            new AddColumn<UUID>("classifieduuid") { IsNullAllowed = false },
            new AddColumn<UUID>("creatoruuid") { IsNullAllowed = false },
            new AddColumn<Date>("creationdate") { IsNullAllowed = false },
            new AddColumn<Date>("expirationdate") { IsNullAllowed = false },
            new AddColumn<int>("Category") { IsNullAllowed = false },
            new AddColumn<string>("name") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("description") { IsNullAllowed = false },
            new AddColumn<UUID>("parceluuid") { IsNullAllowed = false },
            new AddColumn<int>("parentestate") { IsNullAllowed = false },
            new AddColumn<UUID>("snapshotuuid") { IsNullAllowed = false },
            new AddColumn<string>("simname") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("posglobal") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("parcelname") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<uint>("classifiedflags") { IsNullAllowed = false },
            new AddColumn<int>("priceforlisting") { IsNullAllowed = false },
            new PrimaryKeyInfo("classifieduuid"),
            new NamedKeyInfo("creatoruuid_index", new string[] { "creatoruuid" }),
            new TableRevision(2),
            /* some change entry needed for rev 1 tables */
            new ChangeColumn<Date>("creationdate") { IsNullAllowed = false },
            new ChangeColumn<Date>("expirationdate") { IsNullAllowed = false },
            new ChangeColumn<Vector3>("posglobal") { IsNullAllowed = false },

            new SqlTable("usernotes"),
            new AddColumn<UUID>("useruuid") { IsNullAllowed = false },
            new AddColumn<UUID>("targetuuid") { IsNullAllowed = false },
            new AddColumn<string>("notes") { IsNullAllowed = false },
            new PrimaryKeyInfo("useruuid", "targetuuid"),
            new NamedKeyInfo("useruuid", "useruuid"),

            new SqlTable("userpicks"),
            new AddColumn<UUID>("pickuuid") { IsNullAllowed = false },
            new AddColumn<UUID>("creatoruuid") { IsNullAllowed = false },
            new AddColumn<bool>("toppick") { IsNullAllowed = false },
            new AddColumn<UUID>("parceluuid") { IsNullAllowed = false },
            new AddColumn<string>("name") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("description") { IsNullAllowed = false },
            new AddColumn<UUID>("snapshotuuid") { IsNullAllowed = false },
            new AddColumn<string>("parcelname") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("originalname") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("simname") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("posglobal") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<int>("sortorder") { IsNullAllowed = false },
            new AddColumn<bool>("enabled") { IsNullAllowed = false },
            new PrimaryKeyInfo("pickuuid"),
            new NamedKeyInfo("creatoruuid", "creatoruuid"),
            new TableRevision(2),
            new ChangeColumn<Vector3>("posglobal") { IsNullAllowed = false },

            new SqlTable("userprofile"),
            new AddColumn<UUID>("useruuid") { IsNullAllowed = false },
            new AddColumn<UUID>("profilePartner") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("profileAllowPublish") { IsNullAllowed = false },
            new AddColumn<bool>("profileMaturePublish") { IsNullAllowed = false },
            new AddColumn<string>("profileURL") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<uint>("profileWantToMask") { IsNullAllowed = false },
            new AddColumn<string>("profileWantToText"),
            new AddColumn<uint>("profileSkillsMask") { IsNullAllowed = false },
            new AddColumn<string>("profileSkillsText"),
            new AddColumn<string>("profileLanguages"),
            new AddColumn<UUID>("profileImage") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("profileAboutText"),
            new AddColumn<UUID>("profileFirstImage") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("profileFirstText"),
            new PrimaryKeyInfo("useruuid"),
            new TableRevision(2),
            /* needed changes for revision 1 tables */
            new ChangeColumn<bool>("profileAllowPublish") { IsNullAllowed = false },
            new ChangeColumn<bool>("profileMaturePublish") { IsNullAllowed = false },

            new SqlTable("usersettings"),
            new AddColumn<UUID>("useruuid") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("imviaemail") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("visible") { IsNullAllowed = false, Default = true },
            new PrimaryKeyInfo("useruuid")
        };
    }

    #region Factory
    [PluginName("Profile")]
    public sealed class MySQLProfileServiceFactory : IPluginFactory
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
