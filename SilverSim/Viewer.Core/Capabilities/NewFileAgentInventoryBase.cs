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

#pragma warning disable IDE0018

using SilverSim.Scene.Types.Object.Mesh.Item;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Core.Capabilities
{
    public abstract class NewFileAgentInventoryBase : UploadAssetAbstractCapability
    {
        private readonly InventoryServiceInterface m_InventoryService;
        private readonly AssetServiceInterface m_AssetService;
        private readonly ViewerAgent m_Agent;

        private readonly RwLockedDictionary<UUID, InventoryItem> m_Transactions = new RwLockedDictionary<UUID, InventoryItem>();

        public override int ActiveUploads => m_Transactions.Count;

        public NewFileAgentInventoryBase(ViewerAgent agent, string serverURI, string remoteip)
            : base(agent.Owner, serverURI, remoteip)
        {
            m_Agent = agent;
            m_InventoryService = agent.InventoryService;
            m_AssetService = agent.AssetService;
        }

        public override UUID GetUploaderID(Map reqmap)
        {
            var transaction = UUID.Random;
            var item = new InventoryItem
            {
                Description = reqmap["description"].ToString(),
                Name = reqmap["name"].ToString(),
                ParentFolderID = reqmap["folder_id"].AsUUID,
                AssetTypeName = reqmap["asset_type"].ToString(),
                InventoryTypeName = reqmap["inventory_type"].ToString(),
                LastOwner = Creator,
                Owner = Creator,
                Creator = Creator
            };
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
            if (m_Transactions.RemoveIf(transactionID, (InventoryItem v) => true, out kvp))
            {
                var m = new Map
                {
                    { "new_inventory_item", kvp.Value.ID.ToString() }
                };
                kvp.Value.AssetID = data.ID;
                data.Type = kvp.Value.AssetType;
                data.Name = kvp.Value.Name;

                if (kvp.Value.AssetType == AssetType.Mesh)
                {
                    /* special upload format for objects */
                    UploadObject(data);
                    kvp.Value.AssetType = AssetType.Object;
                }
                else if(kvp.Value.AssetType == AssetType.Object)
                {
                    throw new UploadErrorException(this.GetLanguageString(m_Agent.CurrentCulture, "FailedToStoreAsset", "Failed to store asset"));
                }

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

        private void UploadObject(AssetData data)
        {
            var meshitem = MeshInventoryItem.FromUploadFormat(data.Name, data.InputStream, Creator, data);

            /* Store all produced assets */
            foreach(AssetData asset in meshitem.Assets)
            {
                m_AssetService.Store(asset);
            }

            if(meshitem.TextureItems.Count != 0)
            {
                var textureFolder = m_InventoryService.Folder[Creator.ID, AssetType.Texture].ID;
                var folder = new InventoryFolder
                {
                    Name = data.Name + " - Textures",
                    Owner = Creator,
                    DefaultType = AssetType.Unknown,
                    ParentFolderID = textureFolder,
                    Version = 1
                };
                m_InventoryService.Folder.Add(folder);

                foreach(InventoryItem item in meshitem.TextureItems)
                {
                    item.ParentFolderID = folder.ID;
                    m_InventoryService.Item.Add(item);
                }
            }
        }

        protected override UUID NewAssetID => UUID.Random;

        protected override bool AssetIsLocal => false;

        protected override bool AssetIsTemporary => false;

        protected override AssetType NewAssetType => AssetType.Unknown;
    }
}
