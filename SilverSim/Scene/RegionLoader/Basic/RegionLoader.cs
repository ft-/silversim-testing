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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.Types.Grid;
using SilverSim.Types;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.RegionLoader;
using SilverSim.Scene.ServiceInterfaces.Scene;
using Nini.Config;
using System.Web;
using System.Xml;
using System.Net;
using log4net;
using System.Reflection;

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
            }
        }
        #endregion

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
                    r.ServerURI = string.Format("http://{0}:{1}/", m_ExternalHostName, m_HttpPort);
                    r.Size.X = ((uint)regionEntry.GetInt("SizeX", 256) + 255) & (~(uint)255);
                    r.Size.Y = ((uint)regionEntry.GetInt("SizeY", 256) + 255) & (~(uint)255);
                    r.Flags = RegionFlags.RegionOnline;
                    r.Owner.ID = regionEntry.GetString("OwnerID");
                    r.ScopeID = regionEntry.GetString("ScopeID", "00000000-0000-0000-0000-000000000000");
                    r.ServerHttpPort = m_HttpPort;
                    r.RegionMapTexture = regionEntry.GetString("MaptileStaticUUID", "00000000-0000-0000-0000-000000000000");
                    r.Access = (byte)regionEntry.GetInt("Access", 1);
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
