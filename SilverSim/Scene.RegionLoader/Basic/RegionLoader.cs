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
using System.ComponentModel;

namespace SilverSim.Scene.RegionLoader.Basic
{
    [Description("Region Loader")]
    [PluginName("Basic")]
    public class RegionLoaderService : IPlugin, IRegionLoaderInterface
    {
        private readonly string m_RegionStorage = string.Empty;
        private ExternalHostNameServiceInterface m_ExternalHostNameService;
        private GridServiceInterface m_RegionService;
        private SceneFactoryInterface m_SceneFactory;
        private static readonly ILog m_Log = LogManager.GetLogger("REGION LOADER");
        private SceneList m_Scenes;
        private string m_GatekeeperUri;

        #region Constructor
        public RegionLoaderService(IConfig ownConfig)
        {
            m_RegionStorage = ownConfig.GetString("RegionStorage");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_GatekeeperUri = loader.GatekeeperURI;
            m_Scenes = loader.Scenes;
            m_ExternalHostNameService = loader.ExternalHostNameService;
            m_SceneFactory = loader.GetService<SceneFactoryInterface>("DefaultSceneImplementation");
            m_RegionService = loader.GetService<GridServiceInterface>(m_RegionStorage);
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
                ri.GridURI = m_GatekeeperUri;
                if(string.IsNullOrEmpty(ri.ServerIP))
                {
                    ri.ServerIP = m_ExternalHostNameService.ExternalHostName;
                }
                SceneInterface si = m_SceneFactory.Instantiate(ri);
                m_Scenes.Add(si);
                si.LoadScene();
            }
        }
    }
}
