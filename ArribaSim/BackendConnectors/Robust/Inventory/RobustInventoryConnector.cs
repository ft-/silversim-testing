/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using ArribaSim.BackendConnectors.Robust.Common;
using ArribaSim.ServiceInterfaces.Inventory;
using ArribaSim.Types;
using ArribaSim.Types.Asset;
using ArribaSim.Types.Inventory;
using HttpClasses;
using System.Collections.Generic;

namespace ArribaSim.BackendConnectors.Robust.Inventory
{
    public class RobustInventoryConnector : InventoryServiceInterface
    {
        private string m_InventoryURI;
        private RobustInventoryFolderConnector m_FolderService;
        private RobustInventoryItemConnector m_ItemService;
        private int m_TimeoutMs = 20000;

        #region Constructor
        public RobustInventoryConnector(string uri)
        {
            if(!uri.EndsWith("/"))
            {
                uri += "/";
            }
            uri += "xinventory";
            m_InventoryURI = uri;
            m_ItemService = new RobustInventoryItemConnector(uri);
            m_ItemService.TimeoutMs = m_TimeoutMs;
            m_FolderService = new RobustInventoryFolderConnector(uri);
            m_FolderService.TimeoutMs = m_TimeoutMs;
        }
        #endregion

        #region Accessors
        public int TimeoutMs
        {
            get
            {
                return m_TimeoutMs;
            }
            set
            {
                m_TimeoutMs = value;
                m_FolderService.TimeoutMs = value;
                m_ItemService.TimeoutMs = value;
            }
        }

        public override InventoryFolderServiceInterface Folder
        {
            get
            {
                return m_FolderService;
            }
        }

        public override InventoryItemServiceInterface Item
        {
            get
            {
                return m_ItemService;
            }
        }

        public override List<InventoryItem> getActiveGestures(UUID PrincipalID)
        {
            Dictionary<string, string> post = new Dictionary<string,string>();
            post["PRINCIPAL"] = PrincipalID;
            post["METHOD"] = "GETACTIVEGESTURES";
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));
            if (!(map["ITEMS"] is Map))
            {
                throw new InventoryInaccessible();
            }

            List<InventoryItem> items = new List<InventoryItem>();
            foreach(KeyValuePair<string, IValue> i in (Map)map["ITEMS"])
            {
                if(i.Value is Map)
                {
                    items.Add(ItemFromMap((Map)i.Value));
                }
            }
            return items;
        }
        #endregion

        #region Map converson
        public static InventoryFolder FolderFromMap(Map map)
        {
            InventoryFolder folder = new InventoryFolder();
            folder.ID = map["ID"].AsUUID;
            folder.Owner.ID = map["Owner"].AsUUID;
            folder.Name = map["Name"].AsString.ToString();
            folder.Version = map["Version"].AsInteger;
            folder.InventoryType = (InventoryType)map["Type"].AsInt;
            folder.ParentFolderID = map["ParentID"].AsUUID;
            return folder;
        }
        public static InventoryItem ItemFromMap(Map map)
        {
            InventoryItem item = new InventoryItem();
            item.ID = map["ID"].AsUUID;
            item.AssetID = map["AssetID"].AsUUID;
            item.AssetType = (AssetType)map["AssetType"].AsInt;
            item.Permissions.Base = map["BasePermissions"].AsUInt;
            item.CreationDate = Date.UnixTimeToDateTime(map["CreationDate"].AsULong);
            item.Creator = new UUI(map["CreatorId"].AsUUID, map["CreatorData"].AsString.ToString());
            item.Permissions.Current = map["CurrentPermissions"].AsUInt;
            item.Description = map["Description"].AsString.ToString();
            item.Permissions.EveryOne = map["EveryOnePermissions"].AsUInt;
            item.Flags = map["Flags"].AsUInt;
            item.ParentFolderID = map["Folder"].AsUUID;
            item.GroupID = map["GroupID"].AsUUID;
            item.GroupOwned = map["GroupOwned"].AsBoolean;
            item.Permissions.Group = map["GroupPermissions"].AsUInt;
            item.InventoryType = (InventoryType) map["InvType"].AsInt;
            item.Name = map["Name"].AsString.ToString();
            item.Permissions.NextOwner = map["NextPermissions"].AsUInt;
            item.Owner.ID = map["Owner"].AsUUID;
            item.SaleInfo.Price = map["SalePrice"].AsUInt;
            item.SaleInfo.Type = (InventoryItem.SaleInfoData.SaleType) map["SaleType"].AsUInt;
            return item;
        }
        #endregion
    }
}
