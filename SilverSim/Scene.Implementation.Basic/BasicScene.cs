// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Core.Capabilities;
using SilverSim.Viewer.Messages;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.IM;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Neighbor;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using SilverSim.Types.Parcel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using ThreadedClasses;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Scene.Management.Scene;
using System.Diagnostics.CodeAnalysis;
using SilverSim.Types.Estate;

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
            Dictionary<string, string> capabilitiesConfig)
        : base(ri.Size.X, ri.Size.Y)
        {
            ID = ri.ID;
            /* next line is there to break the circular dependencies */
            TryGetScene = SceneManager.Scenes.TryGetValue;

            m_UDPServer = new UDPCircuitsManager(new IPAddress(0), (int)ri.ServerPort, imService, chatService, this);
            ServerUdpPort = ri.ServerPort;
            GroupsNameService = groupsNameService;
            GroupsService = groupsService;
            EstateService = estateService;
            m_NeighborService = neighborService;
            m_SimulationDataStorage = simulationDataStorage;
            PersistentAssetService = persistentAssetService;
            TemporaryAssetService = temporaryAssetService;
            GridService = gridService;
            m_SceneObjects = new BasicSceneObjectsCollection(this);
            m_SceneObjectParts = new BasicSceneObjectPartsCollection(this);
            m_SceneObjectGroups = new DefaultSceneObjectGroupInterface(this);
            m_SceneAgents = new BasicSceneAgentsCollection(this);
            m_SceneRootAgents = new BasicSceneRootAgentsCollection(this);
            m_SceneParcels = new BasicSceneParcelsCollection(this);
            ServerParamService = serverParamService;
            CapabilitiesConfig = capabilitiesConfig;
            foreach (AvatarNameServiceInterface avNameService in avatarNameServices)
            {
                AvatarNameServices.Add(avNameService);
            }
            Access = ri.Access;
            ID = ri.ID;
            GridURI = ri.GridURI;
            Name = ri.Name;
            Owner = ri.Owner;
            GridPosition = ri.Location;
            ScopeID = ri.ScopeID;
            Terrain = new TerrainController(this);
            Environment = new EnvironmentController(this);
            m_ChatService = chatService;
            IMRouter.SceneIM.Add(IMSend);
            OnRemove += RemoveScene;
            ExternalHostName = ri.ServerIP;
            ProductName = ri.ProductName;
            RegionPort = ri.ServerPort;
            ServerURI = ri.ServerURI;
            ServerHttpPort = ri.ServerHttpPort;
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
            ScriptThreadPool.Shutdown();
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
                    m_Agents.Remove(obj.ID);
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

        public override void TriggerEstateUpdate()
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

        public override void TriggerRegionSettingsChanged()
        {
#warning Implement storing region settings
            foreach(IAgent agent in Agents)
            {
                ViewerAgent viewerAgent = agent as ViewerAgent;
                if(null != viewerAgent)
                {
                    SendRegionInfo(viewerAgent);
                }
            }
        }

        protected void SendRegionInfo(ViewerAgent agent)
        {
            Viewer.Messages.Region.RegionInfo res = new Viewer.Messages.Region.RegionInfo();
            res.AgentID = agent.Owner.ID;
            res.SessionID = agent.SessionID;

            uint estateID;
            EstateInfo estateInfo;
            try /* we need a fail protection here */
            {
                if (EstateService.RegionMap.TryGetValue(ID, out estateID) &&
                   EstateService.TryGetValue(estateID, out estateInfo))
                {
                    res.EstateID = estateID;
                    res.ParentEstateID = estateInfo.ParentEstateID;
                    res.BillableFactor = estateInfo.BillableFactor;
                    res.PricePerMeter = estateInfo.PricePerMeter;
                }
                else
                {
                    res.EstateID = 1;
                    res.ParentEstateID = 1;
                    res.BillableFactor = 1;
                    res.PricePerMeter = 1;
                }
            }
            catch(Exception e)
            {
                m_Log.WarnFormat("Exception when accessing EstateService: {0}: {1}\n{2}",
                    e.GetType().FullName,
                    e.Message,
                    e.StackTrace);
                res.EstateID = 1;
                res.ParentEstateID = 1;
                res.BillableFactor = 1;
                res.PricePerMeter = 1;
            }
            res.SimName = Name;
            res.RegionFlags = RegionSettings.AsFlags;
            res.SimAccess = Access;
            res.MaxAgents = (uint)RegionSettings.AgentLimit;
            res.ObjectBonusFactor = RegionSettings.ObjectBonus;
            res.WaterHeight = RegionSettings.WaterHeight;
            res.TerrainRaiseLimit = RegionSettings.TerrainRaiseLimit;
            res.TerrainLowerLimit = RegionSettings.TerrainLowerLimit;
            res.RedirectGridX = 0;
            res.RedirectGridY = 0;
#warning Change this to connect to Estate sun setting
            res.UseEstateSun = true;
            res.SunHour = Environment.TimeOfDay;
            res.ProductSKU = VersionInfo.SimulatorVersion;
            res.ProductName = ProductName;
            res.RegionFlagsExtended.Add(0);

            agent.SendMessageAlways(res, ID);
        }

        #endregion
    }
}
