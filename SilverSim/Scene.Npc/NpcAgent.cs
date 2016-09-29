// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Neighbor;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages.Agent;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Scene.Npc
{
    public partial class NpcAgent : Agent.Agent
    {
        public override event Action<IObject> OnPositionChange;

        InventoryServiceInterface m_InventoryService = null;
        ProfileServiceInterface m_ProfileService = null;
        GridUserServiceInterface m_GridUserService = null;
        PresenceServiceInterface m_PresenceService = null;

        public NpcAgent(
            UUID agentId,
            string firstName,
            string lastName,
            Uri homeURI,
            AgentServiceList serviceList)
            : base(agentId, homeURI)
        {
            FirstName = firstName;
            LastName = lastName;
            m_InventoryService = serviceList.Get<InventoryServiceInterface>();
            m_ProfileService = serviceList.Get<ProfileServiceInterface>();
            m_GridUserService = serviceList.Get<GridUserServiceInterface>();
            m_PresenceService = serviceList.Get<PresenceServiceInterface>();
        }

        UUI m_NpcOwner = UUI.Unknown;

        /* as the Npc must own itself, we actually have to provide a separate means to declare a NPC owner */
        public UUI NpcOwner
        {
            get
            {
                lock(this)
                {
                    return new UUI(m_NpcOwner);
                }
            }
            set
            {
                lock(this)
                {
                    m_NpcOwner = new UUI(value);
                }
            }
        }

        readonly RwLockedDictionary<UUID, AgentChildInfo> m_ActiveChilds = new RwLockedDictionary<UUID, AgentChildInfo>();
        public override RwLockedDictionary<UUID, AgentChildInfo> ActiveChilds
        {
            get
            {
                return m_ActiveChilds;
            }
        }

        public override IAgentTeleportServiceInterface ActiveTeleportService
        {
            get
            {
                return null;
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override AssetServiceInterface AssetService
        {
            get
            {
                return CurrentScene.AssetService;
            }
        }

        public override Vector3 CameraAtAxis
        {
            get
            {
                return Vector3.UnitX;
            }

            set
            {
                /* ignore */
            }
        }

        public override Vector3 CameraLeftAxis
        {
            get
            {
                return Vector3.UnitX;
            }

            set
            {
                /* ignore */
            }
        }

        public override Vector3 CameraPosition
        {
            get
            {
                return Vector3.Zero;
            }

            set
            {
                /* ignore */
            }
        }

        public override Quaternion CameraRotation
        {
            get
            {
                return Quaternion.Identity;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override Vector3 CameraUpAxis
        {
            get
            {
                return Vector3.UnitX;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override ClientInfo Client
        {
            get
            {
                ClientInfo cInfo = new ClientInfo();
                cInfo.Channel = VersionInfo.ProductName;
                cInfo.ClientIP = string.Empty;
                cInfo.ClientVersion = VersionInfo.Version;
                cInfo.ID0 = string.Empty;
                cInfo.Mac = string.Empty;
                return cInfo;
            }
        }

        public override DetectedTypeFlags DetectedType
        {
            get
            {
                return (SittingOnObject != null) ?
                    (DetectedTypeFlags.Npc | DetectedTypeFlags.Passive) :
                    (DetectedTypeFlags.Npc | DetectedTypeFlags.Active);
            }
        }

        public override EconomyServiceInterface EconomyService
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override FriendsServiceInterface FriendsService
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override GridUserServiceInterface GridUserService
        {
            get
            {
                if(m_GridUserService == null)
                {
                    throw new NotSupportedException();
                }
                return m_GridUserService;
            }
        }

        public override GroupsServiceInterface GroupsService
        {
            get
            {
                return CurrentScene.GroupsService;
            }
        }

        public override InventoryServiceInterface InventoryService
        {
            get
            {
                if(m_InventoryService == null)
                {
                    throw new NotSupportedException();
                }
                return m_InventoryService;
            }
        }

        public override bool IsActiveGod
        {
            get
            {
                /* NPCs are never gods */
                return false;
            }
        }

        public override bool IsInMouselook
        {
            get
            {
                /* NPCs do not have this kind of distinguishing view mode */
                return false;
            }
        }

        public override bool IsNpc
        {
            get
            {
                return true;
            }
        }

        public override RwLockedDictionary<UUID, FriendStatus> KnownFriends
        {
            get
            {
                return new RwLockedDictionary<UUID, FriendStatus>();
            }
        }

        public override int LastMeasuredLatencyTickCount
        {
            get
            {
                return 0;
            }

            set
            {
                throw new NotSupportedException("LastMeasuredLatencyTickCount");
            }
        }

        public override OfflineIMServiceInterface OfflineIMService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #region Physics Linkage
        readonly RwLockedDictionary<UUID, IPhysicsObject> m_PhysicsActors = new RwLockedDictionary<UUID, IPhysicsObject>();

        public override RwLockedDictionary<UUID, IPhysicsObject> PhysicsActors
        {
            get
            {
                return m_PhysicsActors;
            }
        }

        public override IPhysicsObject PhysicsActor
        {
            get
            {
                lock (m_DataLock)
                {
                    IPhysicsObject obj;
                    if (!PhysicsActors.TryGetValue(SceneID, out obj))
                    {
                        obj = DummyAgentPhysicsObject.SharedInstance;
                    }
                    return obj;
                }
            }
        }

        #endregion

        public override PresenceServiceInterface PresenceService
        {
            get
            {
                if(null == m_PresenceService)
                {
                    throw new NotSupportedException();
                }
                return m_PresenceService;
            }
        }

        public override ProfileServiceInterface ProfileService
        {
            get
            {
                if(m_ProfileService == null)
                {
                    throw new NotSupportedException();
                }
                return m_ProfileService;
            }
        }

        public override SessionInfo Session
        {
            get
            {
                SessionInfo sInfo = new SessionInfo();
                sInfo.SessionID = UUID.Zero;
                sInfo.SecureSessionID = UUID.Zero;
                sInfo.ServiceSessionID = string.Empty;
                return sInfo;
            }
        }

        public override List<GridType> SupportedGridTypes
        {
            get
            {
                return new List<GridType>();
            }
        }

        RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> m_TransmittedTerrainSerials = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>>(delegate () { return new RwLockedDictionary<uint, uint>(); });
        public override RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> TransmittedTerrainSerials
        {
            get
            {
                return m_TransmittedTerrainSerials;
            }
        }

        public override UserAccount UntrustedAccountInfo
        {
            get
            {
                UserAccount acc = new UserAccount();
                acc.Principal = Owner;
                acc.IsLocalToGrid = true;
                acc.ScopeID = UUID.Zero;
                acc.UserLevel = 0;
                return acc;
            }
        }

        public override UserAgentServiceInterface UserAgentService
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ulong AddNewFile(string filename, byte[] data)
        {
            throw new NotSupportedException("AddNewFile");
        }

        public override void AddWaitForRoot(SceneInterface scene, Action<object, bool> del, object o)
        {
            /* ignored */
        }

        public override void ClearKnownFriends()
        {
            /* ignored */
        }

        public override void EnableSimulator(UUID originSceneID, uint circuitCode, string capsURI, DestinationInfo destinationInfo)
        {
            /* ignored */
        }

        public override void HandleMessage(ChildAgentPositionUpdate m)
        {
            /* ignored */
        }

        public override void HandleMessage(ChildAgentUpdate m)
        {
            /* ignored */
        }

        public override bool IMSend(GridInstantMessage im)
        {
            throw new NotImplementedException();
        }

        public override void ReleaseControls(ScriptInstance instance)
        {
            throw new NotImplementedException();
        }

        public override void RemoveActiveTeleportService(IAgentTeleportServiceInterface service)
        {
            throw new NotImplementedException();
        }

        public override ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions)
        {
            throw new NotImplementedException();
        }

        public override ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions, UUID experienceID)
        {
            throw new NotImplementedException();
        }

        public override void RevokePermissions(UUID sourceID, UUID itemID, ScriptPermissions permissions)
        {
            RevokeAnimPermissions(sourceID, permissions);
        }

        readonly RwLockedList<UUID> m_SelectedObjects = new RwLockedList<UUID>();
        public override RwLockedList<UUID> SelectedObjects(UUID scene)
        {
            return m_SelectedObjects;
        }

        public override void TakeControls(ScriptInstance instance, int controls, int accept, int pass_on)
        {
            throw new NotImplementedException();
        }

        public override bool TeleportHome(SceneInterface sceneInterface)
        {
            throw new NotImplementedException();
        }

        public override bool TeleportTo(SceneInterface sceneInterface, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            throw new NotImplementedException();
        }

        public override bool TeleportTo(SceneInterface sceneInterface, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            throw new NotImplementedException();
        }

        public override bool TeleportTo(SceneInterface sceneInterface, string regionName, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            throw new NotImplementedException();
        }

        public override bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            throw new NotImplementedException();
        }

        public override bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            throw new NotImplementedException();
        }

        public override bool UnSit()
        {
            throw new NotImplementedException();
        }

        public override void InvokeOnPositionUpdate()
        {
            var e = OnPositionChange; /* events are not exactly thread-safe, so copy the reference first */
            if (e != null)
            {
                foreach (Action<IObject> del in e.GetInvocationList().OfType<Action<IObject>>())
                {
                    del(this);
                }
            }

            SceneInterface currentScene = CurrentScene;
            if(null != currentScene)
            {
                currentScene.SendAgentObjectToAllAgents(this);
            }
        }

    }
}
