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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Implementation.Common;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Pathfinding;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Neighbor;
using SilverSim.ServiceInterfaces.PortControl;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types.Grid;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Scene.Implementation.Basic
{
    [Description("Basic Scene Factory")]
    [PluginName("Scene")]
    public sealed class SceneFactory : SceneImplementationFactory, IPlugin
    {
        private readonly string m_ChatFactoryName;
        private readonly string m_GroupsNameServiceName;
        private readonly string m_GroupsServiceName;
        private readonly string m_AssetServiceName;
        private readonly string m_AssetCacheServiceName;
        private readonly string m_GridServiceName;
        private readonly string m_RegionStorageName;
        private readonly string m_IMServiceName;
        private readonly string m_EstateServiceName;
        private readonly string m_SimulationDataStorageName;
        private readonly string m_PhysicsName;
        private readonly string m_NeighborServiceName;
        private readonly string m_WindModelFactoryName;
        private readonly string m_PathfindingServiceFactoryName;
        private readonly string m_ExperienceServiceName;
        private readonly List<string> m_AvatarNameServiceNames = new List<string>();

        public SceneFactory(IConfig ownConfig)
        {
            m_RegionStorageName = ownConfig.GetString("RegionStorage", "RegionStorage");
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
            m_WindModelFactoryName = ownConfig.GetString("WindPlugin", string.Empty);
            m_PathfindingServiceFactoryName = ownConfig.GetString("PathfindingPlugin", string.Empty);
            m_ExperienceServiceName = ownConfig.GetString("ExperienceService", string.Empty);
            string avatarNameServices = ownConfig.GetString("AvatarNameServices", string.Empty);
            if (!string.IsNullOrEmpty(avatarNameServices))
            {
                foreach (string p in avatarNameServices.Split(','))
                {
                    m_AvatarNameServiceNames.Add(p.Trim());
                }
            }

            foreach(string key in ownConfig.GetKeys())
            {
                if(key.StartsWith("Cap_") && key != "Cap_")
                {
                    m_CapabilitiesConfig[key.Substring(4)] = ownConfig.GetString(key).Trim();
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            if(!string.IsNullOrEmpty(m_WindModelFactoryName))
            {
                WindModelFactory = loader.GetService<IWindModelFactory>(m_WindModelFactoryName);
            }
            if(!string.IsNullOrEmpty(m_PathfindingServiceFactoryName))
            {
                PathfindingServiceFactory = loader.GetService<IPathfindingServiceFactory>(m_PathfindingServiceFactoryName);
            }
            HttpServer = loader.HttpServer;
            PortControlServices = loader.GetServicesByValue<IPortControlServiceInterface>();
            ExternalHostNameService = loader.ExternalHostNameService;
            Scenes = loader.Scenes;
            IMRouter = loader.IMRouter;
            RegionStorage = loader.GetService<GridServiceInterface>(m_RegionStorageName);
            ChatFactory = loader.GetService<ChatServiceFactoryInterface>(m_ChatFactoryName);
            if (m_GroupsNameServiceName.Length != 0)
            {
                GroupsNameService = loader.GetService<GroupsNameServiceInterface>(m_GroupsNameServiceName);
            }
            if (m_GroupsServiceName.Length != 0)
            {
                GroupsService = loader.GetService<GroupsServiceInterface>(m_GroupsServiceName);
            }
            if (m_PhysicsName.Length != 0)
            {
                PhysicsFactory = loader.GetService<IPhysicsSceneFactory>(m_PhysicsName);
            }
            if (m_NeighborServiceName.Length != 0)
            {
                NeighborService = loader.GetService<NeighborServiceInterface>(m_NeighborServiceName);
            }
            if(m_ExperienceServiceName.Length != 0)
            {
                ExperienceService = loader.GetService<ExperienceServiceInterface>(m_ExperienceServiceName);
            }
            AssetService = loader.GetService<AssetServiceInterface>(m_AssetServiceName);
            AssetCacheService = loader.GetService<AssetServiceInterface>(m_AssetCacheServiceName);
            GridService = loader.GetService<GridServiceInterface>(m_GridServiceName);
            IMService = loader.GetService<IMServiceInterface>(m_IMServiceName);
            SimulationDataStorage = loader.GetService<SimulationDataStorageInterface>(m_SimulationDataStorageName);
            EstateService = loader.GetService<EstateServiceInterface>(m_EstateServiceName);
            foreach(string servicename in m_AvatarNameServiceNames)
            {
                AvatarNameServices.Add(loader.GetService<AvatarNameServiceInterface>(servicename));
            }
            UserAgentServicePlugins = loader.GetServicesByValue<IUserAgentServicePlugin>();
            AssetServicePlugins = loader.GetServicesByValue<IAssetServicePlugin>();
            InventoryServicePlugins = loader.GetServicesByValue<IInventoryServicePlugin>();
        }

        public override SceneInterface Instantiate(RegionInfo ri) => new BasicScene(
                    this,
                    ri);
    }
}
