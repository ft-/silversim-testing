// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
using log4net;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class UploadBakedTexture : UploadAssetAbstractCapability
    {
        static readonly ILog m_Log = LogManager.GetLogger("UPLOAD BAKED TEXTURE");
        readonly AssetServiceInterface m_AssetService;
        readonly RwLockedDictionary<UUID, UUID> m_Transactions = new RwLockedDictionary<UUID, UUID>();

        public override string CapabilityName
        {
            get
            {
                return "UploadBakedTexture";
            }
        }

        public override int ActiveUploads
        {
            get
            {
                return m_Transactions.Count;
            }
        }

        public UploadBakedTexture(UUI creator, AssetServiceInterface assetService, string serverURI)
            : base(creator, serverURI)
        {
            m_AssetService = assetService;
        }

        public override UUID GetUploaderID(Map reqmap)
        {
            UUID transaction = UUID.Random;
            m_Transactions.Add(transaction, UUID.Zero);
            return transaction;
        }

        public override Map UploadedData(UUID transactionID, AssetData data)
        {
            KeyValuePair<UUID, UUID> kvp;
            if (m_Transactions.RemoveIf(transactionID, delegate(UUID v) { return true; }, out kvp))
            {
                Map m = new Map();

                if (data.Type != NewAssetType)
                {
                    throw new UrlNotFoundException();
                }

                data.Name = "Baked Texture for Agent " + m_Creator.ID.ToString();
                try
                {
                    m_AssetService.Store(data);
                    m_Log.InfoFormat("Uploaded baked texture {1} for {0}", m_Creator.FullName, data.ID);
                }
                catch
                {
                    throw new UploadErrorException("Failed to store asset");
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
                return UUID.RandomFixedFirst(0xFFFFFFFF);
            }
        }

        protected override bool AssetIsLocal
        {
            get
            {
                return true;
            }
        }

        protected override bool AssetIsTemporary
        {
            get
            {
                return true;
            }
        }

        protected override AssetType NewAssetType
        {
            get
            {
                return AssetType.Texture;
            }
        }
    }
}
