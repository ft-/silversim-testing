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
            InventoryFolder folder = new InventoryFolder((string)reader["ID"]);

            folder.ParentFolderID = MySQLUtilities.GetUUID(reader, "ParentFolderID");
            folder.Name = (string)reader["Name"];
            folder.InventoryType = (InventoryType)(int)reader["InventoryType"];
            folder.Owner.ID = MySQLUtilities.GetUUID(reader, "OwnerID");
            folder.Version = (int)reader["Version"];

            return folder;
        }

        public static Dictionary<string, object> ToDictionary(this InventoryFolder folder)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict["ID"] = folder.ID;
            dict["ParentFolderID"] = folder.ParentFolderID;
            dict["Name"] = folder.Name;
            dict["InventoryType"] = (int)folder.InventoryType;
            dict["OwnerID"] = folder.Owner.ID;
            dict["Version"] = folder.Version;
            return dict;
        }

        public static InventoryItem ToItem(this MySqlDataReader reader)
        {
            InventoryItem item = new InventoryItem((string)reader["ID"]);

            item.ParentFolderID = (string)reader["ParentFolderID"];
            item.Name = (string)reader["Name"];
            item.Description = (string)reader["Description"];
            item.InventoryType = (InventoryType)(int)reader["InventoryType"];
            item.Flags = (uint)reader["InventoryFlags"];
            item.Owner.ID = MySQLUtilities.GetUUID(reader, "OwnerID");
            item.LastOwner.ID = MySQLUtilities.GetUUID(reader, "LastOwnerID");
            
            item.Creator.ID = MySQLUtilities.GetUUID(reader, "CreatorID");

            item.CreationDate = MySQLUtilities.GetDate(reader, "CreationDate");
            item.Permissions.Base = (InventoryPermissionsMask)(uint)reader["BasePermissionsMask"];
            item.Permissions.Current = (InventoryPermissionsMask)(uint)reader["CurrentPermissionsMask"];
            item.Permissions.EveryOne = (InventoryPermissionsMask)(uint)reader["EveryOnePermissionsMask"];
            item.Permissions.NextOwner = (InventoryPermissionsMask)(uint)reader["NextOwnerPermissionsMask"];
            item.Permissions.Group = (InventoryPermissionsMask)(uint)reader["GroupPermissionsMask"];
            item.SaleInfo.Price = (int)reader["SalePrice"];
            item.SaleInfo.Type = (InventoryItem.SaleInfoData.SaleType)(uint)reader["SaleType"];
            item.SaleInfo.PermMask = (InventoryPermissionsMask)(uint)reader["SalePermissionsMask"];
            item.Group.ID = reader.GetUUID("GroupID");
            item.IsGroupOwned = ((int)reader["IsGroupOwned"]) != 0;
            item.AssetID = (string)reader["AssetID"];
            item.AssetType = (AssetType)(int)reader["AssetType"];

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
            dict["InventoryFlags"] = item.Flags;
            dict["OwnerID"] = item.Owner.ID;
            dict["CreatorID"] = item.Creator.ID;
            dict["CreationDate"] = item.CreationDate.DateTimeToUnixTime();
            dict["BasePermissionsMask"] = (uint)item.Permissions.Base;
            dict["CurrentPermissionsMask"] = (uint)item.Permissions.Current;
            dict["EveryOnePermissionsMask"] = (uint)item.Permissions.EveryOne;
            dict["NextOwnerPermissionsMask"] = (uint)item.Permissions.NextOwner;
            dict["GroupPermissionsMask"] = (uint)item.Permissions.Group;
            dict["SalePrice"] = item.SaleInfo.Price;
            dict["SaleType"] = (uint)item.SaleInfo.Type;
            dict["GroupID"] = item.Group.ID;
            dict["IsGroupOwned"] = item.IsGroupOwned;
            dict["AssetID"] = item.AssetID;
            dict["AssetType"] = (int)item.AssetType;

            return dict;
        }
    }
}
