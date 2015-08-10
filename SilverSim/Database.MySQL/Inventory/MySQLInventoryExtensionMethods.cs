﻿// SilverSim is distributed under the terms of the
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
            folder.Name = (string)reader["Name"];
            folder.InventoryType = (InventoryType)(int)reader["InventoryType"];
            folder.Owner.ID = reader.GetUUID("OwnerID");
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
            InventoryItem item = new InventoryItem(reader.GetUUID("ID"));

            item.ParentFolderID = reader.GetUUID("ParentFolderID");
            item.Name = (string)reader["Name"];
            item.Description = (string)reader["Description"];
            item.InventoryType = (InventoryType)(int)reader["InventoryType"];
            item.Flags = (uint)reader["InventoryFlags"];
            item.Owner.ID = reader.GetUUID("OwnerID");
            item.LastOwner.ID = reader.GetUUID("LastOwnerID");
            
            item.Creator.ID = reader.GetUUID("CreatorID");

            item.CreationDate = reader.GetDate("CreationDate");
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
