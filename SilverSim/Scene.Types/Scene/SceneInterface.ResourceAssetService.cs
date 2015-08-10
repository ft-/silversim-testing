// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;
using ThreadedClasses;
using System.Reflection;
using System.IO;
using System.IO.Compression;
using SilverSim.StructuredData.AssetXml;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        private class ResourceAssetAccessor : RwLockedDictionary<UUID, AssetData>
        {
            private List<string> m_Resources;
            public ResourceAssetAccessor()
            {
                m_Resources = new List<string>(GetType().Assembly.GetManifestResourceNames());
            }

            public AssetData getAsset(UUID key)
            {
                return this.GetOrAddIfNotExists(key, delegate()
                {
                    string resourcename = "SilverSim.Scene.Types.Resources.Assets." + key + ".gz";
                    if(!m_Resources.Contains(resourcename))
                    {
                        throw new AssetNotFound(key);
                    }
                    Stream resource = GetType().Assembly.GetManifestResourceStream(resourcename);
                    using(GZipStream gz = new GZipStream(resource, CompressionMode.Decompress))
                    {
                        return AssetXml.parseAssetData(gz);
                    }
                });
            }
        }

        private class ResourceAssetMetadataService : AssetMetadataServiceInterface
        {
            ResourceAssetAccessor m_ResourceAssets;

            public ResourceAssetMetadataService(ResourceAssetAccessor resourceAssets)
            {
                m_ResourceAssets = resourceAssets;
            }

            public override AssetMetadata this[UUID key]
            {
                get
                {
                    AssetData ad = m_ResourceAssets.getAsset(key);
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

        private class ResourceAssetDataService : AssetDataServiceInterface
        {
            ResourceAssetAccessor m_ResourceAssets;

            public ResourceAssetDataService(ResourceAssetAccessor resourceAssets)
            {
                m_ResourceAssets = resourceAssets;
            }

            public override Stream this[UUID key]
            {
                get
                {
                    AssetData ad = m_ResourceAssets.getAsset(key);
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
                    return m_ResourceAssets.getAsset(key);
                }
            }

            public override void Delete(UUID id)
            {
            }

            public override Dictionary<UUID, bool> exists(List<UUID> assets)
            {
                Dictionary<UUID, bool> asset1 = new Dictionary<UUID, bool>();
                foreach (UUID key in assets)
                {
                    try
                    {
                        AssetData ad = m_ResourceAssets.getAsset(key);
                        asset1[key] = true;
                    }
                    catch
                    {
                        asset1[key] = false;
                    }
                }
                return asset1;
            }

            public override void exists(UUID key)
            {
                AssetData ad = m_ResourceAssets.getAsset(key);
            }

            public override void Store(AssetData asset)
            {
            }
        }

        private static ResourceAssetService ResourceAssets = new ResourceAssetService();
    }
}
