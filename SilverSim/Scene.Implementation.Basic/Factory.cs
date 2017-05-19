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
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.IM;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.ServiceInterfaces.Scene;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Pathfinding;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Estate;
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
    public sealed class SceneFactory : SceneFactoryInterface, IPlugin
    {
        internal ChatServiceFactoryInterface m_ChatFactory { get; private set; }
        readonly string m_ChatFactoryName;
        readonly string m_GroupsNameServiceName;
        readonly string m_GroupsServiceName;
        readonly string m_AssetServiceName;
        readonly string m_AssetCacheServiceName;
        readonly string m_GridServiceName;
        readonly string m_RegionStorageName;
        readonly string m_IMServiceName;
        readonly string m_EstateServiceName;
        readonly string m_SimulationDataStorageName;
        readonly string m_PhysicsName;
        readonly string m_NeighborServiceName;
        readonly string m_WindModelFactoryName;
        readonly string m_PathfindingServiceFactoryName;
        readonly List<string> m_AvatarNameServiceNames = new List<string>();

        internal GroupsNameServiceInterface m_GroupsNameService { get; private set; }
        internal AssetServiceInterface m_AssetService { get; private set; }
        internal AssetServiceInterface m_AssetCacheService { get; private set; }
        internal GridServiceInterface m_GridService { get; private set; }
        internal GridServiceInterface m_RegionStorage { get; private set; }
        internal GroupsServiceInterface m_GroupsService { get; private set; }
        internal IMServiceInterface m_IMService { get; private set; }
        internal EstateServiceInterface m_EstateService { get; private set; }
        internal SimulationDataStorageInterface m_SimulationDataStorage { get; private set; }
        internal readonly Dictionary<string, string> m_CapabilitiesConfig = new Dictionary<string, string>();
        internal IPhysicsSceneFactory m_PhysicsFactory { get; private set; }
        internal NeighborServiceInterface m_NeighborService { get; private set; }
        internal ExternalHostNameServiceInterface m_ExternalHostNameService { get; private set; }
        internal readonly List<AvatarNameServiceInterface> m_AvatarNameServices = new List<AvatarNameServiceInterface>();
        internal SceneList m_Scenes { get; private set; }
        internal IMRouter m_IMRouter { get; private set; }
        internal BaseHttpServer m_HttpServer { get; private set; }
        internal List<IPortControlServiceInterface> m_PortControlServices { get; private set; }
        internal IWindModelFactory m_WindModelFactory { get; private set; }
        internal IPathfindingServiceFactory m_PathfindingServiceFactory { get; private set; }

        internal List<IUserAgentServicePlugin> UserAgentServicePlugins { get; private set; }
        internal List<IAssetServicePlugin> AssetServicePlugins { get; private set; }
        internal List<IInventoryServicePlugin> InventoryServicePlugins { get; private set; }

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
                    m_CapabilitiesConfig[key.Substring(4)] = ownConfig.GetString(key);
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            if(!string.IsNullOrEmpty(m_WindModelFactoryName))
            {
                m_WindModelFactory = loader.GetService<IWindModelFactory>(m_WindModelFactoryName);
            }
            if(!string.IsNullOrEmpty(m_PathfindingServiceFactoryName))
            {
                m_PathfindingServiceFactory = loader.GetService<IPathfindingServiceFactory>(m_PathfindingServiceFactoryName);
            }
            m_HttpServer = loader.HttpServer;
            m_PortControlServices = loader.GetServicesByValue<IPortControlServiceInterface>();
            m_ExternalHostNameService = loader.ExternalHostNameService;
            m_Scenes = loader.Scenes;
            m_IMRouter = loader.IMRouter;
            m_RegionStorage = loader.GetService<GridServiceInterface>(m_RegionStorageName);
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
            m_SimulationDataStorage = loader.GetService<SimulationDataStorageInterface>(m_SimulationDataStorageName);
            m_EstateService = loader.GetService<EstateServiceInterface>(m_EstateServiceName);
            foreach(string servicename in m_AvatarNameServiceNames)
            {
                m_AvatarNameServices.Add(loader.GetService<AvatarNameServiceInterface>(servicename));
            }
            UserAgentServicePlugins = loader.GetServicesByValue<IUserAgentServicePlugin>();
            AssetServicePlugins = loader.GetServicesByValue<IAssetServicePlugin>();
            InventoryServicePlugins = loader.GetServicesByValue<IInventoryServicePlugin>();
        }

        public override SceneInterface Instantiate(RegionInfo ri) => new BasicScene(
                    this,
                    ri);
    }

    [PluginName("Scene")]
    public class Factory : IPluginFactory
    {
        public IPlugin Initialize(ConfigurationLoader loader, IConfig config) =>
            new SceneFactory(config);
    }
}
