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
using SilverSim.Types;
using SilverSim.Types.Grid;
using System;
using System.Xml;

namespace SilverSim.Scene.RegionLoader.Basic
{
    class RegionLoaderService : IPlugin, IRegionLoaderInterface
    {
        private string m_RegionStorage;
        private string m_RegionCfg;
        private string m_ExternalHostName = string.Empty;
        private GridServiceInterface m_RegionService;
        private SceneFactoryInterface m_SceneFactory;
        private uint m_HttpPort = 0;
        private static readonly ILog m_Log = LogManager.GetLogger("REGION LOADER");
        private string m_Scheme = Uri.UriSchemeHttp;

        #region Constructor
        public RegionLoaderService(string regionStorage, string regionCfg)
        {
            m_RegionStorage = regionStorage;
            m_RegionCfg = regionCfg;
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_SceneFactory = loader.GetService<SceneFactoryInterface>("DefaultSceneImplementation");
            m_RegionService = loader.GetService<GridServiceInterface>(m_RegionStorage);
            if(loader.Config.Configs["Network"] != null)
            {
                m_ExternalHostName = loader.Config.Configs["Network"].GetString("ExternalHostName", "SYSTEMIP");
                m_HttpPort = (uint)loader.Config.Configs["Network"].GetInt("HttpListenerPort", 9000);

                if(loader.Config.Configs["Network"].Contains("ServerCertificate"))
                {
                    m_Scheme = Uri.UriSchemeHttps;
                }
            }
        }
        #endregion

        public void AllRegionsLoaded()
        {

        }

        public void LoadRegions()
        {
            if (!string.IsNullOrEmpty(m_RegionCfg))
            {
                IConfigSource cfg;
                if (Uri.IsWellFormedUriString(m_RegionCfg, UriKind.Absolute))
                {
                    XmlReader r = XmlReader.Create(m_RegionCfg);
                    cfg = new XmlConfigSource(r);
                }
                else
                {
                    cfg = new IniConfigSource(m_RegionCfg);
                }

                foreach (IConfig regionEntry in cfg.Configs)
                {
                    RegionInfo r = new RegionInfo();
                    r.Name = regionEntry.Name;
                    r.ID = regionEntry.GetString("RegionUUID");
                    r.Location = new GridVector(regionEntry.GetString("Location"), 256);
                    r.ServerPort = (uint)regionEntry.GetInt("InternalPort");
                    r.ServerURI = string.Format("{0}://{1}:{2}/", m_Scheme, m_ExternalHostName, m_HttpPort);
                    r.Size.X = ((uint)regionEntry.GetInt("SizeX", 256) + 255) & (~(uint)255);
                    r.Size.Y = ((uint)regionEntry.GetInt("SizeY", 256) + 255) & (~(uint)255);
                    r.Flags = RegionFlags.RegionOnline;
                    r.Owner = new UUI(regionEntry.GetString("Owner"));
                    r.ScopeID = regionEntry.GetString("ScopeID", "00000000-0000-0000-0000-000000000000");
                    r.ServerHttpPort = m_HttpPort;
                    r.RegionMapTexture = regionEntry.GetString("MaptileStaticUUID", "00000000-0000-0000-0000-000000000000");
                    r.Access = (RegionAccess)(byte)regionEntry.GetInt("Access", 1);
                    r.ServerIP = regionEntry.GetString("ExternalHostName", m_ExternalHostName);
                    m_RegionService.RegisterRegion(r);
                }
            }

            foreach(RegionInfo ri in m_RegionService.GetOnlineRegions())
            {
                m_Log.InfoFormat("Starting Region {0}", ri.Name);
                SceneInterface si = m_SceneFactory.Instantiate(ri);
                SceneManager.Scenes.Add(si);
                si.LoadSceneAsync();
            }
        }
    }
}
