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

using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.IM;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.ServiceInterfaces.Scene;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Pathfinding;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.ServiceInterfaces;
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
using System.Collections.Generic;

namespace SilverSim.Scene.Implementation.Common
{
    public abstract class SceneImplementationFactory : SceneFactoryInterface
    {
        public IScriptWorkerThreadPoolFactory ScriptWorkerThreadPoolFactory { get; protected set; }
        public ChatServiceFactoryInterface ChatFactory { get; protected set; }
        public GroupsNameServiceInterface GroupsNameService { get; protected set; }
        public AssetServiceInterface AssetService { get; protected set; }
        public AssetServiceInterface AssetCacheService { get; protected set; }
        public GridServiceInterface GridService { get; protected set; }
        public GridServiceInterface RegionStorage { get; protected set; }
        public GroupsServiceInterface GroupsService { get; protected set; }
        public IMServiceInterface IMService { get; protected set; }
        public EstateServiceInterface EstateService { get; protected set; }
        public SimulationDataStorageInterface SimulationDataStorage { get; protected set; }
        public readonly Dictionary<string, string> m_CapabilitiesConfig = new Dictionary<string, string>();
        public IPhysicsSceneFactory PhysicsFactory { get; protected set; }
        public NeighborServiceInterface NeighborService { get; protected set; }
        public ExternalHostNameServiceInterface ExternalHostNameService { get; protected set; }
        public readonly List<AvatarNameServiceInterface> AvatarNameServices = new List<AvatarNameServiceInterface>();
        public SceneList Scenes { get; protected set; }
        public IMRouter IMRouter { get; protected set; }
        public BaseHttpServer HttpServer { get; protected set; }
        public List<IPortControlServiceInterface> PortControlServices { get; protected set; }
        public IWindModelFactory WindModelFactory { get; protected set; }
        public IPathfindingServiceFactory PathfindingServiceFactory { get; protected set; }
        public ExperienceServiceInterface ExperienceService { get; protected set; }

        public List<IUserAgentServicePlugin> UserAgentServicePlugins { get; protected set; }
        public List<IAssetServicePlugin> AssetServicePlugins { get; protected set; }
        public List<IInventoryServicePlugin> InventoryServicePlugins { get; protected set; }

        protected SceneImplementationFactory()
        {
            ScriptWorkerThreadPoolFactory = ScriptWorkerThreadPool.Factory;
        }
    }
}
