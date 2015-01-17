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

using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.Types;
using Nini.Config;
using System.Net;
using ThreadedClasses;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types.Grid;

namespace SilverSim.Scene.Registration
{
    #region Service Implementation
    public class SceneRegistrar : IPlugin, IPluginShutdown
    {
        private RwLockedList<SceneInterface> m_RegisteredScenes = new RwLockedList<SceneInterface>();
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
            RegionInfo ri = scene.RegionData;
            ri.ServerHttpPort = m_HttpServer.Port;
            if(ri.Owner == null)
            {
                ri.Owner = new UUI();
            }
            if (m_HttpServer.Port == 80)
            {
                ri.ServerURI = "http://" + m_HttpServer.ExternalHostName + "/";
            }
            else 
            {
                ri.ServerURI = "http://" + m_HttpServer.ExternalHostName + ":" + m_HttpServer.Port.ToString() + "/";
            }
            ri.ServerHttpPort = m_HttpServer.Port;
            scene.GridService.RegisterRegion(ri);
        }

        public void IPChanged(SceneInterface scene, IPAddress address)
        {
            scene.GridService.RegisterRegion(scene.RegionData);
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
