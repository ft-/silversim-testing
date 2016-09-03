// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.RegionLoader;
using SilverSim.Scene.ServiceInterfaces.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types.Grid;
using System;
using System.ComponentModel;

namespace SilverSim.Scene.RegionLoader.Basic
{
    [Description("Region Loader")]
    public class RegionLoaderService : IPlugin, IRegionLoaderInterface
    {
        readonly string m_RegionStorage = string.Empty;
        ExternalHostNameServiceInterface m_ExternalHostNameService;
        private GridServiceInterface m_RegionService;
        private SceneFactoryInterface m_SceneFactory;
        private static readonly ILog m_Log = LogManager.GetLogger("REGION LOADER");
        SceneList m_Scenes;
        string m_GatekeeperURI;

        #region Constructor
        internal RegionLoaderService(string regionStorage)
        {
            m_RegionStorage = regionStorage;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_ExternalHostNameService = loader.ExternalHostNameService;
            IConfig config = loader.Config.Configs["Network"];
            m_SceneFactory = loader.GetService<SceneFactoryInterface>("DefaultSceneImplementation");
            m_RegionService = loader.GetService<GridServiceInterface>(m_RegionStorage);

            config = loader.Config.Configs["Startup"];
            if (config != null)
            {
                m_GatekeeperURI = config.GetString("GatekeeperURI", "");
                if (m_GatekeeperURI.Length != 0 && !m_GatekeeperURI.EndsWith("/"))
                {
                    m_GatekeeperURI += "/";
                }
            }
        }
        #endregion

        public void AllRegionsLoaded()
        {
            /* intentionally left empty */
        }

        public void LoadRegions()
        {
            foreach(RegionInfo ri in m_RegionService.GetOnlineRegions())
            {
                m_Log.InfoFormat("Starting Region {0}", ri.Name);
                ri.GridURI = m_GatekeeperURI;
                if(string.IsNullOrEmpty(ri.ServerIP))
                {
                    ri.ServerIP = m_ExternalHostNameService.ExternalHostName;
                }
                SceneInterface si = m_SceneFactory.Instantiate(ri);
                m_Scenes.Add(si);
                si.LoadSceneAsync();
            }
        }
    }
}
