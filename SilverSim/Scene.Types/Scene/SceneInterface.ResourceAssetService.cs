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

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.StructuredData.AssetXml;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        public class ResourceAssetAccessor : RwLockedDictionary<UUID, AssetData>
        {
            private readonly List<string> m_Resources;
            internal ResourceAssetAccessor()
            {
                m_Resources = new List<string>(GetType().Assembly.GetManifestResourceNames());
            }

            private string GetAssetResourceName(UUID key) => "SilverSim.Scene.Types.Resources.Assets." + key.ToString() + ".gz";

            public bool ContainsAsset(UUID key) => m_Resources.Contains(GetAssetResourceName(key));

            public AssetData GetAsset(UUID key)
            {
                return GetOrAddIfNotExists(key, () =>
                {
                    string resourcename = GetAssetResourceName(key);
                    if (!m_Resources.Contains(resourcename))
                    {
                        throw new AssetNotFoundException(key);
                    }
                    using (var resource = GetType().Assembly.GetManifestResourceStream(resourcename))
                    {
                        using (var gz = new GZipStream(resource, CompressionMode.Decompress))
                        {
                            return AssetXml.ParseAssetData(gz);
                        }
                    }
                });
            }

            public List<UUID> GetKnownAssets()
            {
                UUID id;
                var list = new List<UUID>();
                const string SearchKey = "SilverSim.Scene.Types.Resources.Assets.";
                foreach (string res in m_Resources)
                {
                    if(res.StartsWith(SearchKey))
                    {
                        if(UUID.TryParse(res.Substring(SearchKey.Length, 36), out id))
                        {
                            list.Add(id);
                        }
                    }
                }
                return list;
            }

            public bool Exists(UUID key) => m_Resources.Contains(GetAssetResourceName(key));
        }

        public class ResourceAssetService : AssetServiceInterface, IAssetDataServiceInterface, IAssetMetadataServiceInterface
        {
            private readonly ResourceAssetAccessor m_ResourceAssets;
            private readonly ServiceInterfaces.Asset.DefaultAssetReferencesService m_ReferencesService;

            public ResourceAssetService()
            {
                m_ResourceAssets = new ResourceAssetAccessor();
                m_ReferencesService = new SilverSim.ServiceInterfaces.Asset.DefaultAssetReferencesService(this);
            }

            public List<UUID> GetKnownAssets() => m_ResourceAssets.GetKnownAssets();

            public override IAssetMetadataServiceInterface Metadata => this;

            AssetMetadata IAssetMetadataServiceInterface.this[UUID key]
            {
                get
                {
                    AssetData ad = m_ResourceAssets.GetAsset(key);
                    return new AssetMetadata()
                    {
                        AccessTime = ad.AccessTime,
                        CreateTime = ad.CreateTime,
                        Creator = ad.Creator,
                        Flags = ad.Flags,
                        ID = ad.ID,
                        Local = ad.Local,
                        Name = ad.Name,
                        Temporary = ad.Temporary,
                        Type = ad.Type
                    };
                }
            }

            bool IAssetMetadataServiceInterface.TryGetValue(UUID key, out AssetMetadata metadata)
            {
                if (!m_ResourceAssets.ContainsAsset(key))
                {
                    metadata = null;
                    return false;
                }
                metadata = this[key];
                return true;
            }

            public override AssetReferencesServiceInterface References => m_ReferencesService;

            public override IAssetDataServiceInterface Data => this;

            Stream IAssetDataServiceInterface.this[UUID key] => new MemoryStream(m_ResourceAssets.GetAsset(key).Data);

            bool IAssetDataServiceInterface.TryGetValue(UUID key, out Stream s)
            {
                if (!m_ResourceAssets.ContainsAsset(key))
                {
                    s = null;
                    return false;
                }
                s = Data[key];
                return true;
            }

            public override AssetData this[UUID key] => m_ResourceAssets.GetAsset(key);

            public override bool TryGetValue(UUID key, out AssetData assetData)
            {
                if(!m_ResourceAssets.ContainsAsset(key))
                {
                    assetData = null;
                    return false;
                }
                assetData = this[key];
                return true;
            }

            public override void Delete(UUID id)
            {
                /* intentionally left empty */
            }

            public override Dictionary<UUID, bool> Exists(List<UUID> assets)
            {
                Dictionary<UUID, bool> asset1 = new Dictionary<UUID, bool>();
                foreach (UUID key in assets)
                {
                    asset1[key] = m_ResourceAssets.Exists(key);
                }
                return asset1;
            }

            public override bool Exists(UUID key)
            {
                return m_ResourceAssets.Exists(key);
            }

            public override void Store(AssetData asset)
            {
                /* intentionally left empty */
            }
        }

        private static readonly ResourceAssetService ResourceAssets = new ResourceAssetService();
    }
}
