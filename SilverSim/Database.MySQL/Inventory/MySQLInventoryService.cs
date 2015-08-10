// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using MySql.Data.MySqlClient;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Database;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Inventory
{
    #region Service Implementation
    public class MySQLInventoryService : InventoryServiceInterface, IDBServiceInterface, IPlugin, IUserAccountDeleteServiceInterface
    {
        string m_ConnectionString;
        static readonly ILog m_Log = LogManager.GetLogger("MYSQL INVENTORY SERVICE");
        MySQLInventoryItemService m_InventoryItemService;
        MySQLInventoryFolderService m_InventoryFolderService;

        public MySQLInventoryService(string connectionString)
        {
            m_ConnectionString = connectionString;
            m_InventoryItemService = new MySQLInventoryItemService(connectionString);
            m_InventoryFolderService = new MySQLInventoryFolderService(connectionString);
        }

        public override InventoryFolderServiceInterface Folder
        {
            get
            {
                return m_InventoryFolderService;
            }
        }

        public override InventoryItemServiceInterface Item
        {
            get 
            {
                return m_InventoryItemService;
            }
        }

        public override List<InventoryItem> getActiveGestures(UUID PrincipalID)
        {
            List<InventoryItem> items = new List<InventoryItem>();
            using (MySqlConnection connection = new MySqlConnection(m_ConnectionString))
            {
                connection.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM inventoryitems WHERE OwnerID LIKE ?ownerid AND AssetType = ?assettype AND (flags & 1) <>0", connection))
                {
                    cmd.Parameters.AddWithValue("?ownerid", PrincipalID);
                    cmd.Parameters.AddWithValue("?assettype", (int)AssetType.Gesture);
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
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "inventoryfolders", Migrations_inventoryfolders, m_Log);
            MySQLUtilities.ProcessMigrations(m_ConnectionString, "inventoryitems", Migrations_inventoryitems, m_Log);
        }

        private static readonly string[] Migrations_inventoryfolders = new string[]{
            "CREATE TABLE %tablename% (" +
                "ID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "ParentFolderID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "OwnerID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "Name VARCHAR(64) NOT NULL DEFAULT ''," +
                "InventoryType INT(11) NOT NULL DEFAULT '-1'," +
                "Version INT(11) NOT NULL DEFAULT '1'," +
                "PRIMARY KEY(ID)," +
                "KEY inventoryfolders_owner_index (OwnerID)," + 
                "KEY inventoryfolders_owner_folderid (OwnerID, ParentFolderID)," + 
                "KEY inventoryfolders_owner_type (OwnerID, InventoryType))"
        };

        private static readonly string[] Migrations_inventoryitems = new string[]{
            "CREATE TABLE %tablename% (" +
                "ID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "ParentFolderID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "Name VARCHAR(64) NOT NULL DEFAULT ''," +
                "Description VARCHAR(128) NOT NULL DEFAULT ''," +
                "InventoryType INT(11) NOT NULL DEFAULT '0'," +
                "Flags INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "OwnerID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "LastOwnerID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "CreatorID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "CreationDate BIGINT(20) UNSIGNED NOT NULL DEFAULT '0'," +
                "BasePermissionsMask INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "CurrentPermissionsMask INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "EveryOnePermissionsMask INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "NextOwnerPermissionsMask INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "GroupPermissionsMask INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "SalePrice INT(11) NOT NULL DEFAULT '10'," +
                "SaleType INT(11) NOT NULL DEFAULT '0'," +
                "SalePermissionsMask INT(11) UNSIGNED NOT NULL DEFAULT '0'," +
                "GroupID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "IsGroupOwned TINYINT(1) NOT NULL DEFAULT '0'," + 
                "AssetID CHAR(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'," +
                "AssetType INT(11) NOT NULL DEFAULT '0'," +
                "PRIMARY KEY(ID)," +
                "KEY inventoryitems_OwnerID (OwnerID)," +
                "KEY inventoryitems_OwnerID_ID (OwnerID, ID)," +
                "KEY inventoryitems_OwnerID_ParentFolderID (OwnerID, ParentFolderID))"
        };
        #endregion

        public void Startup(ConfigurationLoader loader)
        {
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
                        cmd.Parameters.AddWithValue("?ownerid", userAccount);
                        cmd.ExecuteNonQuery();
                    }
                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM inventoryfolders WHERE OwnerID LIKE ?ownerid", connection))
                    {
                        cmd.Parameters.AddWithValue("?ownerid", userAccount);
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
