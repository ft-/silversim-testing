// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

namespace SilverSim.Scene.Registration
{
    #region Service Implementation
    [Description("Grid Registration Handler")]
    public class SceneRegistrar : IPlugin
    {
        readonly RwLockedList<SceneInterface> m_RegisteredScenes = new RwLockedList<SceneInterface>();
        private BaseHttpServer m_HttpServer;
        SceneList m_Scenes;

        public SceneRegistrar(IConfig ownSection)
        {
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_HttpServer = loader.HttpServer;
            m_Scenes.OnRegionAdd += RegionAdded;
            m_Scenes.OnRegionRemove += RegionRemoved;

            /* register missing scenes */
            foreach(SceneInterface si in m_Scenes.Values)
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
            RegionInfo ri = scene.GetRegionInfo();
            if(ri.Owner == null)
            {
                ri.Owner = new UUI();
            }

            Dictionary<string, string> gridFeatures = scene.GridService.GetGridExtraFeatures();
            if (gridFeatures.ContainsKey("GridURL"))
            {
                ri.GridURI = gridFeatures["GridURL"];
            }
            scene.GridService.RegisterRegion(ri);
        }

        public void RegionRemoved(SceneInterface scene)
        {
            scene.GridService.UnregisterRegion(UUID.Zero, scene.ID);
            m_RegisteredScenes.Remove(scene);
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
