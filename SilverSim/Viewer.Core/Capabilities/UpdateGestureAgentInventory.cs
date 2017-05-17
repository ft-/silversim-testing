// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System.Collections.Generic;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class UpdateGestureAgentInventory : UploadAssetAbstractCapability
    {
        readonly ViewerAgent m_Agent;
        readonly InventoryServiceInterface m_InventoryService;
        readonly AssetServiceInterface m_AssetService;
        readonly RwLockedDictionary<UUID, UUID> m_Transactions = new RwLockedDictionary<UUID, UUID>();

        public override string CapabilityName
        {
            get
            {
                return "UpdateGestureAgentInventory";
            }
        }

        public override int ActiveUploads
        {
            get
            {
                return m_Transactions.Count;
            }
        }

        public UpdateGestureAgentInventory(
            ViewerAgent agent, 
            InventoryServiceInterface inventoryService, 
            AssetServiceInterface assetService, 
            string serverURI, 
            string remoteip)
            : base(agent.Owner, serverURI, remoteip)
        {
            m_InventoryService = inventoryService;
            m_AssetService = assetService;
            m_Agent = agent;
        }

        public override UUID GetUploaderID(Map reqmap)
        {
            var transaction = UUID.Random;
            m_Transactions.Add(transaction, reqmap["item_id"].AsUUID);
            return transaction;
        }

        public override Map UploadedData(UUID transactionID, AssetData data)
        {
            KeyValuePair<UUID, UUID> kvp;
            if (m_Transactions.RemoveIf(transactionID, delegate(UUID v) { return true; }, out kvp))
            {
                var m = new Map();
                InventoryItem item;
                try
                {
                    item = m_InventoryService.Item[m_Creator.ID, kvp.Value];
                }
                catch
                {
                    throw new UrlNotFoundException();
                }

                if (item.AssetType != data.Type)
                {
                    throw new UrlNotFoundException();
                }

                if(!item.CheckPermissions(m_Agent.Owner, m_Agent.Group, InventoryPermissionsMask.Modify))
                {
                    throw new UploadErrorException(this.GetLanguageString(m_Agent.CurrentCulture, "NotAllowedToModifyGesture", "Not allowed to modify gesture"));
                }
                item.AssetID = data.ID;
                data.Creator = item.Creator;
                data.Name = item.Name;

                try
                {
                    m_AssetService.Store(data);
                }
                catch
                {
                    throw new UploadErrorException(this.GetLanguageString(m_Agent.CurrentCulture, "FailedToStoreAsset", "Failed to store asset"));
                }

                try
                {
                    m_InventoryService.Item.Update(item);
                }
                catch
                {
                    throw new UploadErrorException(this.GetLanguageString(m_Agent.CurrentCulture, "FailedToStoreInventoryItem", "Failed to store inventory item"));
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
                return AssetType.Gesture;
            }
        }
    }
}
