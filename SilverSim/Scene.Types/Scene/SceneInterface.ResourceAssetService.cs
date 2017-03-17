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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;

namespace SilverSim.Scene.Types.Scene
{
    public partial class SceneInterface
    {
        public class ResourceAssetAccessor : RwLockedDictionary<UUID, AssetData>
        {
            readonly List<string> m_Resources;
            internal ResourceAssetAccessor()
            {
                m_Resources = new List<string>(GetType().Assembly.GetManifestResourceNames());
            }

            public bool ContainsAsset(UUID key)
            {
                string resourcename = "SilverSim.Scene.Types.Resources.Assets." + key.ToString() + ".gz";
                return m_Resources.Contains(resourcename);
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

        public class ResourceAssetService : AssetServiceInterface, IAssetDataServiceInterface, IAssetMetadataServiceInterface
        {
            readonly ResourceAssetAccessor m_ResourceAssets;
            readonly ServiceInterfaces.Asset.DefaultAssetReferencesService m_ReferencesService;

            public ResourceAssetService()
            {
                m_ResourceAssets = new ResourceAssetAccessor();
                m_ReferencesService = new SilverSim.ServiceInterfaces.Asset.DefaultAssetReferencesService(this);
            }

            public override IAssetMetadataServiceInterface Metadata
            {
                get
                {
                    return this;
                }
            }

            AssetMetadata IAssetMetadataServiceInterface.this[UUID key]
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
            public override AssetReferencesServiceInterface References
            {
                get
                {
                    return m_ReferencesService;
                }
            }

            public override IAssetDataServiceInterface Data
            {
                get
                {
                    return this;
                }
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            Stream IAssetDataServiceInterface.this[UUID key]
            {
                get
                {
                    AssetData ad = m_ResourceAssets.GetAsset(key);
                    return new MemoryStream(ad.Data);
                }
            }

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

            public override AssetData this[UUID key]
            {
                get
                {
                    return m_ResourceAssets.GetAsset(key);
                }
            }

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

        private static ResourceAssetService ResourceAssets = new ResourceAssetService();
    }
}
