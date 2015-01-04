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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.ServiceInterfaces.Scene;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Types.Grid;
using System.Collections.Generic;

namespace SilverSim.Scene.Implementation.Basic
{
    class SceneFactory : SceneFactoryInterface, IPlugin
    {
        ChatServiceFactoryInterface m_ChatFactory;
        string m_ChatFactoryName;
        string m_GroupsNameServiceName;
        string m_AssetServiceName;
        string m_AssetCacheServiceName;
        string m_GridServiceName;
        string m_IMServiceName;
        string m_SimulationDataStorageName;
        List<string> m_AvatarNameServiceNames = new List<string>();

        GroupsNameServiceInterface m_GroupsNameService = null;
        AssetServiceInterface m_AssetService;
        AssetServiceInterface m_AssetCacheService;
        GridServiceInterface m_GridService;
        ServerParamServiceInterface m_ServerParamService;
        IMServiceInterface m_IMService;
        SimulationDataStorageInterface m_SimulationDataStorage;
        Dictionary<string, string> m_CapabilitiesConfig;
        List<AvatarNameServiceInterface> m_AvatarNameServices = new List<AvatarNameServiceInterface>();

        public SceneFactory(IConfig ownConfig)
        {
            m_ChatFactoryName = ownConfig.GetString("ChatService", "Chat");
            m_GroupsNameServiceName = ownConfig.GetString("GroupsNameService", "");
            m_AssetServiceName = ownConfig.GetString("AssetService", "AssetService");
            m_AssetCacheServiceName = ownConfig.GetString("AssetCacheService", m_AssetServiceName);
            m_GridServiceName = ownConfig.GetString("GridService", "GridService");
            m_IMServiceName = ownConfig.GetString("IMService", "IMService");
            m_SimulationDataStorageName = ownConfig.GetString("SimulationDataStorage", "SimulationDataStorage");
            string avatarNameServices = ownConfig.GetString("AvatarNameServices", "");
            foreach(string p in avatarNameServices.Split(','))
            {
                m_AvatarNameServiceNames.Add(p.Trim());
            }

            m_CapabilitiesConfig = new Dictionary<string, string>();
            foreach(string key in ownConfig.GetKeys())
            {
                if(key.StartsWith("Cap_") && key != "Cap_")
                {
                    m_CapabilitiesConfig[key.Substring(4)] = ownConfig.GetString(key);
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_ChatFactory = loader.GetService<ChatServiceFactoryInterface>(m_ChatFactoryName);
            if (m_GroupsNameServiceName != "")
            {
                m_GroupsNameService = loader.GetService<GroupsNameServiceInterface>(m_GroupsNameServiceName);
            }
            m_AssetService = loader.GetService<AssetServiceInterface>(m_AssetServiceName);
            m_AssetCacheService = loader.GetService<AssetServiceInterface>(m_AssetCacheServiceName);
            m_GridService = loader.GetService<GridServiceInterface>(m_GridServiceName);
            m_IMService = loader.GetService<IMServiceInterface>(m_IMServiceName);
            m_ServerParamService = loader.GetService<ServerParamServiceInterface>("ServerParamStorage");
            m_SimulationDataStorage = loader.GetService<SimulationDataStorageInterface>(m_SimulationDataStorageName);
            foreach(string servicename in m_AvatarNameServiceNames)
            {
                m_AvatarNameServices.Add(loader.GetService<AvatarNameServiceInterface>(servicename));
            }
        }

        public override SceneInterface Instantiate(RegionInfo ri)
        {
            return new BasicScene(m_ChatFactory.Instantiate(), 
                m_IMService, 
                m_GroupsNameService, 
                m_AssetService,
                m_AssetCacheService,
                m_GridService,
                m_ServerParamService,
                ri,
                m_AvatarNameServices,
                m_SimulationDataStorage,
                m_CapabilitiesConfig);
        }
    }

    public class Factory : IPluginFactory
    {
        public Factory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownConfig)
        {
            return new SceneFactory(ownConfig);
        }
    }
}
