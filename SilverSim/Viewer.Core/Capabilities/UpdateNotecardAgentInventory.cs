// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System.Collections.Generic;
using ThreadedClasses;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class UpdateNotecardAgentInventory : UploadAssetAbstractCapability
    {
        ViewerAgent m_Agent;
        private InventoryServiceInterface m_InventoryService;
        private AssetServiceInterface m_AssetService;
        private readonly RwLockedDictionary<UUID, UUID> m_Transactions = new RwLockedDictionary<UUID, UUID>();

        public override string CapabilityName
        {
            get
            {
                return "UpdateNotecardAgentInventory";
            }
        }

        public override int ActiveUploads
        {
            get
            {
                return m_Transactions.Count;
            }
        }

        public UpdateNotecardAgentInventory(ViewerAgent agent, InventoryServiceInterface inventoryService, AssetServiceInterface assetService, string serverURI)
            : base(agent.Owner, serverURI)
        {
            m_Agent = agent;
            m_InventoryService = inventoryService;
            m_AssetService = assetService;
        }

        public override UUID GetUploaderID(Map reqmap)
        {
            UUID transaction = UUID.Random;
            m_Transactions.Add(transaction, reqmap["item_id"].AsUUID);
            return transaction;
        }

        public override Map UploadedData(UUID transactionID, AssetData data)
        {
            KeyValuePair<UUID, UUID> kvp;
            if(m_Transactions.RemoveIf(transactionID, delegate(UUID v) { return true; }, out kvp))
            {
                Map m = new Map();
                InventoryItem item;
                try
                {
                    item = m_InventoryService.Item[m_Creator.ID, kvp.Value];
                }
                catch
                {
                    throw new UrlNotFoundException();
                }

                if(item.AssetType != data.Type)
                {
                    throw new UrlNotFoundException();
                }

                if(!item.CheckPermissions(m_Agent.Owner, m_Agent.Group, InventoryPermissionsMask.Modify))
                {
                    throw new UploadErrorException("Not allowed to modify notecard");
                }

                item.AssetID = data.ID;

                try
                {
                    m_AssetService.Store(data);
                }
                catch
                {
                    throw new UploadErrorException("Failed to store asset");
                }

                try
                {
                    m_InventoryService.Item.Update(item);
                }
                catch
                {
                    throw new UploadErrorException("Failed to store inventory item");
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
                return AssetType.Notecard;
            }
        }
    }
}
