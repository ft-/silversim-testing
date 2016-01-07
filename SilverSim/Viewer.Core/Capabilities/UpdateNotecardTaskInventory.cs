﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System.Collections.Generic;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class UpdateNotecardTaskInventory : UploadAssetAbstractCapability
    {
        sealed class TransactionInfo
        {
            public UUID TaskID;
            public UUID ItemID;

            public TransactionInfo(UUID taskID, UUID itemID)
            {
                TaskID = taskID;
                ItemID = itemID;
            }
        }

        readonly ViewerAgent m_Agent;
        readonly SceneInterface m_Scene;
        readonly RwLockedDictionary<UUID, TransactionInfo> m_Transactions = new RwLockedDictionary<UUID, TransactionInfo>();

        public override string CapabilityName
        {
            get
            {
                return "UpdateNotecardTaskInventory";
            }
        }

        public override int ActiveUploads
        {
            get
            {
                return m_Transactions.Count;
            }
        }

        public UpdateNotecardTaskInventory(ViewerAgent agent, SceneInterface scene, string serverURI)
            : base(agent.Owner, serverURI)
        {
            m_Agent = agent;
            m_Scene = scene;
        }

        public override UUID GetUploaderID(Map reqmap)
        {
            UUID transaction = UUID.Random;
            m_Transactions.Add(transaction, new TransactionInfo(reqmap["task_id"].AsUUID, reqmap["item_id"].AsUUID));
            return transaction;
        }

        public override Map UploadedData(UUID transactionID, AssetData data)
        {
            KeyValuePair<UUID, TransactionInfo> kvp;
            if (m_Transactions.RemoveIf(transactionID, delegate(TransactionInfo v) { return true; }, out kvp))
            {
                Map m = new Map();
                ObjectPartInventoryItem item;
                ObjectPart part = m_Scene.Primitives[kvp.Value.TaskID];
                try
                {
                    item = part.Inventory[kvp.Value.ItemID];
                }
                catch
                {
                    throw new UrlNotFoundException();
                }

                if (item.AssetType != data.Type)
                {
                    throw new UrlNotFoundException();
                }

                if(!part.CheckPermissions(m_Agent.Owner, m_Agent.Group, InventoryPermissionsMask.Modify) ||
                    !item.CheckPermissions(m_Agent.Owner, m_Agent.Group, InventoryPermissionsMask.Modify))
                {
                    throw new UploadErrorException(this.GetLanguageString(m_Agent.CurrentCulture, "NotAllowedToModifyNotecard", "Not allowed to modify notecard"));
                }

                item.AssetID = data.ID;
                data.Creator = item.Creator;
                data.Name = item.Name;
                item.ID = UUID.Random;

                try
                {
                    m_Scene.AssetService.Store(data);
                }
                catch
                {
                    throw new UploadErrorException(this.GetLanguageString(m_Agent.CurrentCulture, "FailedToStoreAsset", "Failed to store asset"));
                }

                try
                {
                    part.Inventory.Remove(kvp.Value.ItemID);
                    part.Inventory.Add(item.ID, item.Name, item);
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
                return AssetType.Notecard;
            }
        }
    }
}
