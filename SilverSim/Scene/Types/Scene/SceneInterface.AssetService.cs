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
                            return ResourceAssets.Metadata[key];
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
                        try
                        {
                            return m_Scene.PersistentAssetService[key];
                        }
                        catch
                        {
                            return ResourceAssets[key];
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
