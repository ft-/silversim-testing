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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types;
using ThreadedClasses;

namespace SilverSim.LL.Core.Capabilities
{
    public class NewFileAgentInventoryVariablePrice : UploadAssetAbstractCapability
    {
        private InventoryServiceInterface m_InventoryService;
        private AssetServiceInterface m_AssetService;

        private readonly RwLockedDictionary<UUID, InventoryItem> m_Transactions = new RwLockedDictionary<UUID, InventoryItem>();

        public override string CapabilityName
        {
            get
            {
                return "NewFileAgentInventoryVariablePrice";
            }
        }

        public override int ActiveUploads
        {
            get
            {
                return m_Transactions.Count;
            }
        }

        public NewFileAgentInventoryVariablePrice(UUI creator, InventoryServiceInterface inventoryService, AssetServiceInterface assetService, string serverURI)
            : base(creator, serverURI)
        {
            m_InventoryService = inventoryService;
            m_AssetService = assetService;
        }

        public override UUID GetUploaderID(Map reqmap)
        {
            UUID transaction = UUID.Random;
            InventoryItem item = new InventoryItem();
            item.ID = UUID.Random;
            item.Description = reqmap["description"].ToString();
            item.Name = reqmap["name"].ToString();
            item.ParentFolderID = reqmap["folder_id"].AsUUID;
            item.AssetTypeName = reqmap["asset_type"].ToString();
            item.InventoryTypeName = reqmap["inventory_type"].ToString();
            item.LastOwner = m_Creator;
            item.Owner = m_Creator;
            item.Creator = m_Creator;
            item.Permissions.Base = InventoryPermissionsMask.All;
            item.Permissions.Current = InventoryPermissionsMask.Every;
            item.Permissions.EveryOne = (InventoryPermissionsMask)reqmap["everyone_mask"].AsUInt;
            item.Permissions.Group = (InventoryPermissionsMask)reqmap["group_mask"].AsUInt;
            item.Permissions.NextOwner = (InventoryPermissionsMask)reqmap["next_owner_mask"].AsUInt;
            m_Transactions.Add(transaction, item);
            return transaction;
        }

        public override Map UploadedData(UUID transactionID, AssetData data)
        {
            KeyValuePair<UUID, InventoryItem> kvp;
            if (m_Transactions.RemoveIf(transactionID, delegate(InventoryItem v) { return true; }, out kvp))
            {
                Map m = new Map();
                m.Add("new_inventory_item", kvp.Value.ID.ToString());
                kvp.Value.AssetID = data.ID;
                data.Type = kvp.Value.AssetType;
                data.Name = kvp.Value.Name;

                try
                {
                    m_AssetService.Store(data);
                }
                catch
                {
                    throw new UploadErrorException("Could not store asset");
                }

                try
                {
                    m_InventoryService.Item.Add(kvp.Value);
                }
                catch
                {
                    throw new UploadErrorException("Could not store new inventory item");
                }
                return m;
            }
            else
            {
                throw new UrlNotFoundException();
            }
        }

        protected override UUID NewAssetID
        {
            get
            {
                return UUID.Random;
            }
        }

        protected override bool AssetIsLocal
        {
            get
            {
                return false;
            }
        }

        protected override bool AssetIsTemporary
        {
            get
            {
                return false;
            }
        }

        protected override AssetType NewAssetType
        {
            get
            {
                return AssetType.Unknown;
            }
        }
    }
}
