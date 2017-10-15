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
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Pathfinding;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Economy;
using SilverSim.Types.Estate;
using SilverSim.Types.Experience;
using SilverSim.Types.Grid;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Messages.Agent;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Scene.Types.Scene
{
    public interface ISceneObjects : IEnumerable<IObject>
    {
        IObject this[UUID id] { get; }
        bool TryGetValue(UUID id, out IObject obj);
        void ForEach(Vector3 pos, double maxdistance, Action<IObject> d);
        int Count { get; }
    }

    public interface ISceneObjectGroups : IEnumerable<ObjectGroup>
    {
        ObjectGroup this[UUID id] { get; }
        bool TryGetValue(UUID id, out ObjectGroup grp);
        int Count { get; }
    }

    public interface ISceneObjectParts : IEnumerable<ObjectPart>
    {
        ObjectPart this[UUID id] { get; }
        ObjectPart this[UInt32 localId] { get; }
        bool TryGetValue(UUID id, out ObjectPart part);
        bool TryGetValueByName(string name, out ObjectPart part);
        bool TryGetValue(UInt32 localid, out ObjectPart part);
        int Count { get; }
    }

    public interface ISceneAgents : IEnumerable<IAgent>
    {
        IAgent this[UUID id] { get; }
        bool TryGetValue(UUID id, out IAgent agent);
        int Count { get; }
    }

    public interface IParcelAccessList
    {
        bool this[UUID regionID, UUID parcelID, UUI accessor] { get; }
        List<ParcelAccessEntry> this[UUID regionID, UUID parcelID] { get; }
        void Store(ParcelAccessEntry entry);
        bool Remove(UUID regionID, UUID parcelID);
        bool Remove(UUID regionID, UUID parcelID, UUI accessor);
    }

    public interface IParcelExperienceList
    {
        ParcelExperienceEntry this[UUID regionID, UUID parcelID, UUID experienceID] { get; }
        bool TryGetValue(UUID regionID, UUID parcelID, UUID experienceID, out ParcelExperienceEntry entry);
        List<ParcelExperienceEntry> this[UUID regionID, UUID parcelID] { get; }
        void Store(ParcelExperienceEntry entry);
        bool Remove(UUID regionID, UUID parcelID);
        bool Remove(UUID regionID, UUID parcelID, UUID experienceID);
    }

    public interface IRegionExperienceList
    {
        RegionExperienceInfo this[UUID regionID, UUID experienceID] { get; }
        bool TryGetValue(UUID regionID, UUID experienceID, out RegionExperienceInfo info);
        List<RegionExperienceInfo> this[UUID regionID] { get; }
        void Store(RegionExperienceInfo info);
        bool Remove(UUID regionID, UUID experienceID);
    }

    public interface IRegionTrustedExperienceList
    {
        bool this[UUID regionID, UUID experienceID] { get; set; }
        bool TryGetValue(UUID regionID, UUID experienceID, out bool trusted);
        List<UUID> this[UUID regionID] { get; }
        bool Remove(UUID regionID, UUID experienceID);
    }

    public interface ISceneParcels : IEnumerable<ParcelInfo>
    {
        ParcelInfo this[UUID id] { get; }
        ParcelInfo this[Vector3 position] { get; }
        ParcelInfo this[int localID] { get; }

        bool TryGetValue(UUID id, out ParcelInfo pinfo);
        bool TryGetValue(Vector3 position, out ParcelInfo pinfo);
        bool TryGetValue(int localID, out ParcelInfo pinfo);

        void Add(ParcelInfo parcelInfo);
        void Store(UUID parcelID);
        bool Remove(UUID parcelID);
        void ResetParcels();

        IParcelAccessList WhiteList { get; }
        IParcelAccessList BlackList { get; }

        IParcelExperienceList Experiences { get; }
    }

    public abstract partial class SceneInterface : IServerParamListener
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SCENE");

        public readonly RegionSettings RegionSettings = new RegionSettings();
        protected EstateInfo m_EstateData = new EstateInfo();
        protected readonly object m_EstateDataUpdateLock = new object();

        #region Scene Loading fields (do not use for anything else)
        public Thread m_LoaderThread;
        public readonly object m_LoaderThreadLock = new object();
        #endregion

        public UUID ID { get; protected set; }
        public UUID ScopeID { get; protected set; }
        public UUID RegionSecret { get; }
        public uint RegionPort { get; protected set; }
        public abstract uint ServerHttpPort { get; }
        public UUID RegionMapTexture { get; protected set; }
        public UUID ParcelMapTexture { get; protected set; }
        public abstract string ServerURI { get; }
        public uint SizeX { get; }
        public uint SizeY { get; }
        public string Name { get; set; }
        public GridVector GridPosition { get; protected set; }
        public abstract ISceneObjects Objects { get; }
        public abstract ISceneObjectGroups ObjectGroups { get; }
        public abstract ISceneObjectParts Primitives { get; }
        public abstract ISceneAgents Agents { get; }
        public abstract ISceneAgents RootAgents { get; }
        public abstract ISceneParcels Parcels { get; }
        public abstract IRegionExperienceList RegionExperiences { get; }
        public abstract IRegionTrustedExperienceList RegionTrustedExperiences { get; }
        public abstract List<ObjectUpdateInfo> UpdateInfos { get; }
        public event Action<SceneInterface> OnRemove;
        public AssetServiceInterface TemporaryAssetService { get; protected set; }
        public AssetServiceInterface PersistentAssetService { get; protected set; }
        public AssetServiceInterface AssetService { get; }
        public GroupsServiceInterface GroupsService { get; protected set; }
        public GroupsNameServiceInterface GroupsNameService { get; protected set; }
        public AvatarNameServiceInterface AvatarNameService { get; }
        public IPathfindingService PathfindingService { get; protected set; }
        public readonly RwLockedList<AvatarNameServiceInterface> AvatarNameServices = new RwLockedList<AvatarNameServiceInterface>();
        public readonly RwLockedList<ISceneListener> SceneListeners = new RwLockedList<ISceneListener>();
        public readonly RwLockedList<IAgentListener> AgentListeners = new RwLockedList<IAgentListener>();
        public GridServiceInterface GridService { get; protected set; }
        public EconomyServiceInterface EconomyService { get; protected set; }
        public EstateServiceInterface EstateService { get; protected set; }
        public ExperienceServiceInterface ExperienceService { get; protected set; }
        public EconomyInfo EconomyData { get; protected set; }
        private readonly NotecardCache m_NotecardCache;
        public Dictionary<string, string> CapabilitiesConfig { get; protected set; }
        public string GatekeeperURI { get; protected set; }
        public IScriptWorkerThreadPool ScriptThreadPool { get; protected set; }
        public Date m_StartTime = new Date();

        protected List<IUserAgentServicePlugin> UserAgentServicePlugins = new List<IUserAgentServicePlugin>();
        protected List<IAssetServicePlugin> AssetServicePlugins = new List<IAssetServicePlugin>();
        protected List<IInventoryServicePlugin> InventoryServicePlugins = new List<IInventoryServicePlugin>();

        public string ProductName { get; set; }

        public Date RegionStartTime => new Date(m_StartTime);

        public virtual uint FrameNumber
        {
            get
            {
                IPhysicsScene physicsScene = m_PhysicsScene;
                return physicsScene != null ? physicsScene.PhysicsFrameNumber : 0;
            }
        }

        public bool IsSceneEnabled
        {
            get { return LoginControl.IsLoginEnabled; }

            set
            {
                if (value)
                {
                    LoginControl.Ready(ReadyFlags.LoginsEnable);
                }
                else
                {
                    LoginControl.NotReady(ReadyFlags.LoginsEnable);
                }
            }
        }

        public abstract void LoadScene();
        public abstract void LoadSceneSync();

        [Flags]
        public enum RunState
        {
            None = 0,
            Stopped = 1,
            Started = 2,
            Starting = 4,
            Stopping = 8,
        }

        public RunState CurrentRunState { get; private set; }

        public void UpdateRunState(RunState setState, RunState clrState)
        {
            lock (m_LoaderThreadLock)
            {
                CurrentRunState = (CurrentRunState | setState) & (~clrState);
            }
        }

        private Dictionary<UUID, Vector3> BuildCoarseLocationData()
        {
            var coarseData = new Dictionary<UUID, Vector3>();

            foreach(IAgent agent in RootAgents)
            {
                coarseData.Add(agent.ID, agent.GlobalPosition);
            }

            return coarseData;
        }

        public void SendCoarseLocationUpdateToAllAgents()
        {
            var data = BuildCoarseLocationData();

            foreach(IAgent agent in Agents)
            {
                SendCoarseLocationUpdateToSpecificAgent(agent);
            }
        }

        public void SendCoarseLocationUpdateToSpecificAgent(IAgent agent)
        {
            SendCoarseLocationUpdateToSpecificAgent(agent, BuildCoarseLocationData());
        }

        private void SendCoarseLocationUpdateToSpecificAgent(IAgent agent, Dictionary<UUID, Vector3> data)
        {
            const int NUM_OF_ENTRIES = 50;
            CoarseLocationUpdate upd = null;
            UUID ownId = agent.ID;
            UUID trackId = agent.TracksAgentID;
            /* var region scaling */
            double scaleX = 256 / SizeX;
            double scaleY = 256 / SizeY;

            foreach(KeyValuePair<UUID, Vector3> kvp in data)
            {
                if(upd == null)
                {
                    upd = new CoarseLocationUpdate()
                    {
                        Prey = -1,
                        You = -1
                    };
                }

                int count = upd.AgentData.Count;

                if(kvp.Key == ownId)
                {
                    upd.You = (short)count;
                }
                if(kvp.Key == trackId)
                {
                    upd.Prey = (short)count;
                }

                upd.AgentData.Add(new CoarseLocationUpdate.AgentDataEntry
                {
                    AgentID = kvp.Key,
                    X = (byte)(kvp.Value.X * scaleX).Clamp(0, 255),
                    Y = (byte)(kvp.Value.Y * scaleY).Clamp(0, 255),
                    Z = (byte)(kvp.Value.Z / 4).Clamp(0, 255)
                });

                if(count + 1 == NUM_OF_ENTRIES)
                {
                    agent.SendMessageAlways(upd, ID);
                    upd = null;
                }
            }

            if(upd != null)
            {
                agent.SendMessageAlways(upd, ID);
            }
        }

        #region Physics
        private IPhysicsScene m_PhysicsScene;
        private readonly object m_PhysicsSceneChangeLock = new object();

        public IPhysicsScene PhysicsScene
        {
            get
            {
                lock (m_PhysicsSceneChangeLock)
                {
                    return m_PhysicsScene;
                }
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                lock (m_PhysicsSceneChangeLock)
                {
                    m_PhysicsScene?.RemoveAll();
                    m_PhysicsScene = value;
                    foreach (ObjectPart p in Primitives)
                    {
                        m_PhysicsScene.Add(p);
                    }
                }
            }
        }
        #endregion

        public virtual void TriggerParameterUpdated(UUID regionID, string parametername, string value)
        {
        }

        /* do not put any other than ICapabilityInterface into this list */
        public readonly RwLockedDictionary<string, object> SceneCapabilities = new RwLockedDictionary<string, object>();

        protected ExternalHostNameServiceInterface m_ExternalHostNameService;
        public string ExternalHostName => m_ExternalHostNameService.ExternalHostName;

        public RegionInfo GetRegionInfo() => new RegionInfo()
        {
            Access = Access,
            Flags = RegionFlags.RegionOnline,
            ID = ID,
            Location = GridPosition,
            Name = Name,
            Owner = Owner,
            ServerURI = ServerURI,
            ServerHttpPort = ServerHttpPort,
            ParcelMapTexture = ParcelMapTexture,
            RegionMapTexture = RegionMapTexture,
            RegionSecret = (string)RegionSecret,
            ScopeID = UUID.Zero,
            ServerIP = ExternalHostName,
            ServerPort = RegionPort,
            Size = new GridVector(SizeX, SizeY)
        };

        public uint ParentEstateID
        {
            get
            {
                lock (m_EstateDataUpdateLock)
                {
                    return m_EstateData.ID;
                }
            }
        }

        public RegionAccess Access { get; set; }

        public UUI Owner { get; set; }

        protected virtual object GetService(Type service)
        {
            if (service.IsAssignableFrom(typeof(AssetServiceInterface)))
            {
                return AssetService;
            }
            else if (service.IsAssignableFrom(typeof(AvatarNameServiceInterface)))
            {
                return AvatarNameService;
            }
            else if (service.IsAssignableFrom(typeof(GroupsNameServiceInterface)))
            {
                return GroupsNameService;
            }
            else if (service.IsAssignableFrom(typeof(GridServiceInterface)))
            {
                return GridService;
            }
            else if (service.IsAssignableFrom(typeof(NotecardCache)))
            {
                return m_NotecardCache;
            }
            else if (service.IsAssignableFrom(typeof(EconomyServiceInterface)))
            {
                return EconomyService;
            }
            else if (service.IsAssignableFrom(typeof(GroupsServiceInterface)))
            {
                return GroupsService;
            }
            else if (service.IsAssignableFrom(typeof(EstateServiceInterface)))
            {
                return EstateService;
            }
            else if (service.IsAssignableFrom(typeof(EnvironmentController)))
            {
                return Environment;
            }
            else if (service.IsAssignableFrom(typeof(TerrainController)))
            {
                return Terrain;
            }
            else if(service.IsAssignableFrom(typeof(IPathfindingService)) && PathfindingService != null)
            {
                return PathfindingService;
            }
            else
            {
                m_Log.DebugFormat("Unknown target type {0}", service.FullName);
                return null;
            }
        }

        public T GetService<T>() => (T)GetService(typeof(T));

        private const uint PARCEL_BLOCK_SIZE = 4;

        private void LoginsEnabledHandler(UUID sceneid, bool state)
        {
            if (state)
            {
                m_Log.WarnFormat("LOGINS ENABLED at {0} (ID {1})", Name, ID);
            }
            else
            {
                m_Log.WarnFormat("LOGINS DISABLED at {0} (ID {1})", Name, ID);
            }
        }

        public abstract void ReregisterRegion();
        public abstract void RelocateRegion(GridVector location);
        public GridServiceInterface RegionStorage { get; set; }

        protected SceneInterface(UInt32 sizeX, UInt32 sizeY)
        {
            LoginControl = new LoginController(this);
            RegionMapTexture = TextureConstant.DefaultTerrainTexture2; /* set Default terrain Texture 2 as initial RegionMapTexture */
            SizeX = sizeX;
            SizeY = sizeY;
            Owner = UUI.Unknown;
            AssetService = new DefaultAssetService(this);
            AvatarNameService = new AggregatingAvatarNameService(AvatarNameServices);
            CapabilitiesConfig = new Dictionary<string, string>();
            RegionSecret = UUID.Random;
            m_NotecardCache = new NotecardCache(this);
            LoginControl.OnLoginsEnabled += LoginsEnabledHandler;

            /* basic capabilities */

            InitializeParcelLayer();
        }

        public void InvokeOnRemove()
        {
            LoginControl.OnLoginsEnabled -= LoginsEnabledHandler;
            foreach (Action<SceneInterface> del in OnRemove?.GetInvocationList() ?? new Delegate[0])
            {
                try
                {
                    del(this);
                }
                catch (Exception e)
                {
                    m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace);
                }
            }
        }

        public abstract void AddObjectGroupOnly(IObject obj);
        public abstract void Add(IObject obj);
        public abstract bool RemoveObjectGroupOnly(UUID objID);
        public abstract bool Remove(IObject obj, Script.ScriptInstance instance = null);
        public abstract void ClearObjects();
        public abstract void AbortRegionRestart();
        public abstract void RequestRegionRestart(int seconds);
        public abstract void TriggerLightShareSettingsChanged();
        public abstract void SendRegionInfo(IAgent agent);
        public void SendEstateInfo(IAgent agent)
        {
            EstateInfo info;
            lock (m_EstateDataUpdateLock)
            {
                info = new EstateInfo(m_EstateData);
            }
            agent.SendEstateUpdateInfo(UUID.Zero, UUID.Zero, info, ID, true);
        }

        public bool IsKeyframedMotionEnabled { get; set; }

        public void TriggerAgentChangedScene(IAgent agent)
        {
            foreach(IAgentListener aglistener in AgentListeners)
            {
                try
                {
                    aglistener.AgentChangedScene(agent);
                }
                catch(Exception e)
                {
                    m_Log.DebugFormat("TriggerAgentChangedScene: {0}\n{1}", e.Message, e.StackTrace);
                }
            }
        }

        public abstract IUDPCircuitsManager UDPServer { get; }

        public abstract void TriggerRegionSettingsChanged();
        public abstract void TriggerEstateUpdate();
        public abstract void TriggerRegionDataChanged();

        public abstract void ResetParcels();
        public abstract void StartStorage();

        private readonly RwLockedDictionary<UInt32, IObject> m_LocalIDs = new RwLockedDictionary<uint, IObject>();
        private UInt32 m_LastLocalID;
        private readonly object m_LastLocalIDLock = new object();

        private UInt32 NextLocalID
        {
            get
            {
                UInt32 newLocalID;
                lock(m_LastLocalIDLock)
                {
                    ++m_LastLocalID;
                    if(0 == m_LastLocalID)
                    {
                        ++m_LastLocalID;
                    }
                    newLocalID = m_LastLocalID;
                }
                return newLocalID;
            }
        }

        protected void AddNewLocalID(IObject v)
        {
            while (true)
            {
                try
                {
                    UInt32 localID = NextLocalID;
                    m_LocalIDs.Add(localID, v);
                    v.LocalID = localID;
                    break;
                }
                catch
                {
                    /* no action required */
                }
            }
        }

        protected void RemoveLocalID(IObject v)
        {
            m_LocalIDs.Remove(v.LocalID);
        }

        private Date m_ActiveStatsLastUpdated = Date.Now;
        private int m_ActiveScripts;
        private int m_ActiveObjects;

        private void CheckActiveStats()
        {
            if (DateTime.Now - m_ActiveStatsLastUpdated > TimeSpan.FromSeconds(1))
            {
                m_ActiveStatsLastUpdated = Date.Now;
                int activeScripts = 0;
                int activeObjects = 0;

                foreach (ObjectPart part in Primitives)
                {
                    int runningScripts = part.Inventory.CountRunningScripts;
                    activeScripts += runningScripts;
                    if (runningScripts > 0)
                    {
                        ++activeObjects;
                    }
                    else if (part.IsPhysics && part.ObjectGroup.ID == part.ID)
                    {
                        ++activeObjects;
                    }
                }
                m_ActiveScripts = activeScripts;
                m_ActiveObjects = activeObjects;
            }
        }

        public int ActiveScripts
        {
            get
            {
                CheckActiveStats();
                return m_ActiveScripts;
            }
        }

        public int ActiveObjects
        {
            get
            {
                CheckActiveStats();
                return m_ActiveObjects;
            }
        }
    }
}
