// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.ServiceInterfaces.Asset
{
    public class AssetBuilderContainer : AssetServiceInterface, IAssetMetadataServiceInterface, IAssetDataServiceInterface
    {
        readonly RwLockedDictionary<UUID, AssetData> m_Assets = new RwLockedDictionary<UUID, AssetData>();
        readonly DefaultAssetReferencesService m_DefReferences;

        public AssetBuilderContainer()
        {
            m_DefReferences = new DefaultAssetReferencesService(this);
        }

        #region AssetServiceInterface
        public override AssetData this[UUID key]
        {
            get
            {
                return m_Assets[key];
            }
        }

        Stream IAssetDataServiceInterface.this[UUID key]
        {
            get
            {
                return m_Assets[key].InputStream;
            }
        }

        AssetMetadata IAssetMetadataServiceInterface.this[UUID key]
        {
            get
            {
                return m_Assets[key];
            }
        }

        public override IAssetDataServiceInterface Data
        {
            get
            {
                return this;
            }
        }

        public override IAssetMetadataServiceInterface Metadata
        {
            get
            {
                return this;
            }
        }

        public override AssetReferencesServiceInterface References
        {
            get
            {
                return m_DefReferences;
            }
        }

        public override void Delete(UUID id)
        {
            throw new NotSupportedException();
        }

        public override Dictionary<UUID, bool> Exists(List<UUID> assets)
        {
            Dictionary<UUID, bool> v = new Dictionary<UUID, bool>();
            foreach(UUID k in assets)
            {
                v[k] = m_Assets.ContainsKey(k);
            }
            return v;
        }

        public override bool Exists(UUID key)
        {
            return m_Assets.ContainsKey(key);
        }

        public override void Store(AssetData asset)
        {
            m_Assets.Add(asset.ID, asset);
        }

        public override bool TryGetValue(UUID key, out AssetData assetData)
        {
            return m_Assets.TryGetValue(key, out assetData);
        }

        bool IAssetDataServiceInterface.TryGetValue(UUID key, out Stream s)
        {
            AssetData d;
            s = default(Stream);
            if(m_Assets.TryGetValue(key, out d))
            {
                s = d.InputStream;
                return true;
            }
            return false;
        }

        bool IAssetMetadataServiceInterface.TryGetValue(UUID key, out AssetMetadata metadata)
        {
            AssetData d;
            bool res = m_Assets.TryGetValue(key, out d);
            metadata = d;
            return res;
        }
        #endregion

    }
}