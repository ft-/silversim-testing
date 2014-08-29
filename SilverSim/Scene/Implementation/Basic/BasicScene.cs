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

using SilverSim.LL.Core;
using SilverSim.LL.Messages;
using SilverSim.Scene.Management.IM;
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Parcel;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Terrain;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Avatar;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Types;
using SilverSim.Types.IM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using ThreadedClasses;
using SilverSim.Types.Grid;

namespace SilverSim.Scene.Implementation.Basic
{
    class BasicScene : SceneInterface
    {
        #region Fields
        protected internal RwLockedDictionary<UUID, ObjectPart> m_Primitives = new RwLockedDictionary<UUID,ObjectPart>();
        protected internal RwLockedDictionary<UUID, IObject> m_Objects = new RwLockedDictionary<UUID, IObject>();
        protected internal RwLockedDictionary<UUID, ParcelInfo> m_Parcels = new RwLockedDictionary<UUID, ParcelInfo>();
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
            PresenceServiceInterface presenceService,
            AvatarServiceInterface avatarService,
            GroupsServiceInterface groupsService,
            AssetServiceInterface assetService,
            GridServiceInterface gridService,
            GridUserServiceInterface gridUserService,
            RegionInfo ri)
        {
            m_UDPServer = new LLUDPServer(new IPAddress(0), (int)ri.ServerPort, imService, chatService, this);
            PresenceService = presenceService;
            AvatarService = avatarService;
            GroupsService = groupsService;
            AssetService = assetService;
            GridService = gridService;
            GridUserService = gridUserService;
            Terrain = new TerrainMap(ri.Size.X, ri.Size.Y);
            m_SceneObjects = new BasicSceneObjects(this);
            m_SceneObjectParts = new BasicSceneObjectParts(this);
            m_SceneObjectGroups = new DefaultSceneObjectGroupInterface(this);
            m_SceneAgents = new DefaultSceneAgentInterface(this);
            m_SceneParcels = new BasicSceneParcels(this);
            ID = ri.ID;
            Name = ri.Name;
            GridPosition = ri.Location;
            SizeX = ri.Size.X;
            SizeY = ri.Size.Y;
            m_ChatService = chatService;
            IMRouter.SceneIM.Add(IMSend);
            OnRemove += RemoveScene;
            ExternalHostName = ri.ServerIP;
            RegionPort = ri.ServerPort;
            m_UDPServer.Start();
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
                List<UUID> removeAgain = new List<UUID>();

                try
                {
                    foreach (ObjectPart objpart in objgroup.Values)
                    {
                        m_Primitives.Add(objpart.ID, objpart);
                        removeAgain.Add(objpart.ID);
                    }
                    m_Objects.Add(objgroup.ID, objgroup);
                }
                catch
                {
                    foreach (UUID objpart in removeAgain)
                    {
                        m_Primitives.Remove(objpart);
                    }
                }
            }
            else
            {
                m_Objects.Add(obj.ID, obj);
            }
        }

        public override bool Remove(IObject obj)
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
                }
                m_Objects.Remove(objgroup.ID);
            }
            else
            {
                m_Objects.Remove(obj.ID);
            }

            return true;
        }
        #endregion

        #region Scene LL Message interface
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
        #endregion
    }
}
