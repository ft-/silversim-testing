// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Database.MySQL.Inventory
{
    #region Service Implementation
    [Description("MySQL Inventory Backend")]
    public sealed partial class MySQLInventoryService : InventoryServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        readonly string m_ConnectionString;
        static readonly ILog m_Log = LogManager.GetLogger("MYSQL INVENTORY SERVICE");
        readonly DefaultInventoryFolderContentService m_ContentService;

        public MySQLInventoryService(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_ContentService = new DefaultInventoryFolderContentService(this);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override IInventoryFolderServiceInterface Folder
        {
            get
            {
                return this;
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override IInventoryItemServiceInterface Item
        {
            get 
            {
                return this;
            }
        }

        IInventoryFolderContentServiceInterface IInventoryFolderServiceInterface.Content
        {
            get
            {
                return m_ContentService;
            }
        }

        public override List<InventoryItem> GetActiveGestures(UUID principalID)
        {
            List<InventoryItem> items = new List<InventoryItem>();
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryitems WHERE OwnerID LIKE ?ownerid AND AssetType = ?assettype AND (flags & 1) <>0", connection))
                {
                    cmd.Parameters.AddParameter("?ownerid", principalID);
                    cmd.Parameters.AddParameter("?assettype", AssetType.Gesture);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            items.Add(dbReader.ToItem());
                        }
                    }
                }
            }

            return items;
        }

        public override List<InventoryFolder> GetInventorySkeleton(UUID principalID)
        {
            List<InventoryFolder> folders = new List<InventoryFolder>();
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryfolders WHERE OwnerID LIKE ?ownerid", connection))
                {
                    cmd.Parameters.AddParameter("?ownerid", principalID);
                    using (MySqlDataReader dbReader = cmd.ExecuteReader())
                    {
                        while (dbReader.Read())
                        {
                            folders.Add(dbReader.ToFolder());
                        }
                    }
                }
            }

            return folders;
        }

        public void VerifyConnection()
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
            }
        }

        #region Table migrations
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
            new SqlTable("inventoryfolders"),
            new AddColumn<UUID>("ID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("ParentFolderID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("OwnerID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("Name") { Cardinality = 64, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<InventoryType>("InventoryType") { IsNullAllowed = false, Default = InventoryType.Unknown },
            new AddColumn<int>("Version") { IsNullAllowed = false, Default = 0 },
            new PrimaryKeyInfo("ID"),
            new NamedKeyInfo("inventoryfolders_owner_index", "OwnerID"),
            new NamedKeyInfo("inventoryfolders_owner_folderid", "OwnerID", "ParentFolderID"),
            new NamedKeyInfo("inventoryfolders_owner_type", "OwnerID", "InventoryType"),

            new SqlTable("inventoryitems"),
            new AddColumn<UUID>("ID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("ParentFolderID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("Name") { Cardinality = 64, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("Description") { Cardinality = 128, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<InventoryType>("InventoryType") { IsNullAllowed = false, Default = InventoryType.Unknown },
            new AddColumn<InventoryFlags>("Flags") { IsNullAllowed = false, Default = InventoryFlags.None },
            new AddColumn<UUID>("OwnerID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("LastOwnerID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("CreatorID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Date>("CreationDate") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new AddColumn<InventoryPermissionsMask>("BasePermissionsMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("CurrentPermissionsMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("EveryOnePermissionsMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("NextOwnerPermissionsMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("GroupPermissionsMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<int>("SalePrice") { IsNullAllowed = false, Default = 10 },
            new AddColumn<InventoryItem.SaleInfoData.SaleType>("SaleType") { IsNullAllowed = false, Default = InventoryItem.SaleInfoData.SaleType.NoSale },
            new AddColumn<InventoryPermissionsMask>("SalePermissionsMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<UUID>("GroupID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("IsGroupOwned") { IsNullAllowed = false, Default = false },
            new AddColumn<UUID>("AssetID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<AssetType>("AssetType") { IsNullAllowed = false, Default = AssetType.Unknown },
            new PrimaryKeyInfo("ID"),
            new NamedKeyInfo("inventoryitems_OwnerID", "OwnerID"),
            new NamedKeyInfo("inventoryitems_OwnerID_ID", "OwnerID", "ID"),
            new NamedKeyInfo("inventoryitems_OwnerID_ParentFolderID", "OwnerID", "ParentFolderID"),
            new TableRevision(2),
            /* necessary boolean correction */
            new ChangeColumn<bool>("IsGroupOwned") { IsNullAllowed = false, Default = false },
        };

        #endregion

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        public void Remove(UUID scopeID, UUID userAccount)
        {
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                connection.InsideTransaction(delegate()
                {
                    using(MySqlCommand cmd = new MySqlCommand("DELETE FROM inventoryitems WHERE OwnerID LIKE ?ownerid", connection))
                    {
                        cmd.Parameters.AddParameter("?ownerid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM inventoryfolders WHERE OwnerID LIKE ?ownerid", connection))
                    {
                        cmd.Parameters.AddParameter("?ownerid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                });
            }
        }
    }
    #endregion

    #region Factory
    [PluginName("Inventory")]
    public class MySQLInventoryServiceFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("MYSQL INVENTORY SERVICE");
        public MySQLInventoryServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MySQLInventoryService(MySQLUtilities.BuildConnectionString(ownSection, m_Log));
        }
    }
    #endregion
}
