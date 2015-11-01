// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types.StructuredData.AssetXml;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ThreadedClasses;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        public class ResourceAssetAccessor : RwLockedDictionary<UUID, AssetData>
        {
            private List<string> m_Resources;
            internal ResourceAssetAccessor()
            {
                m_Resources = new List<string>(GetType().Assembly.GetManifestResourceNames());
            }

            public AssetData GetAsset(UUID key)
            {
                return this.GetOrAddIfNotExists(key, delegate()
                {
                    string resourcename = "SilverSim.Scene.Types.Resources.Assets." + key.ToString() + ".gz";
                    if(!m_Resources.Contains(resourcename))
                    {
                        throw new AssetNotFoundException(key);
                    }
                    using (Stream resource = GetType().Assembly.GetManifestResourceStream(resourcename))
                    {
                        using (GZipStream gz = new GZipStream(resource, CompressionMode.Decompress))
                        {
                            return AssetXml.ParseAssetData(gz);
                        }
                    }
                });
            }

            public bool Exists(UUID key)
            {
                string resourcename = "SilverSim.Scene.Types.Resources.Assets." + key.ToString() + ".gz";
                return m_Resources.Contains(resourcename);
            }
        }

        public class ResourceAssetMetadataService : AssetMetadataServiceInterface
        {
            ResourceAssetAccessor m_ResourceAssets;

            internal ResourceAssetMetadataService(ResourceAssetAccessor resourceAssets)
            {
                m_ResourceAssets = resourceAssets;
            }

            public override AssetMetadata this[UUID key]
            {
                get
                {
                    AssetData ad = m_ResourceAssets.GetAsset(key);
                    AssetMetadata md = new AssetMetadata();
                    md.AccessTime = ad.AccessTime;
                    md.CreateTime = ad.CreateTime;
                    md.Creator = ad.Creator;
                    md.Flags = ad.Flags;
                    md.ID = ad.ID;
                    md.Local = ad.Local;
                    md.Name = ad.Name;
                    md.Temporary = ad.Temporary;
                    md.Type = ad.Type;
                    return md;
                }
            }
        }

        public class ResourceAssetDataService : AssetDataServiceInterface
        {
            ResourceAssetAccessor m_ResourceAssets;

            internal ResourceAssetDataService(ResourceAssetAccessor resourceAssets)
            {
                m_ResourceAssets = resourceAssets;
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            public override Stream this[UUID key]
            {
                get
                {
                    AssetData ad = m_ResourceAssets.GetAsset(key);
                    return new MemoryStream(ad.Data);
                }
            }
        }

        public class ResourceAssetService : AssetServiceInterface
        {
            ResourceAssetAccessor m_ResourceAssets;
            ResourceAssetMetadataService m_MetadataService;
            ResourceAssetDataService m_DataService;
            SilverSim.ServiceInterfaces.Asset.DefaultAssetReferencesService m_ReferencesService;

            public ResourceAssetService()
            {
                m_ResourceAssets = new ResourceAssetAccessor();
                m_DataService = new ResourceAssetDataService(m_ResourceAssets);
                m_MetadataService = new ResourceAssetMetadataService(m_ResourceAssets);
                m_ReferencesService = new SilverSim.ServiceInterfaces.Asset.DefaultAssetReferencesService(this);
            }

            public override AssetMetadataServiceInterface Metadata
            {
                get
                {
                    return m_MetadataService;
                }
            }

            public override AssetReferencesServiceInterface References
            {
                get
                {
                    return m_ReferencesService;
                }
            }

            public override AssetDataServiceInterface Data
            {
                get
                {
                    return m_DataService;
                }
            }

            public override AssetData this[UUID key]
            {
                get
                {
                    return m_ResourceAssets.GetAsset(key);
                }
            }

            public override void Delete(UUID id)
            {
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
            }
        }

        private static ResourceAssetService ResourceAssets = new ResourceAssetService();
    }
}
