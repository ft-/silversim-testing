// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Economy;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using SilverSim.Types.Parcel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
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
    }

    public abstract partial class SceneInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("SCENE");

        public readonly RegionSettings RegionSettings = new RegionSettings();
        protected EstateInfo m_EstateData = new EstateInfo();
        protected readonly object m_EstateDataUpdateLock = new object();

        #region Scene Loading fields (do not use for anything else)
        public Thread m_LoaderThread;
        public object m_LoaderThreadLock = new object();
        #endregion

        public UUID ID { get; protected set; }
        public UUID ScopeID { get; protected set; }
        public UUID RegionSecret { get; private set; }
        public uint RegionPort { get; protected set; }
        public uint ServerHttpPort { get; protected set; }
        public UUID RegionMapTexture { get; protected set; }
        public UUID ParcelMapTexture { get; protected set; }
        public string ServerURI { get; set; } /* updated by region registrar */
        public uint SizeX { get; private set; }
        public uint SizeY { get; private set; }
        public string Name { get; set; }
        public IPAddress LastIPAddress { get; protected set; }
        public string ExternalHostName { get; protected set; }
        public GridVector GridPosition { get; protected set; }
        public abstract ISceneObjects Objects { get; }
        public abstract ISceneObjectGroups ObjectGroups { get; }
        public abstract ISceneObjectParts Primitives { get; }
        public abstract ISceneAgents Agents { get; }
        public abstract ISceneAgents RootAgents { get; }
        public abstract ISceneParcels Parcels { get; }
        public abstract List<ObjectUpdateInfo> UpdateInfos { get; }
        public event Action<SceneInterface> OnRemove;
        public event Action<SceneInterface, IPAddress> OnIPChanged;
        public AssetServiceInterface TemporaryAssetService { get; protected set; }
        public AssetServiceInterface PersistentAssetService { get; protected set; }
        public AssetServiceInterface AssetService { get; private set; }
        public GroupsServiceInterface GroupsService { get; protected set; }
        public GroupsNameServiceInterface GroupsNameService { get; protected set; }
        public AvatarNameServiceInterface AvatarNameService { get; private set; }
        public readonly RwLockedList<AvatarNameServiceInterface> AvatarNameServices = new RwLockedList<AvatarNameServiceInterface>();
        public readonly RwLockedList<ISceneListener> SceneListeners = new RwLockedList<ISceneListener>();
        public readonly RwLockedList<IAgentListener> AgentListeners = new RwLockedList<IAgentListener>();
        public GridServiceInterface GridService { get; protected set; }
        public EconomyServiceInterface EconomyService { get; protected set; }
        public EstateServiceInterface EstateService { get; protected set; }
        public ServerParamServiceInterface ServerParamService { get; protected set; }
        public EconomyInfo EconomyData { get; protected set; }
        readonly NotecardCache m_NotecardCache;
        public Dictionary<string, string> CapabilitiesConfig { get; protected set; }
        public string GatekeeperURI { get; protected set; }
        public IScriptWorkerThreadPool ScriptThreadPool { get; protected set; }
        public Date m_StartTime = new Date();

        public string ProductName { get; set; }

        public Date RegionStartTime
        {
            get
            {
                return new Date(m_StartTime);
            }
        }

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
            get
            {
                return LoginControl.IsLoginEnabled;
            }
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

        public abstract void LoadSceneAsync();
        public abstract void LoadSceneSync();

        #region Physics
        IPhysicsScene m_PhysicsScene;
        readonly object m_PhysicsSceneChangeLock = new object();

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
                    if (null != m_PhysicsScene)
                    {
                        m_PhysicsScene.RemoveAll();
                    }
                    m_PhysicsScene = value;
                    foreach (ObjectPart p in Primitives)
                    {
                        m_PhysicsScene.Add(p);
                    }
                }
            }
        }
        #endregion

        /* do not put any other than ICapabilityInterface into this list */
        public readonly RwLockedDictionary<string, object> SceneCapabilities = new RwLockedDictionary<string, object>();

        public RegionInfo GetRegionInfo()
        {
            RegionInfo reg = new RegionInfo();
            reg.Access = Access;
            reg.Flags = RegionFlags.RegionOnline;
            reg.ID = ID;
            reg.Location = GridPosition;
            reg.Name = Name;
            reg.Owner = Owner;
            reg.ServerURI = ServerURI;
            reg.ServerHttpPort = ServerHttpPort;
            reg.ParcelMapTexture = ParcelMapTexture;
            reg.RegionMapTexture = RegionMapTexture;
            reg.RegionSecret = (string)RegionSecret;
            reg.ScopeID = UUID.Zero;
            reg.ServerIP = ExternalHostName;
            reg.ServerPort = RegionPort;
            reg.Size.X = SizeX;
            reg.Size.Y = SizeY;
            return reg;
        }

        public RegionAccess Access
        {
            get;
            set;
        }

        public UUI Owner
        {
            get;
            set;
        }

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
            else
            {
                m_Log.DebugFormat("Unknown target type {0}", service.FullName);
                return null;
            }
        }

        public T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }

        private const uint PARCEL_BLOCK_SIZE = 4;

        void LoginsEnabledHandler(bool state)
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

        public SceneInterface(UInt32 sizeX, UInt32 sizeY)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            Owner = UUI.Unknown;
            AssetService = new DefaultAssetService(this);
            AvatarNameService = new DefaultAvatarNameService(AvatarNameServices);
            CapabilitiesConfig = new Dictionary<string, string>();
            RegionSecret = UUID.Random;
            LastIPAddress = new IPAddress(0);
            m_NotecardCache = new NotecardCache(this);
            LoginControl.OnLoginsEnabled += LoginsEnabledHandler;

            /* basic capabilities */

            InitializeParcelLayer();
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void InvokeOnRemove()
        {
            LoginControl.OnLoginsEnabled -= LoginsEnabledHandler;
            var ev = OnRemove;
            if (null != ev)
            {
                foreach (Action<SceneInterface> del in ev.GetInvocationList())
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
        }

        public abstract void Add(IObject obj);
        public abstract bool Remove(IObject obj, Script.ScriptInstance instance = null);
        public abstract void ClearObjects();
        public abstract void AbortRegionRestart();
        public abstract void RequestRegionRestart(int seconds);
        public abstract void TriggerLightShareSettingsChanged();
        public abstract void SendRegionInfo(IAgent agent);

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

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void TriggerIPChanged(IPAddress ip)
        {
            LastIPAddress = ip;
            var ev = OnIPChanged;
            if (ev != null)
            {
                foreach (Action<SceneInterface, IPAddress> del in ev.GetInvocationList())
                {
                    try
                    {
                        del(this, ip);
                    }
                    catch (Exception e)
                    {
                        m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace);
                    }
                }
            }
        }

        public abstract void TriggerRegionSettingsChanged();
        public abstract void TriggerEstateUpdate();
        public abstract void TriggerRegionDataChanged();

        public abstract void ResetParcels();
        public abstract void StartStorage();

        private readonly RwLockedDictionary<UInt32, IObject> m_LocalIDs = new RwLockedDictionary<uint, IObject>();
        private UInt32 m_LastLocalID;
        readonly object m_LastLocalIDLock = new object();

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

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        protected void AddNewLocalID(IObject v)
        {
            do
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
            } while (true);
        }

        protected void RemoveLocalID(IObject v)
        {
            m_LocalIDs.Remove(v.LocalID);
        }

        #region Dynamic IP Support
        public void CheckExternalNameLookup()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(ExternalHostName);
            for (int i = 0; i < addresses.Length; ++i)
            {
                if (addresses[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    /* we take the first IPv4 address */
                    if (!LastIPAddress.Equals(addresses[i]))
                    {
                        TriggerIPChanged(LastIPAddress);
                    }
                    return;
                }
            }
        }
        #endregion
    }
}
