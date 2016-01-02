// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using Nini.Config;
using System.Net;
using ThreadedClasses;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types.Grid;
using System;
using System.ComponentModel;

namespace SilverSim.Scene.Registration
{
    #region Service Implementation
    [Description("Grid Registration Handler")]
    public class SceneRegistrar : IPlugin, IPluginShutdown
    {
        readonly RwLockedList<SceneInterface> m_RegisteredScenes = new RwLockedList<SceneInterface>();
        private BaseHttpServer m_HttpServer;

        public SceneRegistrar(IConfig ownSection)
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_HttpServer = loader.HttpServer;
            SceneManager.Scenes.OnRegionAdd += RegionAdded;
            SceneManager.Scenes.OnRegionRemove += RegionRemoved;

            /* register missing scenes */
            foreach(SceneInterface si in SceneManager.Scenes.Values)
            {
                if(!m_RegisteredScenes.Contains(si))
                {
                    RegionAdded(si);
                }
            }
        }

        public void RegionAdded(SceneInterface scene)
        {
            m_RegisteredScenes.Add(scene);
            scene.OnIPChanged += IPChanged;
            RegionInfo ri = scene.GetRegionInfo();
            ri.ServerHttpPort = m_HttpServer.Port;
            if(ri.Owner == null)
            {
                ri.Owner = new UUI();
            }
            ri.ServerURI =
                ((m_HttpServer.Port == 80 && m_HttpServer.Scheme == Uri.UriSchemeHttp) ||
                (m_HttpServer.Port == 443 && m_HttpServer.Scheme == Uri.UriSchemeHttps)) ?

                m_HttpServer.Scheme + "://" + m_HttpServer.ExternalHostName + "/" :

                m_HttpServer.Scheme + "://" + m_HttpServer.ExternalHostName + ":" + m_HttpServer.Port.ToString() + "/";

            ri.ServerHttpPort = m_HttpServer.Port;
            scene.ServerURI = ri.ServerURI;
            scene.GridService.RegisterRegion(ri);
        }

        public void IPChanged(SceneInterface scene, IPAddress address)
        {
            scene.GridService.RegisterRegion(scene.GetRegionInfo());
        }

        public void RegionRemoved(SceneInterface scene)
        {
            scene.GridService.UnregisterRegion(UUID.Zero, scene.ID);
            m_RegisteredScenes.Remove(scene);
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {

        }
    }
    #endregion

    #region Factory
    [PluginName("SceneRegistrar")]
    public class SceneRegistrarFactory : IPluginFactory
    {
        public SceneRegistrarFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new SceneRegistrar(ownSection);
        }
    }
    #endregion
}
