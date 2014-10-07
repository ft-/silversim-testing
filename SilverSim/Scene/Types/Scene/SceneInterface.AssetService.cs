using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.ServiceInterfaces.Asset;

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
                        return m_Scene.PersistentAssetService.Metadata[key];
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
                        return m_Scene.PersistentAssetService.References[key];
                    }
                }
            }
        }

        private class DefaultAssetService : AssetServiceInterface
        {
            SceneInterface m_Scene;
            DefaultAssetMetadataService m_MetadataService;
            DefaultAssetReferencesService m_ReferencesService;

            public DefaultAssetService(SceneInterface si)
            {
                m_Scene = si;
                m_MetadataService = new DefaultAssetMetadataService(si);
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
                        return m_Scene.PersistentAssetService[key];
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
                    m_Scene.PersistentAssetService.exists(key);
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
