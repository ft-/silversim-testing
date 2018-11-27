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
using SilverSim.Scene.ServiceInterfaces.Chat;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Neighbor;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.MuteList;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.ServiceInterfaces.UserSession;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Grid;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages.Agent;
using System;
using System.Collections.Generic;

namespace SilverSim.Scene.Npc
{
    public partial class NpcAgent : Agent.Agent, ILocalIDAccessor
    {
        private static readonly ILog m_Log = LogManager.GetLogger("NPC AGENT");

        private readonly InventoryServiceInterface m_InventoryService;
        private readonly ProfileServiceInterface m_ProfileService;
        private readonly IPresenceServiceInterface m_PresenceService;

        private ChatServiceInterface m_ChatService;
        private ChatServiceInterface.Listener m_ChatListener;

        public override bool IsRunning => false;

        public override bool IsFlying => false; /* implement controls */

        public override bool IsAway => false;

        private UUID GetMyUUID() => ID;

        private Vector3 GetMyPosition() => GlobalPosition;

        internal void EnableListen()
        {
            if (m_ChatListener == null)
            {
                try
                {
                    m_ChatService = CurrentScene.GetService<ChatServiceInterface>();
                    m_ChatListener = m_ChatService.AddAgentListen(0, string.Empty, UUID.Zero, string.Empty, GetMyUUID, GetMyPosition, OnChatReceive);
                }
                catch
                {
                    /* intentionally ignored */
                }
            }
        }

        internal void DisableListen()
        {
            if (m_ChatListener != null)
            {
                m_ChatListener.Remove();
                m_ChatListener = null;
            }
        }

        public NpcAgent(
            UGUIWithName npcID,
            AgentServiceList serviceList)
            : base(npcID.ID, npcID.HomeURI)
        {
            FirstName = npcID.FirstName;
            LastName = npcID.LastName;
            m_InventoryService = serviceList.Get<InventoryServiceInterface>();
            m_ProfileService = serviceList.Get<ProfileServiceInterface>();
            m_PresenceService = serviceList.Get<IPresenceServiceInterface>();
            NpcPresenceService = serviceList.Get<NpcPresenceServiceInterface>();
            m_UpdateInfo = new AgentUpdateInfo(this, UUID.Zero);
            OnAppearanceUpdate += HandleAppearanceUpdate;
        }

        ~NpcAgent()
        {
            OnAppearanceUpdate -= HandleAppearanceUpdate;
        }

        private void HandleAppearanceUpdate(IAgent agent)
        {
            CurrentScene?.SendAgentAppearanceToAllAgents(this);
        }

        private UGUI m_NpcOwner = UGUI.Unknown;

        /* as the Npc must own itself, we actually have to provide a separate means to declare a NPC owner */
        public UGUI NpcOwner
        {
            get
            {
                return new UGUI(m_NpcOwner);
            }
            set
            {
                m_NpcOwner = new UGUI(value);
            }
        }

        public override RwLockedDictionary<UUID, AgentChildInfo> ActiveChilds { get; } = new RwLockedDictionary<UUID, AgentChildInfo>();

        public override IAgentTeleportServiceInterface ActiveTeleportService
        {
            get { return null; }

            set { throw new NotSupportedException(); }
        }

        public override AssetServiceInterface AssetService => CurrentScene.AssetService;

        public override Vector3 CameraAtAxis
        {
            get { return Vector3.UnitX; }

            set
            {
                /* ignore */
            }
        }

        public override Vector3 CameraLeftAxis
        {
            get { return Vector3.UnitX; }

            set
            {
                /* ignore */
            }
        }

        public override Vector3 CameraPosition
        {
            get { return Vector3.Zero; }

            set
            {
                /* ignore */
            }
        }

        public override Quaternion CameraRotation
        {
            get { return Quaternion.Identity; }

            set { throw new NotImplementedException(); }
        }

        public override Vector3 CameraUpAxis
        {
            get { return Vector3.UnitX; }

            set { throw new NotImplementedException(); }
        }

        public override ClientInfo Client => new ClientInfo
        {
            Channel = VersionInfo.ProductName,
            ClientIP = string.Empty,
            ClientVersion = VersionInfo.Version,
            ID0 = string.Empty,
            Mac = string.Empty
        };

        public override DetectedTypeFlags DetectedType => (SittingOnObject != null) ?
                    (DetectedTypeFlags.Npc | DetectedTypeFlags.Passive) :
                    (DetectedTypeFlags.Npc | DetectedTypeFlags.Active);

        public override EconomyServiceInterface EconomyService
        {
            get { throw new NotSupportedException(); }
        }

        public override FriendsServiceInterface FriendsService
        {
            get { throw new NotSupportedException(); }
        }

        public NpcPresenceServiceInterface NpcPresenceService { get; }

        public override GroupsServiceInterface GroupsService => CurrentScene.GroupsService;

        public override ExperienceServiceInterface ExperienceService => CurrentScene.ExperienceService;

        public override InventoryServiceInterface InventoryService
        {
            get
            {
                if (m_InventoryService == null)
                {
                    throw new NotSupportedException();
                }
                return m_InventoryService;
            }
        }

        public override bool IsActiveGod => false;

        public override bool IsInMouselook => false;

        public override bool IsNpc => true;

        public override RwLockedDictionary<UUID, FriendStatus> KnownFriends => new RwLockedDictionary<UUID, FriendStatus>();

        public override int LastMeasuredLatencyMsecs
        {
            get { return 0; }
        }

        public override OfflineIMServiceInterface OfflineIMService
        {
            get { throw new NotSupportedException(); }
        }

        public override MuteListServiceInterface MuteListService
        {
            get { throw new NotSupportedException(); }
        }

        #region Physics Linkage

        public override RwLockedDictionary<UUID, IPhysicsObject> PhysicsActors { get; } = new RwLockedDictionary<UUID, IPhysicsObject>();

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

        public override IPresenceServiceInterface PresenceService
        {
            get
            {
                if (m_PresenceService == null)
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
                if (m_ProfileService == null)
                {
                    throw new NotSupportedException();
                }
                return m_ProfileService;
            }
        }

        public override SessionInfo Session => new SessionInfo
        {
            SessionID = UUID.Zero,
            SecureSessionID = UUID.Zero,
        };

        public override List<GridType> SupportedGridTypes => new List<GridType>();
        public override RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> TransmittedTerrainSerials { get; } = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>>(() => new RwLockedDictionary<uint, uint>());

        private readonly AgentUpdateInfo m_UpdateInfo;

        uint ILocalIDAccessor.this[UUID sceneID]
        {
            get
            {
                return sceneID == SceneID ? m_UpdateInfo.LocalID : 0;
            }
            set
            {
                if(sceneID == SceneID)
                {
                    m_UpdateInfo.LocalID = value;
                }
            }
        }

        public override ILocalIDAccessor LocalID => this;

        public override AgentUpdateInfo GetUpdateInfo(UUID sceneID)
        {
            return (sceneID == SceneID) ? m_UpdateInfo : null;
        }

        public override void SendKillObject(UUID sceneID)
        {
            m_UpdateInfo.KillObject();
            CurrentScene?.ScheduleUpdate(m_UpdateInfo);
        }

        public override UserAccount UntrustedAccountInfo => new UserAccount
        {
            Principal = NamedOwner,
            IsLocalToGrid = true,
            UserLevel = 0
        };

        public override UserAgentServiceInterface UserAgentService
        {
            get { throw new NotImplementedException(); }
        }

        public override ulong AddNewFile(string filename, byte[] data)
        {
            throw new NotSupportedException("AddNewFile");
        }

        public override void AddWaitForRoot(SceneInterface scene, Action<object, bool> del, object o)
        {
            /* ignored */
        }

        protected override void DieAgent()
        {
#warning Implement NPC agent death
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

        public override bool WaitsForExperienceResponse(ObjectPart part, UUID itemID)
        {
            return true;
        }

        private readonly RwLockedList<UUID> m_SelectedObjects = new RwLockedList<UUID>();
        public override RwLockedList<UUID> SelectedObjects(UUID scene) => m_SelectedObjects;

        public override void TakeControls(ScriptInstance instance, int controls, int accept, int pass_on)
        {
            throw new NotImplementedException();
        }


        public override List<AgentControlData> ActiveControls => new List<AgentControlData>();

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

        public override void InvokeOnPositionUpdate()
        {
            base.InvokeOnPositionUpdate();

            CurrentScene?.SendAgentObjectToAllAgents(this);
        }

        public override bool OwnsAssetID(UUID id) => false; /* NPCs are dealt with on scene's asset service */
    }
}
