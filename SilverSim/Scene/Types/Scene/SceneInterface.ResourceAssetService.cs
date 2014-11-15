/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

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
                    md.Description = ad.Description;
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

        private class ResourceAssetService : AssetServiceInterface
        {
            ResourceAssetAccessor m_ResourceAssets;
            ResourceAssetMetadataService m_MetadataService;
            SilverSim.ServiceInterfaces.Asset.DefaultAssetReferencesService m_ReferencesService;

            public ResourceAssetService()
            {
                m_ResourceAssets = new ResourceAssetAccessor();
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
