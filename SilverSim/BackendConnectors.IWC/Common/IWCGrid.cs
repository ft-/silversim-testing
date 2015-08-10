// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpClient;
using SilverSim.StructuredData.JSON;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;

namespace SilverSim.BackendConnectors.IWC.Common
{
    public static class IWCGrid
    {
        #region Assets
        public static Map AssetDataToIWC(this AssetData data)
        {
            return new Map
            {
                {"AssetFlags", (uint)data.Flags},
                {"AssetID", data.ID},
                {"CreationDate", data.CreateTime},
                {"CreatorID", data.Creator.ID},
                {"Data", new BinaryData(data.Data)},
                {"LastAccessed", data.AccessTime},
                {"Name", data.Name},
                {"ParentID", data.CreateTime},
                {"TypeAsset", (int)data.Type},
                {"Description", ""},
            };
        }

        public static AssetData IWCtoAssetData(this Map m)
        {
            AssetData data = new AssetData();
            if (m.ContainsKey("AssetFlags"))
            {
                data.Flags = (AssetFlags)m["AssetFlags"].AsUInt;
            }
            if(m.ContainsKey("AssetID"))
            {
                data.ID = m.AsUUID;
            }
            if(m.ContainsKey("CreationDate"))
            {
                data.CreateTime = m["CreationDate"] as Date;
            }
            if(m.ContainsKey("CreatorID"))
            {
                data.Creator.ID = m["CreatorID"].AsUUID;
            }
            if(m.ContainsKey("Data"))
            {
                data.Data = m["Data"] as BinaryData;
            }
            if(m.ContainsKey("LastAccessed"))
            {
                data.AccessTime = m["LastAccessed"] as Date;
            }
            if(m.ContainsKey("Name"))
            {
                data.Name = m["Name"].ToString();
            }
            if(m.ContainsKey("TypeAsset"))
            {
                data.Type = (AssetType)m["TypeAsset"].AsInt;
            }
            return data;
        }

        public static AssetMetadata IWCtoAssetMetadata(this Map m)
        {
            AssetMetadata data = new AssetMetadata();
            if (m.ContainsKey("AssetFlags"))
            {
                data.Flags = (AssetFlags)m["AssetFlags"].AsUInt;
            }
            if (m.ContainsKey("AssetID"))
            {
                data.ID = m.AsUUID;
            }
            if (m.ContainsKey("CreationDate"))
            {
                data.CreateTime = m["CreationDate"] as Date;
            }
            if (m.ContainsKey("CreatorID"))
            {
                data.Creator.ID = m["CreatorID"].AsUUID;
            }
            if (m.ContainsKey("LastAccessed"))
            {
                data.AccessTime = m["LastAccessed"] as Date;
            }
            if (m.ContainsKey("Name"))
            {
                data.Name = m["Name"].ToString();
            }
            if (m.ContainsKey("TypeAsset"))
            {
                data.Type = (AssetType)m["TypeAsset"].AsInt;
            }
            return data;
        }
        #endregion

        #region Inventory Folders
        public static Map FolderToIWC(this InventoryFolder folder)
        {
            return new Map
            {
                {"ID", folder.ID},
                {"Name", folder.Name},
                {"Owner", folder.Owner.ID},
                {"Type", (int)folder.InventoryType},
                {"ParentID", folder.ParentFolderID},
                {"Version", (int)folder.Version}
            };
        }

        public static InventoryFolder IWCtoFolder(this Map m)
        {
            InventoryFolder folder = new InventoryFolder();
            folder.ID = m["ID"].AsUUID;
            folder.Name = m["Name"].ToString();
            folder.Owner.ID = m["Owner"].AsUUID;
            folder.InventoryType = (InventoryType)m["Type"].AsInt;
            folder.ParentFolderID = m["ParentID"].AsUUID;
            folder.Version = m["Version"].AsInt;
            return folder;
        }
        #endregion

        #region Inventory items
        public static Map ItemToIWC(this InventoryItem item)
        {
            return new Map
            {
                {"AssetID", item.AssetID},
                {"AssetType", (int)item.AssetType},
                {"BasePermissions", (uint)item.Permissions.Base},
                {"CreationDate", item.CreationDate},
                {"CreatorData", item.Creator.CreatorData},
                {"CreatorId", item.Creator.ID},
                {"CreatorIdentification", item.Creator.ToString()},
                {"CurrentPermissions", (uint)item.Permissions.Current},
                {"Description", item.Description},
                {"EveryOnePermissions", (uint)item.Permissions.EveryOne},
                {"Flags", item.Flags},
                {"Folder", item.ParentFolderID},
                {"GroupID", item.Group.ID},
                {"GroupOwned", item.IsGroupOwned},
                {"GroupPermissions", (uint)item.Permissions.Group},
                {"ID", item.ID},
                {"InvType", (int)item.InventoryType},
                {"Name", item.Name},
                {"NextPermissions", (uint)item.Permissions.NextOwner},
                {"Owner", item.Owner.ID},
                {"SalePrice", item.SaleInfo.Price},
                {"SaleType", (int)item.SaleInfo.Type}
            };
        }

        public static InventoryItem IWCtoItem(this Map m)
        {
            InventoryItem item = new InventoryItem();
            item.AssetID = m["AssetID"].AsUUID;
            item.AssetType = (AssetType)m["AssetType"].AsInt;
            item.Permissions.Base = (InventoryPermissionsMask)m["BasePermissions"].AsUInt;
            item.CreationDate = m["CreationDate"] as Date;
            item.Creator.FullName = m["CreatorIdentification"].ToString();
            item.Permissions.Current = (InventoryPermissionsMask)m["CurrentPermissions"].AsUInt;
            item.Description = m["Description"].ToString();
            item.Permissions.EveryOne = (InventoryPermissionsMask)m["EveryOnePermissions"].AsUInt;
            item.Flags = m["Flags"].AsUInt;
            item.ParentFolderID = m["Folder"].AsUUID;
            item.Group.ID = m["GroupID"].AsUUID;
            item.IsGroupOwned = m["GroupOwned"].AsBoolean;
            item.Permissions.Group = (InventoryPermissionsMask)m["GroupPermissions"].AsUInt;
            item.ID = m["ID"].AsUUID;
            item.InventoryType = (InventoryType)m["InvType"].AsInt;
            item.Name = m["Name"].ToString();
            item.Permissions.NextOwner = (InventoryPermissionsMask)m["NextPermissions"].AsUInt;
            item.Owner.ID = m["Owner"].AsUUID;
            item.SaleInfo.Price = m["SalePrice"].AsInt;
            item.SaleInfo.Type = (InventoryItem.SaleInfoData.SaleType)m["SaleType"].AsInt;
            return item;
        }
        #endregion

        #region IWC RPC
        public static Map PostToService(string serverUrl, string methodName, Map param, bool compressed, int timeoutms = 100000)
        {
            param.Add("Method", methodName);
            // param.Add("Password", "");

            return (Map)JSON.Deserialize(
                HttpRequestHandler.DoStreamRequest(
                    "POST", 
                    serverUrl, 
                    null, 
                    "application/json", 
                    JSON.Serialize(param),
                    compressed, 
                    timeoutms));
        }

        public static Map PostToService(string serverUrl, string methodName, Map param, int timeoutms = 100000)
        {
            param.Add("Method", methodName);
            // param.Add("Password", "");

            return (Map)JSON.Deserialize(
                HttpRequestHandler.DoStreamRequest(
                    "POST",
                    serverUrl,
                    null,
                    "application/json",
                    JSON.Serialize(param),
                    false,
                    timeoutms));
        }
        #endregion
    }
}
