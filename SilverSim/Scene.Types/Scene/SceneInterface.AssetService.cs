// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Asset;
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Scene.Types.Scene
{
    public abstract partial class SceneInterface
    {
        private class DefaultAssetMetadataService : AssetMetadataServiceInterface
        {
            SceneInterface m_Scene;

            public DefaultAssetMetadataService(SceneInterface scene)
            {
                m_Scene = scene;
            }

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

        private class DefaultAssetDataService : AssetDataServiceInterface
        {
            SceneInterface m_Scene;

            public DefaultAssetDataService(SceneInterface scene)
            {
                m_Scene = scene;
            }

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

        private class DefaultAssetReferencesService : AssetReferencesServiceInterface
        {
            SceneInterface m_Scene;

            public DefaultAssetReferencesService(SceneInterface scene)
            {
                m_Scene = scene;
            }

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

        private class DefaultAssetService : AssetServiceInterface
        {
            SceneInterface m_Scene;
            DefaultAssetMetadataService m_MetadataService;
            DefaultAssetDataService m_DataService;
            DefaultAssetReferencesService m_ReferencesService;

            public DefaultAssetService(SceneInterface si)
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
                    throw new AssetNotFound(id);
                }
            }

            public override Dictionary<UUID, bool> exists(List<UUID> assets)
            {
                Dictionary<UUID, bool> asset1 = m_Scene.TemporaryAssetService.exists(assets);
                foreach(KeyValuePair<UUID, bool> kvp in m_Scene.PersistentAssetService.exists(assets))
                {
                    if(kvp.Value)
                    {
                        asset1[kvp.Key] = true;
                    }
                }
                foreach (KeyValuePair<UUID, bool> kvp in ResourceAssets.exists(assets))
                {
                    if(kvp.Value)
                    {
                        asset1[kvp.Key] = true;
                    }
                }
                return asset1;
            }

            public override void exists(UUID key)
            {
                try
                {
                    m_Scene.TemporaryAssetService.exists(key);
                }
                catch
                {
                    try
                    {
                        m_Scene.PersistentAssetService.exists(key);
                    }
                    catch
                    {
                        ResourceAssets.exists(key);
                    }
                }
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
