/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.BackendConnectors.Robust.Common;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using HttpClasses;
using System.Collections.Generic;

namespace SilverSim.BackendConnectors.Robust.Inventory
{
    class RobustInventoryItemConnector : InventoryItemServiceInterface
    {
        private string m_InventoryURI;
        public int TimeoutMs = 20000;

        #region Constructor
        public RobustInventoryItemConnector(string uri)
        {
            m_InventoryURI = uri;
        }
        #endregion

        #region Accessors
        public override InventoryItem this[UUID PrincipalID, UUID key]
        {
            get
            {
                Dictionary<string, string> post = new Dictionary<string, string>();
                post["PRINCIPAL"] = PrincipalID;
                post["ID"] = key;
                post["METHOD"] = "GETITEM";
                Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));
                if (!(map["item"] is Map))
                {
                    throw new InventoryInaccessible();
                }

                return RobustInventoryConnector.ItemFromMap((Map)map["item"]);
            }
        }
        #endregion

        private Dictionary<string, string> SerializeItem(InventoryItem item)
        {
            Dictionary<string, string> post = new Dictionary<string,string>();
            post["ID"] = item.ID;
            post["AssetID"] = item.AssetID;
            post["CreatorId"] = item.Creator.ID;
            post["GroupID"] = item.GroupID;
            post["GroupOwned"] = item.GroupOwned.ToString();
            post["Folder"] = item.ParentFolderID;
            post["Owner"] = item.Owner.ID;
            post["Name"] = item.Name;
            post["InvType"] = ((int)item.InventoryType).ToString();
            post["AssetType"] = ((uint)item.AssetType).ToString();
            post["BasePermissions"] = ((uint)item.Permissions.Base).ToString();
            post["CreationDate"] = ((uint)item.CreationDate.DateTimeToUnixTime()).ToString();
            post["CreatorData"] = item.Creator.CreatorData;
            post["CurrentPermissions"] = ((uint)item.Permissions.Current).ToString();
            post["GroupPermissions"] = ((uint)item.Permissions.Group).ToString();
            post["Description"] = item.Description;
            post["EveryOnePermissions"] = ((uint)item.Permissions.EveryOne).ToString();
            post["Flags"] = item.Flags.ToString();
            post["NextPermissions"] = ((uint)item.Permissions.NextOwner).ToString();
            post["SalePrice"] = ((uint)item.SaleInfo.Price).ToString();
            post["SaleType"] = ((uint)item.SaleInfo.Type).ToString();

            return post;
        }

        public override void Add(InventoryItem item)
        {
            Dictionary<string, string> post = SerializeItem(item);
            post["METHOD"] = "ADDITEM";
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));
            if(!((AString)map["RESULT"]))
            {
                throw new InventoryItemNotStored(item.ID);
            }
        }

        public override void Update(InventoryItem item)
        {
            Dictionary<string, string> post = SerializeItem(item);
            post["METHOD"] = "UPDATEITEM";
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));
            if (!((AString)map["RESULT"]))
            {
                throw new InventoryItemNotStored(item.ID);
            }
        }

        public override void Delete(UUID PrincipalID, UUID ID)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["ITEMS[]"] = ID;
            post["PRINCIPAL"] = PrincipalID;
            post["METHOD"] = "DELETEITEMS";
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));
            if (!((AString)map["RESULT"]))
            {
                throw new InventoryItemNotFound(ID);
            }
        }

        public override void Move(UUID PrincipalID, UUID ID, UUID newFolder)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            post["IDLIST[]"] = ID;
            post["DESTLIST[]"] = newFolder;
            post["PRINCIPAL"] = PrincipalID;
            post["METHOD"] = "MOVEITEMS";
            Map map = OpenSimResponse.Deserialize(HttpRequestHandler.DoStreamPostRequest(m_InventoryURI, null, post, false, TimeoutMs));
            if (!((AString)map["RESULT"]))
            {
                throw new InventoryItemNotFound(ID);
            }
        }
    }
}
