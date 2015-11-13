// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.ServiceInterfaces.Scene;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Neighbor;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types.Grid;
using System.Collections.Generic;

namespace SilverSim.Scene.Implementation.Basic
{
    public sealed class SceneFactory : SceneFactoryInterface, IPlugin
    {
        ChatServiceFactoryInterface m_ChatFactory;
        readonly string m_ChatFactoryName;
        readonly string m_GroupsNameServiceName;
        readonly string m_GroupsServiceName;
        readonly string m_AssetServiceName;
        readonly string m_AssetCacheServiceName;
        readonly string m_GridServiceName;
        readonly string m_IMServiceName;
        readonly string m_EstateServiceName;
        readonly string m_SimulationDataStorageName;
        readonly string m_PhysicsName;
        readonly string m_NeighborServiceName;
        readonly List<string> m_AvatarNameServiceNames = new List<string>();

        GroupsNameServiceInterface m_GroupsNameService;
        AssetServiceInterface m_AssetService;
        AssetServiceInterface m_AssetCacheService;
        GridServiceInterface m_GridService;
        GroupsServiceInterface m_GroupsService;
        ServerParamServiceInterface m_ServerParamService;
        IMServiceInterface m_IMService;
        EstateServiceInterface m_EstateService;
        SimulationDataStorageInterface m_SimulationDataStorage;
        readonly Dictionary<string, string> m_CapabilitiesConfig;
        IPhysicsSceneFactory m_PhysicsFactory;
        NeighborServiceInterface m_NeighborService;
        readonly List<AvatarNameServiceInterface> m_AvatarNameServices = new List<AvatarNameServiceInterface>();

        public SceneFactory(IConfig ownConfig)
        {
            m_ChatFactoryName = ownConfig.GetString("ChatService", "Chat");
            m_GroupsNameServiceName = ownConfig.GetString("GroupsNameService", string.Empty);
            m_GroupsServiceName = ownConfig.GetString("GroupsService", string.Empty);
            m_AssetServiceName = ownConfig.GetString("AssetService", "AssetService");
            m_AssetCacheServiceName = ownConfig.GetString("AssetCacheService", m_AssetServiceName);
            m_GridServiceName = ownConfig.GetString("GridService", "GridService");
            m_IMServiceName = ownConfig.GetString("IMService", "IMService");
            m_SimulationDataStorageName = ownConfig.GetString("SimulationDataStorage", "SimulationDataStorage");
            m_EstateServiceName = ownConfig.GetString("EstateService", "EstateService");
            m_PhysicsName = ownConfig.GetString("Physics", string.Empty);
            m_NeighborServiceName = ownConfig.GetString("NeighborService", "NeighborService");
            string avatarNameServices = ownConfig.GetString("AvatarNameServices", string.Empty);
            if (!string.IsNullOrEmpty(avatarNameServices))
            {
                foreach (string p in avatarNameServices.Split(','))
                {
                    m_AvatarNameServiceNames.Add(p.Trim());
                }
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
            if (m_GroupsNameServiceName.Length != 0)
            {
                m_GroupsNameService = loader.GetService<GroupsNameServiceInterface>(m_GroupsNameServiceName);
            }
            if (m_GroupsServiceName.Length != 0)
            {
                m_GroupsService = loader.GetService<GroupsServiceInterface>(m_GroupsServiceName);
            }
            if (m_PhysicsName.Length != 0)
            {
                m_PhysicsFactory = loader.GetService<IPhysicsSceneFactory>(m_PhysicsName);
            }
            if (m_NeighborServiceName.Length != 0)
            {
                m_NeighborService = loader.GetService<NeighborServiceInterface>(m_NeighborServiceName);
            }
            m_AssetService = loader.GetService<AssetServiceInterface>(m_AssetServiceName);
            m_AssetCacheService = loader.GetService<AssetServiceInterface>(m_AssetCacheServiceName);
            m_GridService = loader.GetService<GridServiceInterface>(m_GridServiceName);
            m_IMService = loader.GetService<IMServiceInterface>(m_IMServiceName);
            m_ServerParamService = loader.GetService<ServerParamServiceInterface>("ServerParamStorage");
            m_SimulationDataStorage = loader.GetService<SimulationDataStorageInterface>(m_SimulationDataStorageName);
            m_EstateService = loader.GetService<EstateServiceInterface>(m_EstateServiceName);
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
                m_GroupsService,
                m_AssetService,
                m_AssetCacheService,
                m_GridService,
                m_ServerParamService,
                ri,
                m_AvatarNameServices,
                m_SimulationDataStorage,
                m_EstateService,
                m_PhysicsFactory,
                m_NeighborService,
                m_CapabilitiesConfig);
        }
    }

    [PluginName("Scene")]
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
