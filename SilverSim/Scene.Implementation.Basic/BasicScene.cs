﻿// SilverSim is distributed under the terms of the
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
        //protected internal readonly RwLockedDoubleDictionary<UUID, int, ParcelInfo> m_Parcels = new RwLockedDoubleDictionary<UUID, int, ParcelInfo>();
        private UDPCircuitsManager m_UDPServer;
        #endregion

        #region Interface wrappers
        public sealed class BasicSceneObjectsCollection : ISceneObjects
        {
            private BasicScene m_Scene;

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

            public IEnumerator<IObject> GetEnumerator()
            {
                return m_Scene.m_Objects.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public sealed class BasicSceneObjectPartsCollection : ISceneObjectParts
        {
            private BasicScene m_Scene;

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

            public IEnumerator<ObjectPart> GetEnumerator()
            {
                return m_Scene.m_Primitives.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public sealed class BasicSceneParcelsCollection : ISceneParcels
        {
            private BasicScene m_Scene;

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
                    if(pos.X < 0 || pos.Y < 0 || x < 0 || y < 0 || x >= m_Scene.RegionData.Size.X || y >= m_Scene.RegionData.Size.Y)
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
                if (pos.X < 0 || pos.Y < 0 || x < 0 || y < 0 || x >= m_Scene.RegionData.Size.X || y >= m_Scene.RegionData.Size.Y)
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

            public IEnumerator<ParcelInfo> GetEnumerator()
            {
                return m_Scene.m_Parcels.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public sealed class BasicSceneAgentsCollection : ISceneAgents
        {
            BasicScene m_BasicScene;

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

            public IEnumerator<IAgent> GetEnumerator()
            {
                return m_BasicScene.m_Agents.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public sealed class BasicSceneRootAgentsCollection : ISceneAgents
        {
            BasicScene m_BasicScene;

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

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
        #endregion

        #region Services
        private ChatServiceInterface m_ChatService;
        private BasicSceneObjectsCollection m_SceneObjects;
        private BasicSceneParcelsCollection m_SceneParcels;
        private BasicSceneObjectPartsCollection m_SceneObjectParts;
        private DefaultSceneObjectGroupInterface m_SceneObjectGroups;
        private BasicSceneAgentsCollection m_SceneAgents;
        private BasicSceneRootAgentsCollection m_SceneRootAgents;
        private SimulationDataStorageInterface m_SimulationDataStorage;
        private NeighborServiceInterface m_NeighborService;

        public virtual new T GetService<T>()
        {
            if(typeof(T).IsAssignableFrom(typeof(ChatServiceInterface)))
            {
                return (T) (object)m_ChatService;
            }
            else
            {
                return base.GetService<T>();
            }
        }

        protected virtual new void SendChatPass(ListenEvent le)
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
            /* next line is there to break the circular dependencies */
            TryGetScene = SceneManager.Scenes.TryGetValue;

            m_UDPServer = new UDPCircuitsManager(new IPAddress(0), (int)ri.ServerPort, imService, chatService, this);
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
            ID = ri.ID;
            Name = ri.Name;
            Owner = ri.Owner;
            GridPosition = ri.Location;
            Terrain = new TerrainController(this);
            Environment = new EnvironmentController(this);
            m_ChatService = chatService;
            IMRouter.SceneIM.Add(IMSend);
            OnRemove += RemoveScene;
            ExternalHostName = ri.ServerIP;
            RegionPort = ri.ServerPort;
            ServerURI = ri.ServerURI;
            ServerHttpPort = ri.ServerHttpPort;
            m_UDPServer.Start();
            SceneCapabilities.Add("SimulatorFeatures", new SimulatorFeatures(string.Empty, string.Empty, string.Empty, true));
            Terrain.TerrainListeners.Add(this);
            SceneListeners.Add(m_SimulationDataStorage);
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

        private void RemoveScene(SceneInterface s)
        {
            m_StopBasicSceneThreads = true;
            if (null != m_NeighborService)
            {
                RegionInfo rInfo = s.RegionData;
                rInfo.Flags &= (~RegionFlags.RegionOnline);
                m_NeighborService.NotifyNeighborStatus(rInfo);
            }
            SceneListeners.Remove(m_SimulationDataStorage);
            Terrain.TerrainListeners.Remove(this);
            IMRouter.SceneIM.Remove(IMSend);
            m_UDPServer.Shutdown();
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

        #region Add and Remove
        internal int m_ObjectCount;
        internal int m_PrimitiveCount;
        internal int m_AgentCount;

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
                        m_Agents.Add(obj.ID, (IAgent)obj);
                        Interlocked.Increment(ref m_AgentCount);
                        PhysicsScene.Add(obj);
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
                            script.Abort();
                            script.Remove();
                            ScriptLoader.Remove(item.AssetID, script);
                        }
                    }
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
                RegionInfo rInfo = RegionData;
                rInfo.Flags |= RegionFlags.RegionOnline;
                m_NeighborService.NotifyNeighborStatus(rInfo);
            }
            this.LoadSceneAsync(m_SimulationDataStorage);
        }
        #endregion

        #region Scene LL Message interface
        [PacketHandler(MessageType.RequestRegionInfo)]
        public void HandleRequestRegionInfo(Message m)
        {
            SilverSim.Viewer.Messages.Region.RequestRegionInfo req = (SilverSim.Viewer.Messages.Region.RequestRegionInfo)m;
            if(req.SessionID != req.CircuitSessionID || req.AgentID != req.CircuitAgentID)
            {
                return;
            }

            SilverSim.Viewer.Messages.Region.RegionInfo res = new Viewer.Messages.Region.RegionInfo();
            res.AgentID = req.AgentID;
            res.SessionID = req.SessionID;

            res.SimName = RegionData.Name;
            res.EstateID = 1; /* TODO: */
            res.ParentEstateID = 1; /* TODO: */
            res.RegionFlags = RegionData.Flags;
            res.SimAccess = RegionData.Access;
            res.MaxAgents = 40;
            res.BillableFactor = 1;
            res.ObjectBonusFactor = 1;
            res.WaterHeight = 21;
            res.TerrainRaiseLimit = 100;
            res.TerrainLowerLimit = 0;
            res.PricePerMeter = 1;
            res.RedirectGridX = 0;
            res.RedirectGridY = 0;
            res.UseEstateSun = true;
            res.SunHour = 1;
            res.ProductSKU = VersionInfo.SimulatorVersion;
            res.ProductName = VersionInfo.ProductName;
            res.RegionFlagsExtended.Add(0);

            UDPServer.SendMessageToAgent(req.AgentID, res);
        }
        #endregion
    }
}
