// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ThreadedClasses;

namespace SilverSim.Viewer.Core.Capabilities
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
    public class NewFileAgentInventory : UploadAssetAbstractCapability
    {
        readonly InventoryServiceInterface m_InventoryService;
        readonly AssetServiceInterface m_AssetService;
        readonly ViewerAgent m_Agent;

        readonly RwLockedDictionary<UUID, InventoryItem> m_Transactions = new RwLockedDictionary<UUID, InventoryItem>();

        public override string CapabilityName
        {
            get
            {
                return "NewFileAgentInventory";
            }
        }

        public override int ActiveUploads
        {
            get
            {
                return m_Transactions.Count;
            }
        }

        public NewFileAgentInventory(ViewerAgent agent, string serverURI)
            : base(agent.Owner, serverURI)
        {
            m_Agent = agent;
            m_InventoryService = agent.InventoryService;
            m_AssetService = agent.AssetService;
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
                    throw new UploadErrorException(this.GetLanguageString(m_Agent.CurrentCulture, "FailedToStoreAsset", "Failed to store asset"));
                }

                try
                {
                    m_InventoryService.Item.Add(kvp.Value);
                }
                catch
#if DEBUG
                (Exception e)
#endif
                {
                    throw new UploadErrorException(this.GetLanguageString(m_Agent.CurrentCulture, "FailedToStoreNewInventoryItem", "Failed to store new inventory item"));
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
