// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Types;
using SilverSim.Types.Asset;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService
    {
        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        static readonly IMigrationElement[] Migrations = new IMigrationElement[]
        {
            new SqlTable("groups"),
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false },
            new AddColumn<string>("Name") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("Location") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("Charter") { IsLong = true },
            new AddColumn<UUID>("InsigniaID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUI>("Founder") { IsNullAllowed = false },
            new AddColumn<int>("MembershipFee") { IsNullAllowed = false, Default = 0 },
            new AddColumn<bool>("OpenEnrollment") {IsNullAllowed = false, Default = false },
            new AddColumn<bool>("ShowInList") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("AllowPublish") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("MaturePublish") { IsNullAllowed = false, Default = false },
            new AddColumn<UUID>("OwnerRoleID") { IsNullAllowed = false },
            new PrimaryKeyInfo("GroupID"),
            new NamedKeyInfo("Name", "Name") {IsUnique = true },

            new SqlTable("groupinvites"),
            new AddColumn<UUID>("InviteID") { IsNullAllowed = false },
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false },
            new AddColumn<UUID>("RoleID") { IsNullAllowed = false },
            new AddColumn<UUI>("Principal") { IsNullAllowed = false },
            new AddColumn<Date>("Timestamp") { IsNullAllowed = false },
            new PrimaryKeyInfo("InviteID"),
            new NamedKeyInfo("PrincipalGroup", "GroupID", "Principal") { IsUnique = true },

            new SqlTable("groupmemberships"),
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false },
            new AddColumn<UUI>("Principal") { IsNullAllowed = false },
            new AddColumn<UUID>("SelectedRoleID") { IsNullAllowed = false },
            new AddColumn<int>("Contribution") { IsNullAllowed = false, Default = 0 },
            new AddColumn<bool>("ListInProfile") { IsNullAllowed = false, Default = true },
            new AddColumn<bool>("AcceptNotices") { IsNullAllowed = false, Default = true },
            new AddColumn<string>("AccessToken") { Cardinality = 36, IsFixed = true },
            new PrimaryKeyInfo("GroupID", "Principal"),
            new NamedKeyInfo("Principal", "Principal"),
            new NamedKeyInfo("GroupID", "GroupID"),

            new SqlTable("groupnotices"),
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false },
            new AddColumn<UUID>("NoticeID") { IsNullAllowed = false },
            new AddColumn<Date>("Timestamp") { IsNullAllowed = false },
            new AddColumn<string>("FromName") { Cardinality = 255 },
            new AddColumn<string>("Subject") { Cardinality = 255 },
            new AddColumn<string>("Message") { IsNullAllowed = false },
            new AddColumn<bool>("HasAttachment") { IsNullAllowed = false, Default = false },
            new AddColumn<AssetType>("AttachmentType") { IsNullAllowed = false , Default = AssetType.Unknown },
            new AddColumn<string>("AssetName") { IsNullAllowed = false, Default = string.Empty, Cardinality = 128 },
            new AddColumn<UUID>("AttachmentItemID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("AttachmentOwnerID") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo("NoticeID"),
            new NamedKeyInfo("GroupID", "GroupID"),
            new NamedKeyInfo("Timestamp", "Timestamp"),

            new SqlTable("activegroup"),
            new AddColumn<UUI>("Principal") { IsNullAllowed = false },
            new AddColumn<UUID>("ActiveGroupID") { IsNullAllowed = false },
            new PrimaryKeyInfo("Principal"),

            new SqlTable("grouprolememberships"),
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false },
            new AddColumn<UUID>("RoleID") { IsNullAllowed = false },
            new AddColumn<UUI>("Principal") { IsNullAllowed = false },
            new PrimaryKeyInfo("GroupID", "RoleID", "Principal"),
            new NamedKeyInfo("Principal", "Principal"),
            new NamedKeyInfo("RoleID", "RoleID"),

            new SqlTable("grouproles"),
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false },
            new AddColumn<UUID>("RoleID") { IsNullAllowed = false },
            new AddColumn<string>("Name") { Cardinality = 255, IsNullAllowed = false },
            new AddColumn<string>("Description") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("Title") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<ulong>("Powers") { IsNullAllowed = false, Default = (ulong)0 }
        };

        public void ProcessMigrations()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.MigrateTables(Migrations, m_Log);
            }
        }
    }
}
