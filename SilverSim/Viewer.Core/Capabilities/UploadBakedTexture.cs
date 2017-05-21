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

using log4net;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;

namespace SilverSim.Viewer.Core.Capabilities
{
    public class UploadBakedTexture : UploadAssetAbstractCapability
    {
        private static readonly ILog m_Log = LogManager.GetLogger("UPLOAD BAKED TEXTURE");
        private readonly AssetServiceInterface m_AssetService;
        private readonly RwLockedDictionary<UUID, UUID> m_Transactions = new RwLockedDictionary<UUID, UUID>();

        public override string CapabilityName => "UploadBakedTexture";

        public override int ActiveUploads => m_Transactions.Count;

        public UploadBakedTexture(
            UUI creator,
            AssetServiceInterface assetService,
            string serverURI,
            string remoteip)
            : base(creator, serverURI, remoteip)
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
            if (m_Transactions.RemoveIf(transactionID, (UUID v) => true, out kvp))
            {
                var m = new Map();

                if (data.Type != NewAssetType)
                {
                    throw new UrlNotFoundException();
                }

                data.Name = "Baked Texture for Agent " + Creator.ID.ToString();
                try
                {
                    m_AssetService.Store(data);
                    m_Log.InfoFormat("Uploaded baked texture {1} for {0}", Creator.FullName, data.ID);
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

        protected override UUID NewAssetID => UUID.RandomFixedFirst(0xFFFFFFFF);

        protected override bool AssetIsLocal => true;

        protected override bool AssetIsTemporary => true;

        protected override AssetType NewAssetType => AssetType.Texture;
    }
}
