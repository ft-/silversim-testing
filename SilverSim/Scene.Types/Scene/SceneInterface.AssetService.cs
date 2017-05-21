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
using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        public class DefaultAssetReferencesService : AssetReferencesServiceInterface
        {
            private readonly SceneInterface m_Scene;

            internal DefaultAssetReferencesService(SceneInterface scene)
            {
                m_Scene = scene;
            }

            [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
            public override List<UUID> this[UUID key]
            {
                get
                {
                    try
                    {
                        return m_Scene.TemporaryAssetService.References[key];
                    }
                    catch
                    {
                        try
                        {
                            return m_Scene.PersistentAssetService.References[key];
                        }
                        catch
                        {
                            return ResourceAssets.References[key];
                        }
                    }
                }
            }
        }

        public class DefaultAssetService : AssetServiceInterface, IAssetMetadataServiceInterface, IAssetDataServiceInterface
        {
            private readonly SceneInterface m_Scene;
            private readonly DefaultAssetReferencesService m_ReferencesService;

            internal DefaultAssetService(SceneInterface si)
            {
                m_Scene = si;
                m_ReferencesService = new DefaultAssetReferencesService(si);
            }

            public override IAssetMetadataServiceInterface Metadata => this;

            [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
            AssetMetadata IAssetMetadataServiceInterface.this[UUID key]
            {
                get
                {
                    try
                    {
                        return m_Scene.TemporaryAssetService.Metadata[key];
                    }
                    catch
                    {
                        try
                        {
                            return m_Scene.PersistentAssetService.Metadata[key];
                        }
                        catch
                        {
                            AssetMetadata md = ResourceAssets.Metadata[key];
                            md.Temporary = false;
                            return md;
                        }
                    }
                }
            }

            bool IAssetMetadataServiceInterface.TryGetValue(UUID key, out AssetMetadata metadata)
            {
                if (m_Scene.TemporaryAssetService.Metadata.TryGetValue(key, out metadata))
                {
                    return true;
                }

                if (m_Scene.PersistentAssetService.Metadata.TryGetValue(key, out metadata))
                {
                    return true;
                }

                if (ResourceAssets.Metadata.TryGetValue(key, out metadata))
                {
                    return true;
                }
                return false;
            }

            public override AssetReferencesServiceInterface References => m_ReferencesService;

            public override IAssetDataServiceInterface Data => this;

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
            Stream IAssetDataServiceInterface.this[UUID key]
            {
                get
                {
                    Stream s;
                    if (!Data.TryGetValue(key, out s))
                    {
                        throw new AssetNotFoundException(key);
                    }
                    return s;
                }
            }

            bool IAssetDataServiceInterface.TryGetValue(UUID key, out Stream s)
            {
                if (m_Scene.TemporaryAssetService.Data.TryGetValue(key, out s))
                {
                    return true;
                }
                if (m_Scene.PersistentAssetService.Data.TryGetValue(key, out s))
                {
                    return true;
                }
                AssetData ad;
                if (ResourceAssets.TryGetValue(key, out ad))
                {
                    ad.Local = false;
                    ad.Temporary = false;
                    m_Scene.PersistentAssetService.Store(ad);
                    s = new MemoryStream(ad.Data);
                    return true;
                }
                return false;
            }

            [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
            public override AssetData this[UUID key]
            {
                get
                {
                    AssetData ad;
                    if(!TryGetValue(key, out ad))
                    {
                        throw new AssetNotFoundException(key);
                    }
                    return ad;
                }
            }

            public override bool TryGetValue(UUID key, out AssetData assetData)
            {
                if(m_Scene.TemporaryAssetService.TryGetValue(key, out assetData))
                {
                    return true;
                }

                if(m_Scene.PersistentAssetService.TryGetValue(key, out assetData))
                {
                    return true;
                }

                if(ResourceAssets.TryGetValue(key, out assetData))
                {
                    assetData.Local = false;
                    assetData.Temporary = false;
                    m_Scene.PersistentAssetService.Store(assetData);
                    return true;
                }

                return false;
            }

            [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
            public override void Delete(UUID id)
            {
                bool success = false;
                try
                {
                    m_Scene.TemporaryAssetService.Delete(id);
                    success = true;
                }
                catch
                {
                    /* no action required */
                }
                try
                {
                    m_Scene.PersistentAssetService.Delete(id);
                    success = true;
                }
                catch
                {
                    /* no action required */
                }
                if (!success)
                {
                    throw new AssetNotFoundException(id);
                }
            }

            public override Dictionary<UUID, bool> Exists(List<UUID> assets)
            {
                Dictionary<UUID, bool> asset1 = m_Scene.TemporaryAssetService.Exists(assets);
                foreach(KeyValuePair<UUID, bool> kvp in m_Scene.PersistentAssetService.Exists(assets))
                {
                    if(kvp.Value)
                    {
                        asset1[kvp.Key] = true;
                    }
                }
                foreach (KeyValuePair<UUID, bool> kvp in ResourceAssets.Exists(assets))
                {
                    if(kvp.Value)
                    {
                        asset1[kvp.Key] = true;
                    }
                }
                return asset1;
            }

            public override bool Exists(UUID key)
            {
                if(m_Scene.TemporaryAssetService.Exists(key))
                {
                    return true;
                }
                if(m_Scene.PersistentAssetService.Exists(key))
                {
                    return true;
                }
                return ResourceAssets.Exists(key);
            }

            public override void Store(AssetData asset)
            {
                if(asset.Temporary)
                {
                    m_Scene.TemporaryAssetService.Store(asset);
                }
                else
                {
                    m_Scene.PersistentAssetService.Store(asset);
                }
            }
        }
    }
}
