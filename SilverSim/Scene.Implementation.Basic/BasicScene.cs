// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.IM;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scripting.Common;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Neighbor;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Estate;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using SilverSim.Types.Parcel;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Core.Capabilities;
using SilverSim.Viewer.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading;
using System.Timers;

namespace SilverSim.Scene.Implementation.Basic
{
    public partial class BasicScene : SceneInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("BASIC SCENE");

        #region Fields
        bool m_StopBasicSceneThreads;
        protected internal readonly RwLockedDoubleDictionary<UUID, UInt32, ObjectPart> m_Primitives = new RwLockedDoubleDictionary<UUID, UInt32, ObjectPart>();
        protected internal readonly RwLockedDictionary<UUID, IObject> m_Objects = new RwLockedDictionary<UUID, IObject>();
        protected internal readonly RwLockedDictionary<UUID, IAgent> m_Agents = new RwLockedDictionary<UUID, IAgent>();
        private UDPCircuitsManager m_UDPServer;
        #endregion

        #region Interface wrappers
        public sealed class BasicSceneObjectsCollection : ISceneObjects
        {
            readonly BasicScene m_Scene;

            internal BasicSceneObjectsCollection(BasicScene scene)
            {
                m_Scene = scene;
            }

            public IObject this[UUID id] 
            {
                get
                {
                    return m_Scene.m_Objects[id];
                }
            }

            public bool TryGetValue(UUID id, out IObject obj)
            {
                return m_Scene.m_Objects.TryGetValue(id, out obj);
            }

            public int Count
            {
                get
                {
                    return m_Scene.m_ObjectCount;
                }
            }

            public void ForEach(Vector3 pos, double maxdistance, Action<IObject> d)
            {
                double maxDistanceSquared = maxdistance * maxdistance;
                m_Scene.m_Objects.ForEach(delegate(IObject obj)
                {
                    if((obj.GlobalPosition - pos).LengthSquared <= maxDistanceSquared && obj.IsInScene(m_Scene))
                    {
                        d(obj);
                    }
                });
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            public IEnumerator<IObject> GetEnumerator()
            {
                return m_Scene.m_Objects.Values.GetEnumerator();
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public sealed class BasicSceneObjectPartsCollection : ISceneObjectParts
        {
            readonly BasicScene m_Scene;

            internal BasicSceneObjectPartsCollection(BasicScene scene)
            {
                m_Scene = scene;
            }

            public ObjectPart this[UUID id] 
            {
                get
                {
                    return m_Scene.m_Primitives[id];
                }
            }

            public ObjectPart this[UInt32 localID]
            {
                get
                {
                    return m_Scene.m_Primitives[localID];
                }
            }

            public bool TryGetValue(UUID id, out ObjectPart obj)
            {
                return m_Scene.m_Primitives.TryGetValue(id, out obj);
            }

            public bool TryGetValue(UInt32 localID, out ObjectPart obj)
            {
                return m_Scene.m_Primitives.TryGetValue(localID, out obj);
            }

            public int Count
            {
                get
                {
                    return m_Scene.m_PrimitiveCount;
                }
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            public IEnumerator<ObjectPart> GetEnumerator()
            {
                return m_Scene.m_Primitives.Values.GetEnumerator();
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public sealed class BasicSceneParcelsCollection : ISceneParcels
        {
            readonly BasicScene m_Scene;
            readonly object m_SceneUpdateLock = new object();

            internal BasicSceneParcelsCollection(BasicScene scene)
            {
                m_Scene = scene;
            }

            public IParcelAccessList WhiteList
            {
                get
                {
                    return m_Scene.m_SimulationDataStorage.Parcels.WhiteList;
                }
            }

            public IParcelAccessList BlackList
            {
                get
                {
                    return m_Scene.m_SimulationDataStorage.Parcels.BlackList;
                }
            }

            public ParcelInfo this[UUID id]
            {
                get
                {
                    return m_Scene.m_Parcels[id];
                }
            }

            public ParcelInfo this[Vector3 pos]
            {
                get
                {
                    int x = (int)pos.X;
                    int y = (int)pos.Y;
                    if(pos.X < 0 || pos.Y < 0 || x < 0 || y < 0 || x >= m_Scene.SizeX || y >= m_Scene.SizeY)
                    {
                        throw new KeyNotFoundException();
                    }
                    foreach(ParcelInfo p in m_Scene.m_Parcels.Values)
                    {
                        if(p.LandBitmap[x / 4, y / 4])
                        {
                            return p;
                        }
                    }
                    throw new KeyNotFoundException();
                }
            }

            public ParcelInfo this[int localID]
            {
                get
                {
                    return m_Scene.Parcels[localID];
                }
            }

            public bool TryGetValue(UUID id, out ParcelInfo pinfo)
            {
                return m_Scene.m_Parcels.TryGetValue(id, out pinfo);
            }

            public bool TryGetValue(Vector3 pos, out ParcelInfo pinfo)
            {
                pinfo = null;
                int x = (int)pos.X;
                int y = (int)pos.Y;
                if (pos.X < 0 || pos.Y < 0 || x < 0 || y < 0 || x >= m_Scene.SizeX || y >= m_Scene.SizeY)
                {
                    return false;
                }
                foreach (ParcelInfo p in m_Scene.m_Parcels.Values)
                {
                    if (p.LandBitmap[x / 4, y / 4])
                    {
                        pinfo = p;
                        return true;
                    }
                }
                return false;
            }

            public bool TryGetValue(int localID, out ParcelInfo pinfo)
            {
                return m_Scene.m_Parcels.TryGetValue(localID, out pinfo);
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            public IEnumerator<ParcelInfo> GetEnumerator()
            {
                return m_Scene.m_Parcels.Values.GetEnumerator();
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(ParcelInfo parcelInfo)
            {
                lock (m_SceneUpdateLock)
                {
                    m_Scene.AddParcel(parcelInfo);
                    m_Scene.m_SimulationDataStorage.Parcels.Store(m_Scene.ID, parcelInfo);
                }
            }

            public void Store(UUID parcelID)
            {
                lock(m_SceneUpdateLock)
                {
                    ParcelInfo pInfo;
                    if (m_Scene.m_Parcels.TryGetValue(parcelID, out pInfo))
                    {
                        m_Scene.m_SimulationDataStorage.Parcels.Store(m_Scene.ID, pInfo);
                    }
                }
            }

            public bool Remove(UUID parcelID)
            {
                lock (m_SceneUpdateLock)
                {
                    bool res = m_Scene.m_Parcels.Remove(parcelID);
                    res = res && m_Scene.m_SimulationDataStorage.Parcels.Remove(m_Scene.ID, parcelID);
                    return res;
                }
            }

            public void ResetParcels()
            {

            }
        }

        public sealed class BasicSceneAgentsCollection : ISceneAgents
        {
            readonly BasicScene m_BasicScene;

            internal BasicSceneAgentsCollection(BasicScene scene)
            {
                m_BasicScene = scene;
            }

            public int Count
            {
                get
                {
                    return m_BasicScene.m_AgentCount;
                }
            }

            public IAgent this[UUID id]
            {
                get 
                {
                    return m_BasicScene.m_Agents[id];
                }
            }

            public bool TryGetValue(UUID id, out IAgent obj)
            {
                return m_BasicScene.m_Agents.TryGetValue(id, out obj);
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            public IEnumerator<IAgent> GetEnumerator()
            {
                return m_BasicScene.m_Agents.Values.GetEnumerator();
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public sealed class BasicSceneRootAgentsCollection : ISceneAgents
        {
            readonly BasicScene m_BasicScene;

            internal BasicSceneRootAgentsCollection(BasicScene scene)
            {
                m_BasicScene = scene;
            }

            public int Count
            {
                get
                {
                    int count = 0;
                    foreach(IAgent agent in this)
                    {
                        if (agent.IsInScene(m_BasicScene))
                        {
                            ++count;
                        }
                    }
                    return count;
                }
            }

            public IAgent this[UUID id]
            {
                get
                {
                    IAgent ag = m_BasicScene.m_Agents[id];
                    if(!ag.IsInScene(m_BasicScene))
                    {
                        throw new KeyNotFoundException();
                    }
                    return ag;
                }
            }

            public bool TryGetValue(UUID id, out IAgent obj)
            {
                if(!m_BasicScene.m_Agents.TryGetValue(id, out obj))
                {
                    return false;
                }
                if(!obj.IsInScene(m_BasicScene))
                {
                    obj = null;
                    return false;
                }
                return true;
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            public IEnumerator<IAgent> GetEnumerator()
            {
                ICollection<IAgent> agents = m_BasicScene.m_Agents.Values;
                List<IAgent> rootAgents = new List<IAgent>();
                foreach(IAgent agent in agents)
                {
                    if(agent.IsInScene(m_BasicScene))
                    {
                        rootAgents.Add(agent);
                    }
                }
                return rootAgents.GetEnumerator();
            }

            [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
        #endregion

        #region Services
        readonly ChatServiceInterface m_ChatService;
        readonly BasicSceneObjectsCollection m_SceneObjects;
        readonly BasicSceneParcelsCollection m_SceneParcels;
        readonly BasicSceneObjectPartsCollection m_SceneObjectParts;
        readonly DefaultSceneObjectGroupInterface m_SceneObjectGroups;
        readonly BasicSceneAgentsCollection m_SceneAgents;
        readonly BasicSceneRootAgentsCollection m_SceneRootAgents;
        readonly SimulationDataStorageInterface m_SimulationDataStorage;
        readonly NeighborServiceInterface m_NeighborService;

        protected override object GetService(Type service)
        {
            return (service.IsAssignableFrom(typeof(ChatServiceInterface))) ?
                m_ChatService :
                base.GetService(service);
        }

        protected override void SendChatPass(ListenEvent le)
        {
            ChatServiceInterface chatService = m_ChatService;
            if (null != chatService)
            {
                chatService.Send(le);
            }
        }

        #endregion

        #region Constructor
        internal BasicScene(
            ChatServiceInterface chatService, 
            IMServiceInterface imService,
            GroupsNameServiceInterface groupsNameService,
            GroupsServiceInterface groupsService,
            AssetServiceInterface persistentAssetService,
            AssetServiceInterface temporaryAssetService,
            GridServiceInterface gridService,
            ServerParamServiceInterface serverParamService,
            RegionInfo ri,
            List<AvatarNameServiceInterface> avatarNameServices,
            SimulationDataStorageInterface simulationDataStorage,
            EstateServiceInterface estateService,
            IPhysicsSceneFactory physicsFactory,
            NeighborServiceInterface neighborService,
            Dictionary<string, string> capabilitiesConfig,
            GridServiceInterface regionStorage,
            SceneFactory myFactory)
        : base(ri.Size.X, ri.Size.Y)
        {
            if(persistentAssetService == null)
            {
                throw new ArgumentNullException("persistentAssetService");
            }
            if (gridService == null)
            {
                throw new ArgumentNullException("gridService");
            }
            if (serverParamService == null)
            {
                throw new ArgumentNullException("serverParamService");
            }
            if (ri == null)
            {
                throw new ArgumentNullException("ri");
            }
            if (avatarNameServices == null)
            {
                throw new ArgumentNullException("avatarNameServices");
            }
            if (simulationDataStorage == null)
            {
                throw new ArgumentNullException("simulationDataStorage");
            }
            if (estateService == null)
            {
                throw new ArgumentNullException("estateService");
            }
            if (capabilitiesConfig == null)
            {
                throw new ArgumentNullException("capabilitiesConfig");
            }
            if (regionStorage == null)
            {
                throw new ArgumentNullException("regionStorage");
            }

            #region Setup services
            m_ChatService = chatService;
            GroupsNameService = groupsNameService;
            GroupsService = groupsService;
            m_NeighborService = neighborService;
            m_SimulationDataStorage = simulationDataStorage;
            PersistentAssetService = persistentAssetService;
            TemporaryAssetService = temporaryAssetService;
            GridService = gridService;
            EstateService = estateService;
            /* next line is there to break the circular dependencies */
            TryGetScene = SceneManager.Scenes.TryGetValue;
            #endregion

            #region Setup Region Data
            ID = ri.ID;
            GatekeeperURI = ri.GridURI;
            Access = ri.Access;
            ID = ri.ID;
            Name = ri.Name;
            Owner = ri.Owner;
            GridPosition = ri.Location;
            ScopeID = ri.ScopeID;
            ProductName = ri.ProductName;
            RegionPort = ri.ServerPort;
            ServerURI = ri.ServerURI;
            ServerHttpPort = ri.ServerHttpPort;
            ServerParamService = serverParamService;
            ExternalHostName = ri.ServerIP;
            #endregion

            /* load estate flags cache */
            uint estateID;
            EstateInfo estate;
            if (EstateService.RegionMap.TryGetValue(ID, out estateID) &&
                EstateService.TryGetValue(estateID, out estate))
            {
                m_EstateData = estate;
            }
            else
            {
                throw new ArgumentException("Could not load estate data");
            }

            m_RestartObject = new RestartObject(this, myFactory, regionStorage);

            m_UDPServer = new UDPCircuitsManager(new IPAddress(0), (int)ri.ServerPort, imService, chatService, this);
            m_SceneObjects = new BasicSceneObjectsCollection(this);
            m_SceneObjectParts = new BasicSceneObjectPartsCollection(this);
            m_SceneObjectGroups = new DefaultSceneObjectGroupInterface(this);
            m_SceneAgents = new BasicSceneAgentsCollection(this);
            m_SceneRootAgents = new BasicSceneRootAgentsCollection(this);
            m_SceneParcels = new BasicSceneParcelsCollection(this);
            CapabilitiesConfig = capabilitiesConfig;
            foreach (AvatarNameServiceInterface avNameService in avatarNameServices)
            {
                AvatarNameServices.Add(avNameService);
            }

            Terrain = new TerrainController(this);
            Environment = new EnvironmentController(this);

            IMRouter.SceneIM.Add(IMSend);
            OnRemove += RemoveScene;
            m_UDPServer.Start();
            SceneCapabilities.Add("SimulatorFeatures", new SimulatorFeatures(string.Empty, string.Empty, string.Empty, true));
            Terrain.TerrainListeners.Add(this);
            SceneListeners.Add(m_SimulationDataStorage);

            ScriptThreadPool = new ScriptWorkerThreadPool(50, 150);
            new Thread(StoreTerrainProcess).Start();
            if(null != physicsFactory)
            {
                PhysicsScene = physicsFactory.InstantiatePhysicsScene(this);
            }
            else
            {
                PhysicsScene = new DummyPhysicsScene(ID);
                LoginControl.Ready(ReadyFlags.PhysicsTerrain);
            }
            Environment.Start();
        }
        #endregion

        #region Internal Delegates
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        private bool IMSend(GridInstantMessage im)
        {
            IAgent agent;
            try
            {
                agent = Agents[im.ToAgent.ID];
            }
            catch
            {
                return false;
            }

            return agent.IMSend(im);
        }

        void RemoveScene(SceneInterface s)
        {
            Environment.Stop();
            ScriptThreadPool.Shutdown();
            m_RestartObject = null;
            m_StopBasicSceneThreads = true;
            if (null != m_NeighborService)
            {
                RegionInfo rInfo = s.GetRegionInfo();
                rInfo.Flags &= (~RegionFlags.RegionOnline);
                m_NeighborService.NotifyNeighborStatus(rInfo);
            }
            SceneListeners.Remove(m_SimulationDataStorage);
            Terrain.TerrainListeners.Remove(this);
            IMRouter.SceneIM.Remove(IMSend);
            UDPCircuitsManager udpServer = m_UDPServer;
            if (udpServer != null)
            {
                udpServer.Shutdown();
            }
            m_UDPServer = null;
        }
        #endregion

        #region Properties

        public override List<ObjectUpdateInfo> UpdateInfos
        { 
            get
            {
                List<ObjectUpdateInfo> infos = new List<ObjectUpdateInfo>();
                foreach(IObject obj in m_Objects.Values)
                {
                    ObjectGroup objgrp = obj as ObjectGroup;
                    if (null != objgrp)
                    {
                        foreach (ObjectPart part in objgrp.ValuesByKey1)
                        {
                            infos.Add(part.UpdateInfo);
                        }
                    }
                }
                return infos;
            }
        }

        public override ISceneObjects Objects
        {
            get
            {
                return m_SceneObjects;
            }
        }
        
        public override ISceneAgents Agents
        {
            get
            {
                return m_SceneAgents;
            }
        }

        public override ISceneAgents RootAgents
        {
            get 
            {
                return m_SceneRootAgents;
            }
        }

        public override ISceneObjectGroups ObjectGroups 
        { 
            get
            {
                return m_SceneObjectGroups;
            }
        }

        public override ISceneObjectParts Primitives 
        { 
            get
            {
                return m_SceneObjectParts;
            }
        }

        public override ISceneParcels Parcels
        {
            get
            {
                return m_SceneParcels;
            }    
        }

        public override IUDPCircuitsManager UDPServer
        {
            get
            {
                return m_UDPServer;
            }
        }

        #endregion

        public override void ReregisterRegion()
        {
            RegionInfo ri = GetRegionInfo();
            GridService.RegisterRegion(ri);
            GridServiceInterface regionStorage = RegionStorage;
            if(null != regionStorage)
            {
                regionStorage.RegisterRegion(ri);
            }
            foreach (IAgent agent in Agents)
            {
                ViewerAgent viewerAgent = agent as ViewerAgent;
                if (null != viewerAgent)
                {
                    SendRegionInfo(viewerAgent);
                }
            }
        }

        #region Restart Timer
        RestartObject m_RestartObject;

        class RestartObject
        {
            readonly WeakReference m_WeakScene;
            readonly SceneFactory m_SceneFactory;
            readonly GridServiceInterface m_RegionStorage;
            public readonly System.Timers.Timer RestartTimer = new System.Timers.Timer(1000);
            int m_SecondsToRestart;
            public bool FirstTrigger;
            readonly object m_ActionLock = new object();
            public bool Abort()
            {
                lock(m_ActionLock)
                {
                    bool aborted = m_SecondsToRestart > 0;
                    m_SecondsToRestart = -1;
                    return aborted;
                }
            }

            public int SecondsToRestart
            {
                get
                {
                    lock (m_ActionLock)
                    {
                        return m_SecondsToRestart;
                    }
                }
                set
                {
                    lock (m_ActionLock)
                    {
                        if (value >= 0)
                        {
                            m_SecondsToRestart = value;
                        }
                    }
                }
            }

            ~RestartObject()
            {
                RestartTimer.Elapsed -= RestartTimerHandler;
                RestartTimer.Dispose();
            }

            public RestartObject(SceneInterface scene, SceneFactory sceneFactory, GridServiceInterface regionStorage)
            {
                m_WeakScene = new WeakReference(scene);
                m_SceneFactory = sceneFactory;
                m_RegionStorage = regionStorage;
                RestartTimer.Elapsed += RestartTimerHandler;
            }

            public void RestartTimerHandler(object o, ElapsedEventArgs evargs)
            {
                int timeLeft;
                lock (m_ActionLock)
                {
                    timeLeft = m_SecondsToRestart--;
                }
                if (timeLeft < 0)
                {
                    /* may happen during stopping */
                    return;
                }

                SceneInterface scene = (SceneInterface)m_WeakScene.Target;
                if (!m_WeakScene.IsAlive)
                {
#if DEBUG
                    m_Log.Debug("Weak reference lost");
#endif
                    return;
                }

                if (timeLeft % 15 == 0 || FirstTrigger)
                {
                    FirstTrigger = false;
                    foreach (IAgent agent in scene.RootAgents)
                    {
                        agent.SendRegionNotice(scene.Owner, 
                            string.Format(this.GetLanguageString(agent.CurrentCulture, "RegionIsRestartingInXSeconds", "Region is restarting in {0} seconds"), timeLeft),
                            scene.ID);
                    }
                    m_Log.InfoFormat("Region {0} restarting in {1} seconds", scene.Name, timeLeft);
                }

                if (timeLeft == 0)
                {
                    UUID sceneID = scene.ID;
                    UUID scopeID = scene.ScopeID;
                    RegionInfo rInfo;
                    m_WeakScene.Target = null;
                    if (m_RegionStorage.TryGetValue(scopeID, sceneID, out rInfo))
                    {
                        m_Log.InfoFormat("Restarting Region {0} ({1})", rInfo.Name, rInfo.ID.ToString());
                        SceneManager.Scenes.Remove(scene,
                            delegate(System.Globalization.CultureInfo culture)
                            {
                                return this.GetLanguageString(culture, "RegionIsNowRestarting", "Region is now restarting.");
                            });
                        scene = null;
                        /* we are still alive despite having just stopped the region */
                        m_Log.InfoFormat("Starting Region {0} ({1})", rInfo.Name, rInfo.ID.ToString());
                        try
                        {
                            scene = m_SceneFactory.Instantiate(rInfo);
                        }
                        catch (Exception e)
                        {
                            m_Log.InfoFormat("Failed to start region: {0}", e.Message);
                            return;
                        }
                        SceneManager.Scenes.Add(scene);
                        scene.LoadSceneAsync();
                    }
                    RestartTimer.Stop();
                }
            }
        }

        public override void AbortRegionRestart()
        {
            AbortRegionRestart(false);
        }

        void AbortRegionRestart(bool quietAbort)
        {
            bool aborted = m_RestartObject.Abort();
            try
            {
                m_RestartObject.RestartTimer.Stop();
            }
            catch (NullReferenceException)
            {
                /* we use NullReferenceException here */
                return;
            }
            catch (ObjectDisposedException)
            {
                /* ensure that a disposed Timer does not kill something unnecessarily */
                return;
            }

            if (aborted && !quietAbort)
            {
                foreach (IAgent agent in RootAgents)
                {
                    agent.SendRegionNotice(Owner,
                        this.GetLanguageString(agent.CurrentCulture, "RegionRestartIsAborted", "Region restart is aborted."),
                        ID);
                }
                m_Log.InfoFormat("Region restart of {0} is aborted.", Name);
            }
        }

        public override void RequestRegionRestart(int seconds)
        {
            AbortRegionRestart(seconds >= 15);
            if (seconds < 15)
            {
                return;
            }
            m_RestartObject.FirstTrigger = true;
            m_RestartObject.SecondsToRestart = seconds;
            m_RestartObject.RestartTimer.Start();
        }
        #endregion

        #region Add and Remove
        internal int m_ObjectCount;
        internal int m_PrimitiveCount;
        internal int m_AgentCount;

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override void Add(IObject obj)
        {
            ObjectGroup objgroup = obj as ObjectGroup;
            if (null != objgroup)
            {
                List<ObjectPart> removeAgain = new List<ObjectPart>();

                AddLegacyMaterials(objgroup);
                
                try
                {
                    foreach (ObjectPart objpart in objgroup.Values)
                    {
                        AddNewLocalID(objpart);
                        m_Primitives.Add(objpart.ID, objpart.LocalID, objpart);
                        removeAgain.Add(objpart);
                    }
                    objgroup.Scene = this;
                    m_Objects.Add(objgroup.ID, objgroup);

                    foreach(ObjectPart objpart in objgroup.Values)
                    {
                        Interlocked.Increment(ref m_PrimitiveCount);
                        objpart.SendObjectUpdate();
                    }
                    Interlocked.Increment(ref m_ObjectCount);
                }
                catch(Exception e)
                {
                    m_Log.DebugFormat("Failed to add object: {0}: {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                    m_Objects.Remove(objgroup.ID);
                    foreach (ObjectPart objpart in removeAgain)
                    {
                        m_Primitives.Remove(objpart.ID);
                        RemoveLocalID(objpart);
                    }
                }
            }
            else
            {
                AddNewLocalID(obj);
                try
                {
                    m_Objects.Add(obj.ID, obj);
                    if (obj.GetType().GetInterfaces().Contains(typeof(IAgent)))
                    {
                        IAgent agent = (IAgent)obj;
                        m_Agents.Add(obj.ID, agent);
                        Interlocked.Increment(ref m_AgentCount);
                        PhysicsScene.Add(obj);
                        foreach (IAgentListener aglistener in AgentListeners)
                        {
                            try
                            {
                                aglistener.AddedAgent(agent);
                            }
                            catch(Exception e)
                            {
                                m_Log.DebugFormat("Exception {0}\n{1}", e.Message, e.StackTrace);
                            }
                        }
                    }
                }
                catch
                {
                    if(m_Agents.Remove(obj.ID))
                    {
                        Interlocked.Decrement(ref m_AgentCount);
                    }
                    m_Objects.Remove(obj.ID);
                    RemoveLocalID(obj);
                }
            }
        }

        void RemoveAllScripts(ScriptInstance instance, ObjectPart part)
        {
            foreach (ObjectPartInventoryItem item in part.Inventory.Values)
            {
                if (item.AssetType == AssetType.LSLText)
                {
                    ScriptInstance script = item.ScriptInstance;
                    if (script != instance && script != null)
                    {
                        script = item.RemoveScriptInstance;
                        if (script != null)
                        {
                            ScriptThreadPool.AbortScript(script);
                            script.Abort();
                            script.Remove();
                            ScriptLoader.Remove(item.AssetID, script);
                        }
                    }
                }
            }
        }

        readonly object m_ParcelUpdateLock = new object();

        public override bool RemoveParcel(ParcelInfo p, UUID mergeTo)
        {
            bool removed = false;
            ParcelInfo mergeParcel;
            lock (m_ParcelUpdateLock)
            {

                if (m_Parcels.TryGetValue(mergeTo, out mergeParcel))
                {
                    removed = m_Parcels.Remove(p.ID);
                    m_SimulationDataStorage.RemoveRegion(p.ID);
                    mergeParcel.LandBitmap.Merge(p.LandBitmap);
                }
            }
            return removed;
        }

        public override void ResetParcels()
        {
            List<UUID> parcelIDs = m_SimulationDataStorage.Parcels.ParcelsInRegion(ID);
            foreach(UUID parcelID in parcelIDs)
            {
                m_SimulationDataStorage.Parcels.Remove(ID, parcelID);
            }

            ParcelInfo pi = new ParcelInfo((int)SizeX / 4, (int)SizeY / 4);
            pi.AABBMin = new Vector3(0, 0, 0);
            pi.AABBMax = new Vector3(SizeX - 1, SizeY - 1, 0);
            pi.ActualArea = (int)(SizeX * SizeY);
            pi.Area = (int)(SizeX * SizeY);
            pi.AuctionID = 0;
            pi.LocalID = 1;
            pi.ID = UUID.Random;
            pi.Name = "Your Parcel";
            pi.Owner = Owner;
            pi.Flags = ParcelFlags.None; /* we keep all flags disabled initially */
            pi.BillableArea = (int)(SizeX * SizeY);
            pi.LandBitmap.SetAllBits();
            pi.LandingPosition = new Vector3(128, 128, 23);
            pi.LandingLookAt = new Vector3(1, 0, 0);
            pi.ClaimDate = new Date();
            pi.Status = ParcelStatus.Leased;
            lock(m_ParcelUpdateLock)
            {
                m_SimulationDataStorage.Parcels.Store(ID, pi);
                ClearParcels();
                AddParcel(pi);
            }
        }

        public override void TriggerParcelUpdate(ParcelInfo pInfo)
        {
            lock (m_ParcelUpdateLock)
            {
                if (m_Parcels.ContainsKey(pInfo.ID))
                {
                    m_SimulationDataStorage.Parcels.Store(ID, pInfo);
                }
            }

            foreach (IAgent agent in RootAgents)
            {
                int x = (int)agent.Position.X;
                int y = (int)agent.Position.Y;
                if (agent.Position.X < 0 || agent.Position.Y < 0 || x < 0 || y < 0 || x >= SizeX || y >= SizeY)
                {
                    continue;
                }
                if (pInfo.LandBitmap[x / 4, y / 4])
                {
                    agent.SendUpdatedParcelInfo(pInfo, ID);
                }
            }
        }

        readonly object m_LightShareStoreLock = new object();
        public override void TriggerLightShareSettingsChanged()
        {
            lock(m_LightShareStoreLock)
            {
                if (Environment.IsWindLightValid)
                {
                    EnvironmentController.WindlightSkyData skyData = Environment.SkyData;
                    EnvironmentController.WindlightWaterData waterData = Environment.WaterData;
                    m_SimulationDataStorage.LightShare.Store(ID, skyData, waterData);
                }
                else
                {
                    m_SimulationDataStorage.LightShare.Remove(ID);
                }
            }
        }

        public override void ClearObjects()
        {
            List<ObjectGroup> objects = new List<ObjectGroup>();
            foreach(ObjectGroup obj in ObjectGroups)
            {
                if(!obj.IsAttached)
                {
                    objects.Add(obj);
                }
            }
            foreach(ObjectGroup obj in objects)
            {
                if (!obj.GetType().GetInterfaces().Contains(typeof(IAgent)))
                {
                    Remove(obj);
                }
            }
        }

        public override bool Remove(IObject obj, ScriptInstance instance = null)
        {
            if(!m_Objects.ContainsValue(obj))
            {
                return false;
            }
            ObjectGroup objgroup = obj as ObjectGroup;
            if (null != objgroup)
            {
                foreach (ObjectPart objpart in objgroup.Values)
                {
                    Interlocked.Decrement(ref m_PrimitiveCount);
                    m_Primitives.Remove(objpart.ID);
                    objpart.SendKillObject();
                    RemoveLocalID(objpart);
                }
                Interlocked.Decrement(ref m_ObjectCount);
                m_Objects.Remove(objgroup.ID);
            }
            else if(obj.GetType().GetInterfaces().Contains(typeof(IAgent)))
            {
                IAgent agent = (IAgent)obj;
                foreach (IAgentListener aglistener in AgentListeners)
                {
                    try
                    {
                        aglistener.RemovedAgent(agent);
                    }
                    catch (Exception e)
                    {
                        m_Log.DebugFormat("Exception {0}\n{1}", e.Message, e.StackTrace);
                    }
                }
                List<ObjectGroup> grps = agent.Attachments.RemoveAll();
                foreach(ObjectGroup grp in grps)
                {
                    Remove(grp);
                }
                PhysicsScene.Remove(agent);
                m_Objects.Remove(agent.ID);
                if(m_Agents.Remove(agent.ID))
                {
                    Interlocked.Decrement(ref m_AgentCount);
                }
                SendKillObjectToAgents(agent.LocalID);
                RemoveLocalID(agent);
                Interlocked.Decrement(ref m_AgentCount);
            }
            else
            {
                m_Objects.Remove(obj.ID);
                SendKillObjectToAgents(obj.LocalID);
                RemoveLocalID(obj);
            }

            return true;
        }
        #endregion

        #region Scene Loading
        public override void LoadSceneAsync()
        {
            if (null != m_NeighborService)
            {
                RegionInfo rInfo = GetRegionInfo();
                rInfo.Flags |= RegionFlags.RegionOnline;
                m_NeighborService.NotifyNeighborStatus(rInfo);
            }
            this.LoadSceneAsync(m_SimulationDataStorage);
        }

        /** <summary>for testing purposes only</summary> */
        public override void LoadSceneSync()
        {
            if (null != m_NeighborService)
            {
                RegionInfo rInfo = GetRegionInfo();
                rInfo.Flags |= RegionFlags.RegionOnline;
                m_NeighborService.NotifyNeighborStatus(rInfo);
            }
            this.LoadSceneSync(m_SimulationDataStorage);
        }
        #endregion

        #region Scene LL Message interface
        [PacketHandler(MessageType.RequestRegionInfo)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        public void HandleRequestRegionInfo(Message m)
        {
            SilverSim.Viewer.Messages.Region.RequestRegionInfo req = (SilverSim.Viewer.Messages.Region.RequestRegionInfo)m;
            if(req.SessionID != req.CircuitSessionID || req.AgentID != req.CircuitAgentID)
            {
                return;
            }

            IAgent agent;
            if(m_Agents.TryGetValue(req.AgentID, out agent))
            {
                ViewerAgent viewerAgent = agent as ViewerAgent;
                if (null != viewerAgent)
                {
                    SendRegionInfo(viewerAgent);
                }
            }
        }

        protected override void TriggerSpawnpointUpdate()
        {
            m_SimulationDataStorage.Spawnpoints[ID] = SpawnPoints;
        }

        public override void TriggerEstateUpdate()
        {
            uint estateID;
            EstateInfo estateInfo;
            try /* we need a fail protection here */
            {
                if (EstateService.RegionMap.TryGetValue(ID, out estateID) &&
                   EstateService.TryGetValue(estateID, out estateInfo))
                {
                    lock (m_EstateDataUpdateLock)
                    {
                        m_EstateData = estateInfo;
                    }
                }
            }
            catch (Exception e)
            {
                m_Log.WarnFormat("Exception when accessing EstateService: {0}: {1}\n{2}",
                    e.GetType().FullName,
                    e.Message,
                    e.StackTrace);
            }

            foreach (IAgent agent in Agents)
            {
                ViewerAgent viewerAgent = agent as ViewerAgent;
                if (null != viewerAgent)
                {
                    SendRegionInfo(viewerAgent);

                    ParcelInfo pInfo;
                    if(Parcels.TryGetValue(viewerAgent.GlobalPosition, out pInfo))
                    {
                        viewerAgent.SendUpdatedParcelInfo(pInfo, ID);
                    }
                }
            }
            UpdateEnvironmentSettings();
        }

        public override void TriggerRegionDataChanged()
        {
            foreach (IAgent agent in Agents)
            {
                ViewerAgent viewerAgent = agent as ViewerAgent;
                if (null != viewerAgent)
                {
                    SendRegionInfo(viewerAgent);
                }
            }
        }

        public override void TriggerStoreOfEnvironmentSettings()
        {
            m_SimulationDataStorage.EnvironmentSettings[ID] = EnvironmentSettings;
            TriggerRegionDataChanged();
        }

        public override void TriggerRegionSettingsChanged()
        {
            m_SimulationDataStorage.RegionSettings[ID] = RegionSettings;

            foreach (IAgent agent in Agents)
            {
                SendRegionInfo(agent);

                ParcelInfo pInfo;
                if (Parcels.TryGetValue(agent.GlobalPosition, out pInfo))
                {
                    agent.SendUpdatedParcelInfo(pInfo, ID);
                }
            }
            UpdateEnvironmentSettings();
        }

        public override RegionOptionFlags GetRegionFlags()
        {
            RegionOptionFlags regionFlags;
            lock(m_EstateDataUpdateLock)
            {
                regionFlags = m_EstateData.Flags;
            }
            regionFlags &= ~RegionOptionFlags.SunFixed;
            regionFlags |= RegionSettings.AsFlags;
            if (RegionSettings.IsSunFixed)
            {
                regionFlags |= RegionOptionFlags.SunFixed;
            }
            return regionFlags;
        }

        public override void SendRegionInfo(IAgent agent)
        {
            Viewer.Messages.Region.RegionInfo res = new Viewer.Messages.Region.RegionInfo();
            res.AgentID = agent.Owner.ID;
            res.SessionID = agent.Session.SessionID;

            EstateInfo estateInfo;
            lock(m_EstateDataUpdateLock)
            {
                estateInfo = m_EstateData;
            }

            RegionOptionFlags regionFlags = GetRegionFlags();

            res.EstateID = estateInfo.ID;
            res.ParentEstateID = estateInfo.ParentEstateID;
            res.BillableFactor = estateInfo.BillableFactor;
            res.PricePerMeter = estateInfo.PricePerMeter;

            estateInfo.Flags &= ~RegionOptionFlags.SunFixed;
            res.SimName = Name;
            res.RegionFlags = regionFlags;
            if(RegionSettings.IsSunFixed)
            {
                res.RegionFlags |= RegionOptionFlags.SunFixed;
                res.SunHour = RegionSettings.SunPosition + 6;
            }
            else
            {
                res.SunHour = 0;
            }
            res.SimAccess = Access;
            res.MaxAgents = (uint)RegionSettings.AgentLimit;
            res.ObjectBonusFactor = RegionSettings.ObjectBonus;
            res.WaterHeight = RegionSettings.WaterHeight;
            res.TerrainRaiseLimit = RegionSettings.TerrainRaiseLimit;
            res.TerrainLowerLimit = RegionSettings.TerrainLowerLimit;
            res.RedirectGridX = 0;
            res.RedirectGridY = 0;
            res.UseEstateSun = RegionSettings.UseEstateSun;
            res.ProductSKU = VersionInfo.SimulatorVersion;
            res.ProductName = ProductName;
            res.RegionFlagsExtended.Add((ulong)regionFlags);

            agent.SendMessageAlways(res, ID);
        }

        #endregion
    }
}
