/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using log4net;
using SilverSim.LL.Core.Capabilities;
using SilverSim.LL.Core;
using SilverSim.LL.Messages;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.IM;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Scene.ServiceInterfaces.SimulationData;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
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
using SilverSim.LL.Messages.LayerData;
using SilverSim.ServiceInterfaces.Estate;

namespace SilverSim.Scene.Implementation.Basic
{
    class BasicScene : SceneInterface
    {
        private static readonly ILog m_Log = LogManager.GetLogger("BASIC SCENE");

        #region Fields
        protected internal readonly RwLockedDoubleDictionary<UUID, UInt32, ObjectPart> m_Primitives = new RwLockedDoubleDictionary<UUID, UInt32, ObjectPart>();
        protected internal readonly RwLockedDictionary<UUID, IObject> m_Objects = new RwLockedDictionary<UUID, IObject>();
        //protected internal readonly RwLockedDoubleDictionary<UUID, int, ParcelInfo> m_Parcels = new RwLockedDoubleDictionary<UUID, int, ParcelInfo>();
        private LLUDPServer m_UDPServer;
        #endregion

        #region Interface wrappers
        class BasicSceneObjects : ISceneObjects
        {
            private BasicScene m_Scene;

            public BasicSceneObjects(BasicScene scene)
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

            public int Count
            {
                get
                {
                    return m_Scene.m_Objects.Count;
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

        class BasicSceneObjectParts : ISceneObjectParts
        {
            private BasicScene m_Scene;

            public BasicSceneObjectParts(BasicScene scene)
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

            public int Count
            {
                get
                {
                    int n = 0;
                    foreach(ObjectPart i in this)
                    {
                        ++n;
                    }
                    return n;
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

        class BasicSceneParcels : ISceneParcels
        {
            private BasicScene m_Scene;

            public BasicSceneParcels(BasicScene scene)
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
                    throw new NotImplementedException();
                }
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
        #endregion

        #region Services
        private ChatServiceInterface m_ChatService;
        private BasicSceneObjects m_SceneObjects;
        private BasicSceneParcels m_SceneParcels;
        private BasicSceneObjectParts m_SceneObjectParts;
        private DefaultSceneObjectGroupInterface m_SceneObjectGroups;
        private DefaultSceneAgentInterface m_SceneAgents;
        private DefaultSceneRootAgentInterface m_SceneRootAgents;
        private SimulationDataStorageInterface m_SimulationDataStorage;

        public override T GetService<T>()
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

        #endregion

        #region Constructor
        public BasicScene(
            ChatServiceInterface chatService, 
            IMServiceInterface imService,
            GroupsNameServiceInterface groupsNameService,
            AssetServiceInterface persistentAssetService,
            AssetServiceInterface temporaryAssetService,
            GridServiceInterface gridService,
            ServerParamServiceInterface serverParamService,
            RegionInfo ri,
            List<AvatarNameServiceInterface> avatarNameServices,
            SimulationDataStorageInterface simulationDataStorage,
            EstateServiceInterface estateService,
            Dictionary<string, string> capabilitiesConfig)
        : base(ri.Size.X, ri.Size.Y)
        {
            m_UDPServer = new LLUDPServer(new IPAddress(0), (int)ri.ServerPort, imService, chatService, this);
            GroupsNameService = groupsNameService;
            EstateService = estateService;
            m_SimulationDataStorage = simulationDataStorage;
            PersistentAssetService = persistentAssetService;
            TemporaryAssetService = temporaryAssetService;
            GridService = gridService;
            m_SceneObjects = new BasicSceneObjects(this);
            m_SceneObjectParts = new BasicSceneObjectParts(this);
            m_SceneObjectGroups = new DefaultSceneObjectGroupInterface(this);
            m_SceneAgents = new DefaultSceneAgentInterface(this);
            m_SceneRootAgents = new DefaultSceneRootAgentInterface(this);
            m_SceneParcels = new BasicSceneParcels(this);
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
            m_UDPServer.Start();
            SceneCapabilities.Add("SimulatorFeatures", new SimulatorFeatures("", "", "", true));

            m_PacketHandlers[MessageType.RequestRegionInfo] = HandleRequestRegionInfo;
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
            IMRouter.SceneIM.Remove(IMSend);
            m_UDPServer.Shutdown();
            m_UDPServer = null;
        }
        #endregion

        #region Properties

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

        public override ILLUDPServer UDPServer
        {
            get
            {
                return m_UDPServer;
            }
        }

        #endregion

        #region Add and Remove
        public override void Add(IObject obj)
        {
            if(obj is ObjectGroup)
            {
                ObjectGroup objgroup = (ObjectGroup)obj;
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
                        objpart.SendObjectUpdate();
                    }
                }
                catch(Exception e)
                {
                    m_Log.DebugFormat("Failed to add object: {0}: {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace.ToString());
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
                }
                catch
                {
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

        public override bool Remove(IObject obj, ScriptInstance instance = null)
        {
            if(!m_Objects.ContainsValue(obj))
            {
                return false;
            }
            if (obj is ObjectGroup)
            {
                ObjectGroup objgroup = (ObjectGroup)obj;

                foreach (ObjectPart objpart in objgroup.Values)
                {
                    m_Primitives.Remove(objpart.ID);
                    objpart.SendKillObject();
                    RemoveLocalID(objpart);
                }
                m_Objects.Remove(objgroup.ID);
            }
            else if(obj.GetType().GetInterfaces().Contains(typeof(IAgent)))
            {
                IAgent agent = (IAgent)obj;
                /* TODO: add attachments */
                m_Objects.Remove(agent.ID);
                SendKillObjectToAgents(agent.LocalID);
                RemoveLocalID(agent);
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
            this.LoadSceneAsync(m_SimulationDataStorage);
        }
        #endregion

        #region Scene LL Message interface
#if OLD
        public override void HandleSimulatorMessage(Message m)
        {
            switch(m.Number)
            {
                case MessageType.ObjectGrab: /* => simulator */
                case MessageType.ObjectGrabUpdate: /* => simulator */
                case MessageType.ObjectDeGrab: /* => simulator */
                    break;
            }
        }
#endif

        public void HandleRequestRegionInfo(Message m)
        {
            SilverSim.LL.Messages.Region.RequestRegionInfo req = (SilverSim.LL.Messages.Region.RequestRegionInfo)m;
            if(req.SessionID != req.CircuitSessionID || req.AgentID != req.CircuitAgentID)
            {
                return;
            }

            SilverSim.LL.Messages.Region.RegionInfo res = new LL.Messages.Region.RegionInfo();
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
