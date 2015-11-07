// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
        public class DefaultAssetMetadataService : AssetMetadataServiceInterface
        {
            readonly SceneInterface m_Scene;

            internal DefaultAssetMetadataService(SceneInterface scene)
            {
                m_Scene = scene;
            }

            [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
            public override AssetMetadata this[UUID key] 
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
        }

        public class DefaultAssetDataService : AssetDataServiceInterface
        {
            readonly SceneInterface m_Scene;

            internal DefaultAssetDataService(SceneInterface scene)
            {
                m_Scene = scene;
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
            public override Stream this[UUID key]
            {
                get
                {
                    try
                    {
                        return m_Scene.TemporaryAssetService.Data[key];
                    }
                    catch
                    {
                        try
                        {
                            return m_Scene.PersistentAssetService.Data[key];
                        }
                        catch
                        {
                            AssetData ad = ResourceAssets[key];
                            try
                            {
                                /* store these permanently */
                                ad.Local = false;
                                ad.Temporary = false;
                                m_Scene.PersistentAssetService.Store(ad);
                            }
                            catch
                            {

                            }
                            return new MemoryStream(ad.Data);
                        }
                    }
                }
            }
        }

        public class DefaultAssetReferencesService : AssetReferencesServiceInterface
        {
            readonly SceneInterface m_Scene;

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

        public class DefaultAssetService : AssetServiceInterface
        {
            readonly SceneInterface m_Scene;
            readonly DefaultAssetMetadataService m_MetadataService;
            readonly DefaultAssetDataService m_DataService;
            readonly DefaultAssetReferencesService m_ReferencesService;

            internal DefaultAssetService(SceneInterface si)
            {
                m_Scene = si;
                m_MetadataService = new DefaultAssetMetadataService(si);
                m_DataService = new DefaultAssetDataService(si);
                m_ReferencesService = new DefaultAssetReferencesService(si);
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

            [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
            public override AssetData this[UUID key]
            {
                get
                {
                    try
                    {
                        return m_Scene.TemporaryAssetService[key];
                    }
                    catch
                    {
                        try
                        {
                            return m_Scene.PersistentAssetService[key];
                        }
                        catch
                        {
                            AssetData ad = ResourceAssets[key];
                            try
                            {
                                /* store these permanently */
                                ad.Local = false;
                                ad.Temporary = false;
                                m_Scene.PersistentAssetService.Store(ad);
                            }
                            catch
                            {

                            }
                            return ad;
                        }
                    }
                }
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

                }
                try
                {
                    m_Scene.PersistentAssetService.Delete(id);
                    success = true;
                }
                catch
                {

                }
                if(!success)
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
