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
using SilverSim.Main.Common;
using SilverSim.Main.Common.HttpServer;
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
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Neighbor;
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
        protected internal readonly RwLockedDoubleDictionary<UUID, UInt32, ObjectPart> m_Primitives = new RwLockedDoubleDictionary<UUID, UInt32, ObjectPart>();
        protected internal readonly RwLockedDictionary<UUID, IObject> m_Objects = new RwLockedDictionary<UUID, IObject>();
        protected internal readonly RwLockedDictionary<UUID, IAgent> m_Agents = new RwLockedDictionary<UUID, IAgent>();
        private UDPCircuitsManager m_UDPServer;
        private readonly IMServiceInterface m_IMService;
        private readonly SceneList m_Scenes;
        private readonly IMRouter m_IMRouter;
        #endregion

        #region Interface wrappers
        public sealed class BasicSceneObjectsCollection : ISceneObjects
        {
            private readonly BasicScene m_Scene;

            internal BasicSceneObjectsCollection(BasicScene scene)
            {
                m_Scene = scene;
            }

            public IObject this[UUID id] => m_Scene.m_Objects[id];

            public bool TryGetValue(UUID id, out IObject obj) => m_Scene.m_Objects.TryGetValue(id, out obj);

            public int Count => m_Scene.m_ObjectCount;

            public void ForEach(Vector3 pos, double maxdistance, Action<IObject> d)
            {
                double maxDistanceSquared = maxdistance * maxdistance;
                foreach(IObject obj in m_Scene.Objects)
                {
                    if ((obj.GlobalPosition - pos).LengthSquared <= maxDistanceSquared && obj.IsInScene(m_Scene))
                    {
                        d(obj);
                    }
                }
            }

            public IEnumerator<IObject> GetEnumerator() => m_Scene.m_Objects.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public sealed class BasicSceneObjectPartsCollection : ISceneObjectParts
        {
            private readonly BasicScene m_Scene;

            internal BasicSceneObjectPartsCollection(BasicScene scene)
            {
                m_Scene = scene;
            }

            public ObjectPart this[UUID id] => m_Scene.m_Primitives[id];

            public ObjectPart this[UInt32 localID] => m_Scene.m_Primitives[localID];

            public bool TryGetValue(UUID id, out ObjectPart obj) => m_Scene.m_Primitives.TryGetValue(id, out obj);

            public bool TryGetValue(UInt32 localID, out ObjectPart obj) => m_Scene.m_Primitives.TryGetValue(localID, out obj);

            public int Count => m_Scene.m_PrimitiveCount;

            public IEnumerator<ObjectPart> GetEnumerator() => m_Scene.m_Primitives.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public sealed class BasicSceneParcelsCollection : ISceneParcels
        {
            private readonly BasicScene m_Scene;
            private readonly object m_SceneUpdateLock = new object();

            internal BasicSceneParcelsCollection(BasicScene scene)
            {
                m_Scene = scene;
            }

            public IParcelAccessList WhiteList => m_Scene.m_SimulationDataStorage.Parcels.WhiteList;

            public IParcelAccessList BlackList => m_Scene.m_SimulationDataStorage.Parcels.BlackList;

            public IParcelExperienceList Experiences => m_Scene.m_SimulationDataStorage.Parcels.Experiences;

            public ParcelInfo this[UUID id] => m_Scene.m_Parcels[id];

            public ParcelInfo this[Vector3 pos]
            {
                get
                {
                    var x = (int)pos.X;
                    var y = (int)pos.Y;
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

            public ParcelInfo this[int localID] => m_Scene.Parcels[localID];

            public bool TryGetValue(UUID id, out ParcelInfo pinfo) => m_Scene.m_Parcels.TryGetValue(id, out pinfo);

            public bool TryGetValue(Vector3 pos, out ParcelInfo pinfo)
            {
                pinfo = null;
                var x = (int)pos.X;
                var y = (int)pos.Y;
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

            public bool TryGetValue(int localID, out ParcelInfo pinfo) => m_Scene.m_Parcels.TryGetValue(localID, out pinfo);

            public IEnumerator<ParcelInfo> GetEnumerator() => m_Scene.m_Parcels.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
            private readonly BasicScene m_BasicScene;

            internal BasicSceneAgentsCollection(BasicScene scene)
            {
                m_BasicScene = scene;
            }

            public int Count => m_BasicScene.m_AgentCount;

            public IAgent this[UUID id] => m_BasicScene.m_Agents[id];

            public bool TryGetValue(UUID id, out IAgent obj) => m_BasicScene.m_Agents.TryGetValue(id, out obj);

            public IEnumerator<IAgent> GetEnumerator() => m_BasicScene.m_Agents.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public sealed class BasicSceneRootAgentsCollection : ISceneAgents
        {
            private readonly BasicScene m_BasicScene;

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

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        #endregion

        #region Services
        private readonly ChatServiceInterface m_ChatService;
        private readonly BasicSceneObjectsCollection m_SceneObjects;
        private readonly BasicSceneParcelsCollection m_SceneParcels;
        private readonly BasicSceneObjectPartsCollection m_SceneObjectParts;
        private readonly DefaultSceneObjectGroupInterface m_SceneObjectGroups;
        private readonly BasicSceneAgentsCollection m_SceneAgents;
        private readonly BasicSceneRootAgentsCollection m_SceneRootAgents;
        private readonly SimulationDataStorageInterface m_SimulationDataStorage;
        private readonly NeighborServiceInterface m_NeighborService;
        private readonly BaseHttpServer m_HttpServer;

        protected override object GetService(Type service)
        {
            if (service.IsAssignableFrom(typeof(ChatServiceInterface)))
            {
                return m_ChatService;
            }
            else if (service.IsAssignableFrom(typeof(IMServiceInterface)))
            {
                return m_IMService;
            }
            else
            {
                return base.GetService(service);
            }
        }

        protected override void SendChatPass(ListenEvent le)
        {
            ChatServiceInterface chatService = m_ChatService;
            chatService?.Send(le);
        }

        #endregion

        public override string ServerURI => m_HttpServer.ServerURI;

        public override uint ServerHttpPort => m_HttpServer.Port;

        #region Constructor
        internal BasicScene(
            SceneFactory sceneParams,
            RegionInfo ri)
        : base(ri.Size.X, ri.Size.Y)
        {
            m_Scenes = sceneParams.Scenes;
            m_HttpServer = sceneParams.HttpServer;
            if (sceneParams.AssetService == null)
            {
                throw new ArgumentNullException("persistentAssetService");
            }
            if (sceneParams.GridService == null)
            {
                throw new ArgumentNullException("gridService");
            }
            if (ri == null)
            {
                throw new ArgumentNullException(nameof(ri));
            }
            if (sceneParams.AvatarNameServices == null)
            {
                throw new ArgumentNullException("avatarNameServices");
            }
            if (sceneParams.SimulationDataStorage == null)
            {
                throw new ArgumentNullException("simulationDataStorage");
            }
            if (sceneParams.EstateService == null)
            {
                throw new ArgumentNullException("estateService");
            }
            if (sceneParams.m_CapabilitiesConfig == null)
            {
                throw new ArgumentNullException("capabilitiesConfig");
            }
            if (sceneParams.RegionStorage == null)
            {
                throw new ArgumentNullException("regionStorage");
            }

            #region Setup services
            m_ChatService = sceneParams.ChatFactory.Instantiate(ri.ID);
            RegionStorage = sceneParams.RegionStorage;
            GroupsNameService = sceneParams.GroupsNameService;
            GroupsService = sceneParams.GroupsService;
            m_NeighborService = sceneParams.NeighborService;
            m_SimulationDataStorage = sceneParams.SimulationDataStorage;
            PersistentAssetService = sceneParams.AssetService;
            TemporaryAssetService = sceneParams.AssetCacheService;
            GridService = sceneParams.GridService;
            ExperienceService = sceneParams.ExperienceService;
            EstateService = sceneParams.EstateService;
            /* next line is there to break the circular dependencies */
            TryGetScene = m_Scenes.TryGetValue;

            UserAgentServicePlugins.AddRange(sceneParams.UserAgentServicePlugins);
            AssetServicePlugins.AddRange(sceneParams.AssetServicePlugins);
            InventoryServicePlugins.AddRange(sceneParams.InventoryServicePlugins);

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
            m_ExternalHostNameService = sceneParams.ExternalHostNameService;
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

            m_RestartObject = new RestartObject(m_Scenes, this, sceneParams, sceneParams.RegionStorage);
            m_IMService = sceneParams.IMService;
            m_UDPServer = new UDPCircuitsManager(new IPAddress(0), (int)ri.ServerPort, sceneParams.IMService, m_ChatService, this, sceneParams.PortControlServices);
            m_SceneObjects = new BasicSceneObjectsCollection(this);
            m_SceneObjectParts = new BasicSceneObjectPartsCollection(this);
            m_SceneObjectGroups = new DefaultSceneObjectGroupInterface(this);
            m_SceneAgents = new BasicSceneAgentsCollection(this);
            m_SceneRootAgents = new BasicSceneRootAgentsCollection(this);
            m_SceneParcels = new BasicSceneParcelsCollection(this);
            CapabilitiesConfig = sceneParams.m_CapabilitiesConfig;
            foreach (AvatarNameServiceInterface avNameService in sceneParams.AvatarNameServices)
            {
                AvatarNameServices.Add(avNameService);
            }

            Terrain = new TerrainController(this);
            Environment = new EnvironmentController(this, sceneParams.WindModelFactory);

            if(sceneParams.PathfindingServiceFactory != null)
            {
                PathfindingService = sceneParams.PathfindingServiceFactory.Instantiate(this);
            }

            m_IMRouter = sceneParams.IMRouter;
            m_IMRouter.SceneIM.Add(IMSend);
            OnRemove += RemoveScene;
            m_UDPServer.Start();
            SceneCapabilities.Add("SimulatorFeatures", new SimulatorFeatures(string.Empty, string.Empty, string.Empty, true));

            ScriptThreadPool = new ScriptWorkerThreadPool(50, 150, ID);
            if(sceneParams.PhysicsFactory != null)
            {
                try
                {
                    PhysicsScene = sceneParams.PhysicsFactory.InstantiatePhysicsScene(this);
                }
                catch
                {
                    RemoveScene(this);
                    throw;
                }
            }
            else
            {
                PhysicsScene = new DummyPhysicsScene(this);
                LoginControl.Ready(ReadyFlags.PhysicsTerrain);
            }
            Environment.Start();
            Environment.OnEnvironmentControllerChangeParams += StoreEnvironmentControllerData;
        }
        #endregion

        private void StoreEnvironmentControllerData(byte[] serializedData)
        {
            m_SimulationDataStorage.EnvironmentController[ID] = serializedData;
        }

        #region Internal Delegates
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

        private SimulationDataStorageInterface.TerrainListener m_TerrainListener;
        private SimulationDataStorageInterface.SceneListener m_SceneListener;

        public override void StartStorage()
        {
            m_TerrainListener = m_SimulationDataStorage.GetTerrainListener(ID);
            m_TerrainListener.StartStorageThread();
            Terrain.TerrainListeners.Add(m_TerrainListener);

            m_SceneListener = m_SimulationDataStorage.GetSceneListener(ID);
            m_SceneListener.StartStorageThread();
            SceneListeners.Add(m_SceneListener);
        }

        private void RemoveScene(SceneInterface s)
        {
            ScriptThreadPool.Shutdown();
            Environment.OnEnvironmentControllerChangeParams -= StoreEnvironmentControllerData;
            Environment.Stop();
            PathfindingService?.Stop();
            int serializedcount = 0;
            foreach(ObjectPart part in Primitives)
            {
                foreach(ObjectPartInventoryItem item in part.Inventory.Values)
                {
                    IScriptState state = item.ScriptState;
                    if(state != null)
                    {
                        try
                        {
                            m_SimulationDataStorage.ScriptStates[ID, part.ID, item.ID] = state.ToDbSerializedState();
                            if (++serializedcount % 50 == 0)
                            {
                                m_Log.InfoFormat("Serialized {0} script states", serializedcount);
                            }
                        }
                        catch(Exception e)
                        {
                            m_Log.ErrorFormat("Script state serialization failed for {0} ({1}): prim {2} ({3}): item {4} ({5}): {6}: {7}\n{8}",
                                Name, ID, part.Name, part.ID, item.Name, item.ID, e.GetType().FullName, e.Message, e.StackTrace);
                        }
                    }
                }
            }

            if(serializedcount == 1)
            {
                m_Log.InfoFormat("Serialized {0} script state", serializedcount);
            }
            else if (serializedcount % 50 != 0)
            {
                m_Log.InfoFormat("Serialized {0} script states", serializedcount);
            }

            m_RestartObject = null;

            if (m_NeighborService != null)
            {
                RegionInfo rInfo = s.GetRegionInfo();
                rInfo.Flags &= ~RegionFlags.RegionOnline;
                m_NeighborService.NotifyNeighborStatus(rInfo);
            }

            if (m_SceneListener != null)
            {
                m_SceneListener.StopStorageThread();
                SceneListeners.Remove(m_SceneListener);
            }

            if (m_TerrainListener != null)
            {
                m_TerrainListener.StopStorageThread();
                Terrain.TerrainListeners.Remove(m_TerrainListener);
            }

            if (m_IMRouter != null)
            {
                m_IMRouter.SceneIM.Remove(IMSend);
            }
            UDPCircuitsManager udpServer = m_UDPServer;
            udpServer?.Shutdown();
            m_UDPServer = null;
        }
        #endregion

        #region Properties

        public override List<ObjectUpdateInfo> UpdateInfos
        {
            get
            {
                var infos = new List<ObjectUpdateInfo>();
                foreach(IObject obj in m_Objects.Values)
                {
                    var objgrp = obj as ObjectGroup;
                    if (objgrp != null)
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

        public override ISceneObjects Objects => m_SceneObjects;

        public override ISceneAgents Agents => m_SceneAgents;

        public override ISceneAgents RootAgents => m_SceneRootAgents;

        public override ISceneObjectGroups ObjectGroups => m_SceneObjectGroups;

        public override ISceneObjectParts Primitives => m_SceneObjectParts;

        public override ISceneParcels Parcels => m_SceneParcels;

        public override IUDPCircuitsManager UDPServer => m_UDPServer;

        public override IRegionExperienceList RegionExperiences => m_SimulationDataStorage.RegionExperiences;

        public override IRegionTrustedExperienceList RegionTrustedExperiences => m_SimulationDataStorage.TrustedExperiences;
        #endregion

        public override void RelocateRegion(GridVector location)
        {
            RegionInfo ri = GetRegionInfo();
            ri.Location = location;
            GridService.RegisterRegion(ri);
            GridPosition = location;

            GridServiceInterface regionStorage = RegionStorage;
            regionStorage?.RegisterRegion(ri);
        }

        public override void ReregisterRegion()
        {
            RegionInfo ri = GetRegionInfo();
            GridService.RegisterRegion(ri);
            GridServiceInterface regionStorage = RegionStorage;
            regionStorage?.RegisterRegion(ri);
            foreach (IAgent agent in Agents)
            {
                var viewerAgent = agent as ViewerAgent;
                if (viewerAgent != null)
                {
                    SendRegionInfo(viewerAgent);
                }
            }
        }

        #region Restart Timer
        private RestartObject m_RestartObject;

        private class RestartObject
        {
            private readonly WeakReference m_WeakScene;
            private readonly SceneFactory m_SceneFactory;
            private readonly GridServiceInterface m_RegionStorage;
            public readonly System.Timers.Timer RestartTimer = new System.Timers.Timer(1000);
            private int m_SecondsToRestart;
            public bool FirstTrigger;
            private readonly SceneList m_Scenes;
            private readonly object m_ActionLock = new object();
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

            public RestartObject(SceneList scenes, SceneInterface scene, SceneFactory sceneFactory, GridServiceInterface regionStorage)
            {
                m_Scenes = scenes;
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

                var scene = (SceneInterface)m_WeakScene.Target;
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
                        m_Scenes.Remove(scene,
                            (System.Globalization.CultureInfo culture) =>
                                this.GetLanguageString(culture, "RegionIsNowRestarting", "Region is now restarting."));
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
                        m_Scenes.Add(scene);
                        scene.LoadScene();
                    }
                    RestartTimer.Stop();
                }
            }
        }

        public override void AbortRegionRestart()
        {
            AbortRegionRestart(false);
        }

        private void AbortRegionRestart(bool quietAbort)
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

        public override void AddObjectGroupOnly(IObject obj)
        {
            var objgroup = obj as ObjectGroup;
            if (objgroup != null)
            {
                objgroup.Scene = this;
                m_Objects.Add(objgroup.ID, objgroup);
                Interlocked.Increment(ref m_ObjectCount);
            }
        }

        public override void Add(IObject obj)
        {
            var objgroup = obj as ObjectGroup;
            if (objgroup != null)
            {
                var removeAgain = new List<ObjectPart>();

                AddLegacyMaterials(objgroup);

                try
                {
                    objgroup.Scene = this;
                    foreach (ObjectPart objpart in objgroup.Values)
                    {
                        AddNewLocalID(objpart);
                        m_Primitives.Add(objpart.ID, objpart.LocalID, objpart);
                        removeAgain.Add(objpart);
                    }
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
                    objgroup.Scene = null;
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
                        var agent = (IAgent)obj;
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

        private void RemoveAllScripts(ScriptInstance instance, ObjectPart part)
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

        private readonly object m_ParcelUpdateLock = new object();

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
                    TriggerParcelUpdate(mergeParcel);
                }
            }
            return removed;
        }

        public override void ResetParcels()
        {
            foreach(UUID parcelID in m_SimulationDataStorage.Parcels.ParcelsInRegion(ID))
            {
                m_SimulationDataStorage.Parcels.Remove(ID, parcelID);
            }

            var pi = new ParcelInfo((int)SizeX / 4, (int)SizeY / 4)
            {
                AABBMin = new Vector3(0, 0, 0),
                AABBMax = new Vector3(SizeX - 1, SizeY - 1, 0),
                ActualArea = (int)(SizeX * SizeY),
                Area = (int)(SizeX * SizeY),
                AuctionID = 0,
                LocalID = 1,
                ID = UUID.Random,
                Name = "Your Parcel",
                Owner = Owner,
                Flags = ParcelFlags.None, /* we keep all flags disabled initially */
                BillableArea = (int)(SizeX * SizeY),
                LandingPosition = new Vector3(128, 128, 23),
                LandingLookAt = new Vector3(1, 0, 0),
                ClaimDate = new Date(),
                Status = ParcelStatus.Leased
            };
            pi.LandBitmap.SetAllBits();
            lock (m_ParcelUpdateLock)
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
                var x = (int)agent.Position.X;
                var y = (int)agent.Position.Y;
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

        private readonly object m_LightShareStoreLock = new object();
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
            var objects = new List<ObjectGroup>();
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

        public override bool RemoveObjectGroupOnly(UUID objID)
        {
            if (m_Objects.Remove(objID))
            {
                Interlocked.Decrement(ref m_ObjectCount);
                return true;
            }
            return false;
        }

        public override bool Remove(IObject obj, ScriptInstance instance = null)
        {
            if(!m_Objects.ContainsValue(obj))
            {
                return false;
            }
            var objgroup = obj as ObjectGroup;
            if (objgroup != null)
            {
                foreach (ObjectPart objpart in objgroup.Values)
                {
                    Interlocked.Decrement(ref m_PrimitiveCount);
                    foreach(ObjectPartInventoryItem item in objpart.Inventory.Values)
                    {
                        ScriptInstance removeinstance = item.ScriptInstance;
                        if (removeinstance != instance)
                        {
                            removeinstance?.Remove();
                        }
                    }
                    m_Primitives.Remove(objpart.ID);
                    objpart.SendKillObject();
                    RemoveLocalID(objpart);
                }
                
                if(m_Objects.Remove(objgroup.ID))
                {
                    Interlocked.Decrement(ref m_ObjectCount);
                }
            }
            else if(obj.GetType().GetInterfaces().Contains(typeof(IAgent)))
            {
                var agent = (IAgent)obj;
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
                foreach(ObjectGroup grp in agent.Attachments.RemoveAll())
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
        public override void LoadScene()
        {
            if (m_NeighborService != null)
            {
                RegionInfo rInfo = GetRegionInfo();
                rInfo.Flags |= RegionFlags.RegionOnline;
                m_NeighborService.NotifyNeighborStatus(rInfo);
            }
            this.LoadScene(m_SimulationDataStorage, m_Scenes);
        }

        /** <summary>for testing purposes only</summary> */
        public override void LoadSceneSync()
        {
            if (m_NeighborService != null)
            {
                RegionInfo rInfo = GetRegionInfo();
                rInfo.Flags |= RegionFlags.RegionOnline;
                m_NeighborService.NotifyNeighborStatus(rInfo);
            }
            this.LoadSceneSync(m_SimulationDataStorage, m_Scenes);
        }
        #endregion

        #region Scene LL Message interface
        [PacketHandler(MessageType.RequestRegionInfo)]
        public void HandleRequestRegionInfo(Message m)
        {
            var req = (Viewer.Messages.Region.RequestRegionInfo)m;
            if(req.SessionID != req.CircuitSessionID || req.AgentID != req.CircuitAgentID)
            {
                return;
            }

            IAgent agent;
            if(m_Agents.TryGetValue(req.AgentID, out agent))
            {
                var viewerAgent = agent as ViewerAgent;
                if (viewerAgent != null)
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
                var viewerAgent = agent as ViewerAgent;
                if (viewerAgent != null)
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
                var viewerAgent = agent as ViewerAgent;
                if (viewerAgent != null)
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

            foreach (ParcelInfo pinfo in m_Parcels.Values)
            {
                pinfo.Access = Access;
            }

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

        public override void SendRegionInfo(IAgent agent)
        {
            EstateInfo estateInfo;
            lock (m_EstateDataUpdateLock)
            {
                estateInfo = m_EstateData;
            }
            estateInfo.Flags &= ~RegionOptionFlags.SunFixed;

            RegionOptionFlags regionFlags = RegionSettings.AsFlags;

            var res = new Viewer.Messages.Region.RegionInfo()
            {
                AgentID = agent.Owner.ID,
                SessionID = agent.Session.SessionID,

                EstateID = estateInfo.ID,
                ParentEstateID = estateInfo.ParentEstateID,
                BillableFactor = estateInfo.BillableFactor,
                PricePerMeter = estateInfo.PricePerMeter,

                SimName = Name,
                RegionFlags = regionFlags,
                SimAccess = Access,
                MaxAgents = (uint)RegionSettings.AgentLimit,
                ObjectBonusFactor = RegionSettings.ObjectBonus,
                WaterHeight = RegionSettings.WaterHeight,
                TerrainRaiseLimit = RegionSettings.TerrainRaiseLimit,
                TerrainLowerLimit = RegionSettings.TerrainLowerLimit,
                RedirectGridX = 0,
                RedirectGridY = 0,
                UseEstateSun = RegionSettings.UseEstateSun,
                ProductSKU = VersionInfo.SimulatorVersion,
                ProductName = ProductName
            };
            if (RegionSettings.IsSunFixed)
            {
                res.RegionFlags |= RegionOptionFlags.SunFixed;
                res.SunHour = RegionSettings.SunPosition + 6;
            }
            else
            {
                res.SunHour = 0;
            }
            res.RegionFlagsExtended.Add((ulong)regionFlags);

            agent.SendMessageAlways(res, ID);
        }

        #endregion
    }
}
