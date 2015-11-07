// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Scripting.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types;
using ThreadedClasses;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class UpdateScriptAgent : UploadAssetAbstractCapability
    {
        readonly ViewerAgent m_Agent;
        readonly InventoryServiceInterface m_InventoryService;
        readonly AssetServiceInterface m_AssetService;
        readonly RwLockedDictionary<UUID, UUID> m_Transactions = new RwLockedDictionary<UUID, UUID>();

        public override string CapabilityName
        {
            get
            {
                return "UpdateScriptAgent";
            }
        }

        public override int ActiveUploads
        {
            get
            {
                return m_Transactions.Count;
            }
        }

        public UpdateScriptAgent(ViewerAgent agent, InventoryServiceInterface inventoryService, AssetServiceInterface assetService, string serverURI)
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
            if (m_Transactions.RemoveIf(transactionID, delegate(UUID v) { return true; }, out kvp))
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

                if (item.AssetType != data.Type)
                {
                    throw new UrlNotFoundException();
                }

                if (!item.CheckPermissions(m_Agent.Owner, m_Agent.Group, InventoryPermissionsMask.Modify))
                {
                    throw new UploadErrorException("Not allowed to modify script");
                }

                item.AssetID = data.ID;
                data.Name = item.Name;
                data.Creator = item.Creator;

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

                try
                {
                    using (TextReader reader = new StreamReader(data.InputStream))
                    {
                        ScriptLoader.SyntaxCheck(item.Owner, data);
                    }
                    m.Add("compiled", true);
                }
                catch(CompilerException e)
                {
                    AnArray errors = new AnArray();
                    foreach(KeyValuePair<int, string> line in e.Messages)
                    {
                        errors.Add(string.Format("{0}:{1}", kvp.Key, kvp.Value));
                    }
                    m.Add("errors", errors);
                    m.Add("compiled", false);
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
                return AssetType.LSLText;
            }
        }
    }
}
