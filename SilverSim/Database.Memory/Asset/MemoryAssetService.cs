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

using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces._Combined;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace SilverSim.Database.Memory.Asset
{
    [Description("Memory Asset Backend")]
    [PluginName("Assets")]
    public class MemoryAssetService : AssetServiceCombinedInterface, IPlugin
    {
        private readonly DefaultAssetReferencesService m_ReferencesService;
        private readonly RwLockedDictionary<UUID, AssetData> m_Assets = new RwLockedDictionary<UUID, AssetData>();

        #region Constructor
        public MemoryAssetService()
        {
            m_ReferencesService = new DefaultAssetReferencesService(this);
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
        #endregion

        #region Exists methods
        public override bool Exists(UUID key) =>
            m_Assets.ContainsKey(key);

        public override Dictionary<UUID, bool> Exists(List<UUID> assets)
        {
            var res = new Dictionary<UUID,bool>();
            foreach(UUID id in assets)
            {
                res[id] = m_Assets.ContainsKey(id);
            }

            return res;
        }

        #endregion

        public override bool IsSameServer(AssetServiceInterface other) =>
            other.GetType() == typeof(MemoryAssetService) && other == this;

        #region Accessors
        public override bool TryGetValue(UUID key, out AssetData asset)
        {
            AssetData internalAsset;
            if(m_Assets.TryGetValue(key, out internalAsset))
            {
                internalAsset.AccessTime = Date.Now;
                asset = new AssetData(internalAsset);
                return true;
            }
            asset = null;
            return false;
        }

        #endregion

        #region Metadata interface
        public override bool TryGetValue(UUID key, out AssetMetadata metadata)
        {
            AssetData data;
            if (m_Assets.TryGetValue(key, out data))
            {
                data.AccessTime = Date.Now;
                metadata = new AssetMetadata(data);
                return true;
            }
            else
            {
                metadata = null;
                return false;
            }
        }

        #endregion

        #region References interface
        public override AssetReferencesServiceInterface References => m_ReferencesService;
        #endregion

        #region Data interface
        public override bool TryGetValue(UUID key, out Stream s)
        {
            AssetData data;
            if (m_Assets.TryGetValue(key, out data))
            {
                data.AccessTime = Date.Now;
                s = data.InputStream;
                return true;
            }
            else
            {
                s = null;
                return false;
            }
        }
        #endregion

        #region Store asset method
        public override void Store(AssetData asset)
        {
            AssetData internalAsset;
            if(m_Assets.TryGetValue(asset.ID, out internalAsset))
            {
                if(internalAsset.Flags != AssetFlags.Normal)
                {
                    internalAsset = new AssetData(asset);
                    m_Assets[internalAsset.ID] = internalAsset;
                }
            }
            else
            {
                internalAsset = new AssetData(asset);
                m_Assets.Add(internalAsset.ID, internalAsset);
            }
        }
        #endregion

        #region Delete asset method
        public override void Delete(UUID id)
        {
            m_Assets.RemoveIf(id, (AssetData d) => d.Flags != AssetFlags.Normal);
        }
        #endregion

        private const int MAX_ASSET_NAME = 64;
    }
}
