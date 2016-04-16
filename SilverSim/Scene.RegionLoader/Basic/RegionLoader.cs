// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.RegionLoader;
using SilverSim.Scene.ServiceInterfaces.Scene;
using SilverSim.Scene.Types.Scene;
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
        private string m_ExternalHostName = string.Empty;
        private GridServiceInterface m_RegionService;
        private SceneFactoryInterface m_SceneFactory;
        private uint m_HttpPort;
        private static readonly ILog m_Log = LogManager.GetLogger("REGION LOADER");
        private string m_Scheme = Uri.UriSchemeHttp;
        SceneList m_Scenes;

        #region Constructor
        internal RegionLoaderService(string regionStorage)
        {
            m_RegionStorage = regionStorage;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            IConfig config = loader.Config.Configs["Network"];
            m_SceneFactory = loader.GetService<SceneFactoryInterface>("DefaultSceneImplementation");
            m_RegionService = loader.GetService<GridServiceInterface>(m_RegionStorage);
            if (config != null)
            {
                m_ExternalHostName = config.GetString("ExternalHostName", "SYSTEMIP");
                m_HttpPort = (uint)config.GetInt("HttpListenerPort", 9000);

                if (config.Contains("ServerCertificate"))
                {
                    m_Scheme = Uri.UriSchemeHttps;
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
                SceneInterface si = m_SceneFactory.Instantiate(ri);
                m_Scenes.Add(si);
                si.LoadSceneAsync();
            }
        }
    }
}
