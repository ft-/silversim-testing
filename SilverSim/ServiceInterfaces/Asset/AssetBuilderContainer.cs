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