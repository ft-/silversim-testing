// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3
using MySql.Data.MySqlClient;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Database.MySQL.Inventory
{
    public static class MySQLInventoryExtensionMethods
    {
        public static InventoryFolder ToFolder(this MySqlDataReader reader)
        {
            InventoryFolder folder = new InventoryFolder(reader.GetUUID("ID"));

            folder.ParentFolderID = reader.GetUUID("ParentFolderID");
            folder.Name = reader.GetString("Name");
            folder.InventoryType = reader.GetEnum<InventoryType>("InventoryType");
            folder.Owner.ID = reader.GetUUID("OwnerID");
            folder.Version = reader.GetInt32("Version");

            return folder;
        }

        public static Dictionary<string, object> ToDictionary(this InventoryFolder folder)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict["ID"] = folder.ID;
            dict["ParentFolderID"] = folder.ParentFolderID;
            dict["Name"] = folder.Name;
            dict["InventoryType"] = folder.InventoryType;
            dict["OwnerID"] = folder.Owner.ID;
            dict["Version"] = folder.Version;
            return dict;
        }

        public static InventoryItem ToItem(this MySqlDataReader reader)
        {
            InventoryItem item = new InventoryItem(reader.GetUUID("ID"));

            item.ParentFolderID = reader.GetUUID("ParentFolderID");
            item.Name = reader.GetString("Name");
            item.Description = reader.GetString("Description");
            item.InventoryType = reader.GetEnum<InventoryType>("InventoryType");
            item.Flags = reader.GetEnum<InventoryFlags>("InventoryFlags");
            item.Owner.ID = reader.GetUUID("OwnerID");
            item.LastOwner.ID = reader.GetUUID("LastOwnerID");
            
            item.Creator.ID = reader.GetUUID("CreatorID");

            item.CreationDate = reader.GetDate("CreationDate");
            item.Permissions.Base = reader.GetEnum<InventoryPermissionsMask>("BasePermissionsMask");
            item.Permissions.Current = reader.GetEnum<InventoryPermissionsMask>("CurrentPermissionsMask");
            item.Permissions.EveryOne = reader.GetEnum<InventoryPermissionsMask>("EveryOnePermissionsMask");
            item.Permissions.NextOwner = reader.GetEnum<InventoryPermissionsMask>("NextOwnerPermissionsMask");
            item.Permissions.Group = reader.GetEnum<InventoryPermissionsMask>("GroupPermissionsMask");
            item.SaleInfo.Price = reader.GetInt32("SalePrice");
            item.SaleInfo.Type = reader.GetEnum<InventoryItem.SaleInfoData.SaleType>("SaleType");
            item.SaleInfo.PermMask = reader.GetEnum<InventoryPermissionsMask>("SalePermissionsMask");
            item.Group.ID = reader.GetUUID("GroupID");
            item.IsGroupOwned = reader.GetBool("IsGroupOwned");
            item.AssetID = reader.GetUUID("AssetID");
            item.AssetType = reader.GetEnum<AssetType>("AssetType");

            return item;
        }

        public static Dictionary<string, object> ToDictionary(this InventoryItem item)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict["ID"] = item.ID;
            dict["ParentFolderID"] = item.ParentFolderID;
            dict["Name"] = item.Name;
            dict["Description"] = item.Description;
            dict["InventoryType"] = item.InventoryType;
            dict["Flags"] = item.Flags;
            dict["OwnerID"] = item.Owner.ID;
            dict["CreatorID"] = item.Creator.ID;
            dict["CreationDate"] = item.CreationDate.DateTimeToUnixTime();
            dict["BasePermissionsMask"] = (uint)item.Permissions.Base;
            dict["CurrentPermissionsMask"] = (uint)item.Permissions.Current;
            dict["EveryOnePermissionsMask"] = (uint)item.Permissions.EveryOne;
            dict["NextOwnerPermissionsMask"] = (uint)item.Permissions.NextOwner;
            dict["GroupPermissionsMask"] = (uint)item.Permissions.Group;
            dict["SalePrice"] = item.SaleInfo.Price;
            dict["SaleType"] = item.SaleInfo.Type;
            dict["GroupID"] = item.Group.ID;
            dict["IsGroupOwned"] = item.IsGroupOwned;
            dict["AssetID"] = item.AssetID;
            dict["AssetType"] = item.AssetType;

            return dict;
        }
    }
}
