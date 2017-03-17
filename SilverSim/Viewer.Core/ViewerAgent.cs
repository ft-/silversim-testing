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
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Neighbor;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
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
using SilverSim.Types.Friends;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using SilverSim.Types.Parcel;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Agent;
using SilverSim.Viewer.Messages.Avatar;
using SilverSim.Viewer.Messages.Parcel;
using SilverSim.Viewer.Messages.Script;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace SilverSim.Viewer.Core
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    public partial class ViewerAgent : SilverSim.Scene.Agent.Agent
    {
        private static readonly ILog m_Log = LogManager.GetLogger("VIEWER AGENT");
        readonly SceneList m_Scenes;

        #region Agent fields
        private UUID m_CurrentSceneID;
        #endregion

        readonly RwLockedDictionary<UUID, AgentChildInfo> m_ActiveChilds = new RwLockedDictionary<UUID, AgentChildInfo>();

        /** <summary>Key is region ID</summary> */
        public override RwLockedDictionary<UUID, AgentChildInfo> ActiveChilds
        {
            get
            {
                return m_ActiveChilds;
            }
        }

        readonly ClientInfo m_ClientInfo;
        public override ClientInfo Client 
        { 
            get
            {
                return m_ClientInfo;
            }
        }

        readonly UserAccount m_UntrustedAccountInfo;
        public override UserAccount UntrustedAccountInfo
        { 
            get
            {
                return new UserAccount(m_UntrustedAccountInfo);
            }
        }

        public override SessionInfo Session 
        {
            get
            {
                SessionInfo info = new SessionInfo();
                info.SessionID = SessionID;
                info.SecureSessionID = m_SecureSessionID;
                info.ServiceSessionID = m_ServiceSessionID;
                return info;
            }
        }

        public override List<GridType> SupportedGridTypes 
        {
            get
            {
                List<GridType> gridTypes = new List<GridType>();
                foreach (IAgentTeleportServiceInterface agentteleport in m_TeleportServices)
                {
                    gridTypes.Add(agentteleport.GridType);
                }
                return gridTypes;
            }
        }

        IAgentTeleportServiceInterface m_ActiveTeleportService;
        public override IAgentTeleportServiceInterface ActiveTeleportService
        {
            get
            {
                return m_ActiveTeleportService;
            }
            set
            {
                lock(m_DataLock)
                {
                    if(m_ActiveTeleportService != null && value != null)
                    {
                        throw new InvalidOperationException();
                    }
                    m_ActiveTeleportService = value;
                }
            }
        }

        public override void RemoveActiveTeleportService(IAgentTeleportServiceInterface service)
        {
            lock(m_DataLock)
            {
                if(m_ActiveTeleportService == service)
                {
                    m_ActiveTeleportService = null;
                }
            }
        }

        #region ViewerAgent Properties
        public UUID SessionID { get; private set; }
        public double DrawDistance { get; private set; }
        #endregion

        /* Circuits: UUID is SceneID */
        public readonly RwLockedDictionary<UUID, AgentCircuit> Circuits = new RwLockedDictionary<UUID, AgentCircuit>();
        public readonly RwLockedDictionary<GridVector, string> KnownChildAgentURIs = new RwLockedDictionary<GridVector, string>();

        private readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> m_TransmittedTerrainSerials = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>>(delegate() { return new RwLockedDictionary<uint, uint>(); });

        public override RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> TransmittedTerrainSerials
        {
            get
            {
                return m_TransmittedTerrainSerials;
            }
        }

        #region IObject Calls
        public override void InvokeOnPositionUpdate()
        {
            base.InvokeOnPositionUpdate();

            AgentCircuit c;
            if(Circuits.TryGetValue(SceneID, out c))
            {
                c.Scene.SendAgentObjectToAllAgents(this);
            }
        }
        #endregion

        #region IObject Properties

        public override bool IsInScene(SceneInterface scene)
        {
            lock (m_DataLock)
            {
                return SceneID == scene.ID;
            }
        }

        public override UUID SceneID
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_CurrentSceneID;
                }
            }
            set
            {
                IList<KeyValuePair<Action<object, bool>, object>> waitForRootList;
                AgentCircuit circuit;
                IPhysicsObject physAgent;
                if(PhysicsActors.TryGetValue(m_CurrentSceneID, out physAgent) && m_CurrentSceneID != value)
                {
                    physAgent.IsPhysicsActive = false;
                }
                if(!Circuits.TryGetValue(value, out circuit))
                {
                    circuit = null;
                }
                lock (m_DataLock)
                {
                    m_CurrentSceneID = value;
                }

                if (circuit != null)
                {
                    waitForRootList = circuit.WaitForRootList.GetAndClear();
                    foreach (KeyValuePair<Action<object, bool>, object> kvp in waitForRootList)
                    {
                        kvp.Key(kvp.Value, true);
                    }
                }
                if (PhysicsActors.TryGetValue(m_CurrentSceneID, out physAgent))
                {
                    physAgent.IsPhysicsActive = true;
                }
            }
        }

        public override void AddWaitForRoot(SceneInterface scene, Action<object, bool> del, object o)
        {
            AgentCircuit circuit;
            if (Circuits.TryGetValue(scene.ID, out circuit))
            {
                if (IsInScene(scene))
                {
                    del(o, true);
                }
                else
                {
                    circuit.WaitForRootList.Add(new KeyValuePair<Action<object, bool>, object>(del, o));
                }
            }
            else
            {
                del(o, false);
            }
        }
        #endregion

        #region IAgent Properties
        public override bool IsNpc
        {
            get
            {
                return false;
            }
        }

        bool m_IsInMouselook;

        public override bool IsInMouselook
        {
            get
            {
                return m_IsInMouselook;
            }
        }


        readonly RwLockedDictionary<UUID, FriendStatus> m_KnownFriends = new RwLockedDictionary<UUID, FriendStatus>();
        bool m_KnownFriendsCached;
        readonly object m_KnownFriendsCacheLock = new object();

        void CacheFriends()
        {
            lock (m_KnownFriendsCacheLock)
            {
                if (!m_KnownFriendsCached)
                {
                    if (FriendsService != null)
                    {
                        if (m_KnownFriends.Count == 0)
                        {
                            foreach (FriendInfo fi in FriendsService[Owner])
                            {
                                m_KnownFriends.Add(fi.Friend.ID, new FriendStatus(fi));
                            }
                        }
                    }
                    else
                    {
                        /* if we have already some entries, we keep the ones that are still valid */
                        List<UUID> haveIDs = new List<UUID>(m_KnownFriends.Keys);
                        foreach (FriendInfo fi in FriendsService[Owner])
                        {
                            FriendStatus fStat;
                            if (m_KnownFriends.TryGetValue(fi.Friend.ID, out fStat))
                            {
                                fStat.FriendGivenFlags = fi.FriendGivenFlags;
                                fStat.UserGivenFlags = fi.UserGivenFlags;
                            }
                            else
                            {
                                m_KnownFriends.Add(fi.Friend.ID, new FriendStatus(fi));
                            }
                            haveIDs.Remove(fi.Friend.ID);
                        }

                        foreach(UUID id in haveIDs)
                        {
                            m_KnownFriends.Remove(id);
                        }
                    }
                    m_KnownFriendsCached = true;
                }
            }
        }

        public override RwLockedDictionary<UUID, FriendStatus> KnownFriends
        {
            get
            {
                /* on-demand caching */
                if (!m_KnownFriendsCached)
                {
                    CacheFriends();
                }
                return m_KnownFriends;
            }
        }

        public override void ClearKnownFriends()
        {
            lock(m_KnownFriendsCacheLock)
            {
                m_KnownFriendsCached = false;
            }
        }

        public Dictionary<string, string> ServiceURLs = new Dictionary<string, string>();

        private bool m_IsActiveGod;

        public override bool IsActiveGod
        {
            get
            {
                return m_IsActiveGod;
            }
        }

        public override int LastMeasuredLatencyTickCount /* info from Circuit ping measurement */
        {
            get;
            set;
        }

        public override AssetServiceInterface AssetService
        {
            get
            {
                return m_AssetService;
            }
        }

        public override InventoryServiceInterface InventoryService
        {
            get
            {
                return m_InventoryService;
            }
        }

        public override OfflineIMServiceInterface OfflineIMService
        {
            get
            {
                return m_OfflineIMService;
            }
        }

        public override GroupsServiceInterface GroupsService
        {
            get
            {
                return m_GroupsService;
            }
        }

        public override ProfileServiceInterface ProfileService
        {
            get
            {
                return m_ProfileService;
            }
        }

        public override FriendsServiceInterface FriendsService
        {
            get
            {
                return m_FriendsService;
            }
        }

        public override UserAgentServiceInterface UserAgentService
        {
            get
            {
                return m_UserAgentService;
            }
        }

        public override PresenceServiceInterface PresenceService
        {
            get
            {
                return m_PresenceService;
            }
        }

        public override GridUserServiceInterface GridUserService
        {
            get
            {
                return m_GridUserService;
            }
        }

        public override EconomyServiceInterface EconomyService
        {
            get
            {
                return m_EconomyService;
            }
        }
        #endregion

        #region IAgent Methods
        public override bool IMSend(GridInstantMessage gim)
        {
            AgentCircuit c;
            UUID sceneID = SceneID;
            if (Circuits.TryGetValue(sceneID, out c))
            {
                Messages.IM.ImprovedInstantMessage im = new Messages.IM.ImprovedInstantMessage(gim);
                if (gim.IsSystemMessage)
                {
                    /* this is a system message, so we change its sender name */
                    im.FromAgentName = "System";
                    im.RegionID = UUID.Zero;
                    im.ParentEstateID = 0;
                    im.Position = Vector3.Zero;
                }
                SendMessageAlways(im, sceneID);
                return true;
            }
            return false;
        }
        #endregion

        public override DetectedTypeFlags DetectedType
        {
            get
            {
                return (SittingOnObject != null) ? 
                    (DetectedTypeFlags.Agent | DetectedTypeFlags.Passive) :
                    (DetectedTypeFlags.Agent | DetectedTypeFlags.Active);
            }
        }

        #region Fields

        private AssetServiceInterface m_AssetService;
        private InventoryServiceInterface m_InventoryService;
        private GroupsServiceInterface m_GroupsService;
        private ProfileServiceInterface m_ProfileService;
        private FriendsServiceInterface m_FriendsService;
        private UserAgentServiceInterface m_UserAgentService;
        private PresenceServiceInterface m_PresenceService;
        private GridUserServiceInterface m_GridUserService;
        private GridServiceInterface m_GridService;
        readonly EconomyServiceInterface m_EconomyService;
        readonly OfflineIMServiceInterface m_OfflineIMService;

        #endregion

        readonly UUID m_SecureSessionID;
        readonly string m_ServiceSessionID;
        readonly List<IAgentTeleportServiceInterface> m_TeleportServices;

        void CloseAllCircuits(bool result)
        {
            foreach(AgentChildInfo info in ActiveChilds.Values)
            {
                info.ChildAgentUpdateService.Disconnect();
            }
            foreach(Circuit circ in Circuits.Values)
            {
                circ.Stop();
            }
        }

        public override void KickUser(string msg)
        {
            Messages.User.KickUser req = new Messages.User.KickUser();
            req.AgentID = Owner.ID;
            req.SessionID = SessionID;
            req.Message = msg;
            req.OnSendCompletion += CloseAllCircuits;
            SendMessageAlways(req, m_CurrentSceneID);
        }

        public override void KickUser(string msg, Action<bool> callbackDelegate)
        {
            Messages.User.KickUser req = new Messages.User.KickUser();
            req.OnSendCompletion += callbackDelegate;
            req.OnSendCompletion += CloseAllCircuits;
            req.AgentID = Owner.ID;
            req.SessionID = SessionID;
            req.Message = msg;
            SendMessageAlways(req, m_CurrentSceneID);
        }

        public override bool TeleportTo(SceneInterface sceneInterface, string regionName, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            foreach(IAgentTeleportServiceInterface service in m_TeleportServices)
            {
#if DEBUG
                m_Log.DebugFormat("Checking Teleport Service {0} for {1}", service.GetType().ToString(), Owner.FullName.ToString());
#endif

                if (service.TeleportTo(sceneInterface, this, regionName, position, lookAt, flags))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool TeleportTo(SceneInterface sceneInterface, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            foreach (IAgentTeleportServiceInterface service in m_TeleportServices)
            {
#if DEBUG
                m_Log.DebugFormat("Checking Teleport Service {0} for {1}", service.GetType().ToString(), Owner.FullName.ToString());
#endif
                if (service.TeleportTo(sceneInterface, this, location, position, lookAt, flags))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            foreach (IAgentTeleportServiceInterface service in m_TeleportServices)
            {
#if DEBUG
                m_Log.DebugFormat("Checking Teleport Service {0} for {1}", service.GetType().ToString(), Owner.FullName.ToString());
#endif
                if (service.TeleportTo(sceneInterface, this, gatekeeperURI, location, position, lookAt, flags))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool TeleportTo(SceneInterface sceneInterface, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            foreach (IAgentTeleportServiceInterface service in m_TeleportServices)
            {
#if DEBUG
                m_Log.DebugFormat("Checking Teleport Service {0} for {1}", service.GetType().ToString(), Owner.FullName.ToString());
#endif
                if (service.TeleportTo(sceneInterface, this, regionID, position, lookAt, flags))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            foreach (IAgentTeleportServiceInterface service in m_TeleportServices)
            {
#if DEBUG
                m_Log.DebugFormat("Checking Teleport Service {0} for {1}", service.GetType().ToString(), Owner.FullName.ToString());
#endif
                if (service.TeleportTo(sceneInterface, this, gatekeeperURI, regionID, position, lookAt, flags))
                {
                    return true;
                }
            }
            return false;
        }

        /* following function returns true if it accepts a teleport request or if it wants to distribute more specific error message except home location not available */
        public override bool TeleportHome(SceneInterface sceneInterface)
        {
            foreach(IAgentTeleportServiceInterface service in m_TeleportServices)
            {
#if DEBUG
                m_Log.DebugFormat("Checking Teleport Service {0} for {1}", service.GetType().ToString(), Owner.FullName.ToString());
#endif
                if (service.TeleportHome(sceneInterface, this))
                {
                    return true;
                }
            }
            return false;
        }

        public ViewerAgent(
            SceneList scenes,
            UUID agentID,
            string firstName,
            string lastName,
            Uri homeURI,
            UUID sessionID,
            UUID secureSessionID,
            string serviceSessionID,
            ClientInfo clientInfo,
            UserAccount untrustedAccountInfo,
            AgentServiceList serviceList)
            : base(agentID, homeURI)
        {
            m_Scenes = scenes;
            m_TeleportServices = serviceList.GetAll<IAgentTeleportServiceInterface>();
            CollisionPlane = Vector4.UnitW;
            SessionID = sessionID;
            m_UntrustedAccountInfo = untrustedAccountInfo;
            m_SecureSessionID = secureSessionID;
            m_ServiceSessionID = serviceSessionID;
            m_ClientInfo = clientInfo;
            m_AssetService = serviceList.Get<AssetServiceInterface>();
            m_InventoryService = serviceList.Get<InventoryServiceInterface>();
            m_GroupsService = serviceList.Get<GroupsServiceInterface>();
            m_ProfileService = serviceList.Get<ProfileServiceInterface>();
            m_FriendsService = serviceList.Get<FriendsServiceInterface>();
            m_UserAgentService = serviceList.Get<UserAgentServiceInterface>();
            m_PresenceService = serviceList.Get<PresenceServiceInterface>();
            m_GridUserService = serviceList.Get<GridUserServiceInterface>();
            m_GridService = serviceList.Get<GridServiceInterface>();
            m_EconomyService = serviceList.Get<EconomyServiceInterface>();
            m_OfflineIMService = serviceList.Get<OfflineIMServiceInterface>();
            FirstName = firstName;
            LastName = lastName;
            if (m_EconomyService != null)
            {
                m_EconomyService.Login(Owner, SessionID, m_SecureSessionID);
            }
            OnPositionChange += ChildUpdateOnPositionChange;
        }

        ~ViewerAgent()
        {
            OnPositionChange -= ChildUpdateOnPositionChange;
            lock (m_DataLock)
            {
                DetachAllAttachments();
                if (m_EconomyService != null)
                {
                    m_EconomyService.Logout(Owner, SessionID, m_SecureSessionID);
                }
                m_AssetService = null;
                m_InventoryService = null;
                m_GroupsService = null;
                m_ProfileService = null;
                m_FriendsService = null;
                m_UserAgentService = null;
                m_PresenceService = null;
                m_GridUserService = null;
                m_GridService = null;
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
                lock(m_DataLock)
                {
                    IPhysicsObject obj;
                    if(!PhysicsActors.TryGetValue(SceneID, out obj))
                    {
                        obj = DummyAgentPhysicsObject.SharedInstance;
                    }
                    return obj;
                }
            }
        }

        #endregion

        public override RwLockedList<UUID> SelectedObjects(UUID scene)
        {
            AgentCircuit circuit;
            return (Circuits.TryGetValue(scene, out circuit)) ?
                circuit.SelectedObjects :
                new RwLockedList<UUID>();
        }

        public override ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions)
        {
            return RequestPermissions(part, itemID, permissions, UUID.Zero);
        }

        public override ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions, UUID experienceID)
        {
            ScriptPermissions autoGrant = ScriptPermissions.None;
            if (SittingOnObject.ID == itemID || part.ObjectGroup.AttachPoint != Types.Agent.AttachmentPoint.NotAttached)
            {
                autoGrant |= ScriptPermissions.ControlCamera;
                autoGrant |= ScriptPermissions.TakeControls;
                autoGrant |= ScriptPermissions.TrackCamera;
            }
            if(part.ObjectGroup.AttachPoint != Types.Agent.AttachmentPoint.NotAttached)
            {
                autoGrant |= ScriptPermissions.OverrideAnimations;
                autoGrant |= ScriptPermissions.Attach;
            }
            if((permissions & autoGrant) == permissions)
            {
                return permissions;
            }
            ScriptQuestion m = new ScriptQuestion();
            m.ExperienceID = experienceID;
            m.ItemID = itemID;
            m.ObjectName = part.ObjectGroup.Name;
            m.ObjectOwner = part.Owner.FullName;
            m.Questions = permissions;
            m.TaskID = part.ID;
            SendMessageAlways(m, part.ObjectGroup.Scene.ID);
            return ScriptPermissions.None;
        }

        public override void RevokePermissions(UUID sourceID, UUID itemID, ScriptPermissions permissions)
        {
            RevokeAnimPermissions(sourceID, permissions);
        }

        [PacketHandler(MessageType.RegionHandshakeReply)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleRegionHandshakeReply(Message m)
        {
            Messages.Region.RegionHandshakeReply rhr = (Messages.Region.RegionHandshakeReply)m;
            AgentCircuit circuit;
            if (Circuits.TryGetValue(rhr.CircuitSceneID, out circuit))
            {
                SceneInterface scene = circuit.Scene;
                /* Add our agent to scene */
                scene.SendAllParcelOverlaysTo(this);
                scene.Terrain.UpdateTerrainDataToSingleClient(this, true);
                scene.Environment.UpdateWindDataToSingleClient(this);
                scene.SendAgentObjectToAllAgents(this);
                scene.SendRegionInfo(this);
                ParcelInfo pinfo;
                if(scene.Parcels.TryGetValue(GlobalPosition, out pinfo))
                {
                    ParcelProperties props = scene.ParcelInfo2ParcelProperties(Owner.ID, pinfo, NextParcelSequenceId, ParcelProperties.RequestResultType.Single);
                    circuit.SendMessage(props);
                }
                circuit.ScheduleFirstUpdate();
                SendAnimations();
            }
        }

        public override void SendUpdatedParcelInfo(ParcelInfo pinfo, UUID fromSceneID)
        {
            AgentCircuit circuit;
            if (Circuits.TryGetValue(fromSceneID, out circuit))
            {
                ParcelProperties props = circuit.Scene.ParcelInfo2ParcelProperties(Owner.ID, pinfo, NextParcelSequenceId, ParcelProperties.RequestResultType.Single);
                circuit.SendMessage(props);
            }
        }

        UUID m_AgentSitTarget = UUID.Zero;
        Vector3 m_AgentRequestedSitOffset = Vector3.Zero;

        [PacketHandler(MessageType.AgentRequestSit)]
        public void HandleAgentRequestSit(Message m)
        {
            AgentCircuit circuit;
            AgentRequestSit sitreq = (AgentRequestSit)m;
            if(sitreq.SessionID != sitreq.CircuitSessionID ||
                sitreq.AgentID != sitreq.CircuitAgentID)
            {
                return;
            }

            if(Circuits.TryGetValue(sitreq.CircuitSceneID, out circuit))
            {
                SceneInterface scene = circuit.Scene;
                if(null == scene || scene.ID != SceneID)
                {
                    return;
                }
                ObjectPart part;
                if(scene.Primitives.TryGetValue(sitreq.TargetID, out part))
                {
                    ObjectGroup grp = part.ObjectGroup;
                    if(null != grp)
                    {
                        ObjectPart sitOnLink;
                        Vector3 sitOffset;
                        Quaternion sitRotation;
                        grp.AgentSitting.CheckSittable(this, out sitOffset, out sitRotation, out sitOnLink, sitreq.Offset, grp.RootPart != part ? part.LinkNumber : -1);
                        if(null == sitOnLink)
                        {
                            return;
                        }
                        lock (m_DataLock)
                        {
                            m_AgentSitTarget = sitOnLink.ID;
                            m_AgentRequestedSitOffset = sitOffset;
                        }

                        AvatarSitResponse sitres = new AvatarSitResponse();
                        sitres.SitObject = sitOnLink.ID;
                        sitres.IsAutoPilot = false;
                        sitres.SitPosition = LocalPosition;
                        sitres.SitRotation = LocalRotation;
                        sitres.CameraEyeOffset = sitOnLink.CameraEyeOffset;
                        sitres.CameraAtOffset = sitOnLink.CameraAtOffset;
                        sitres.ForceMouselook = sitOnLink.ForceMouselook;
                        SendMessageIfRootAgent(sitres, scene.ID);
                    }
                }
            }
        }

        [PacketHandler(MessageType.AgentSit)]
        public void HandleAgentSit(Message m)
        {
            AgentCircuit circuit;
            AgentSit sitreq = (AgentSit)m;
            if (sitreq.SessionID != sitreq.CircuitSessionID ||
                sitreq.AgentID != sitreq.CircuitAgentID)
            {
                return;
            }

            if (Circuits.TryGetValue(sitreq.CircuitSceneID, out circuit))
            {
                SceneInterface scene = circuit.Scene;
                if (null == scene || scene.ID != SceneID)
                {
                    return;
                }
                ObjectPart part;
                if (scene.Primitives.TryGetValue(m_AgentSitTarget, out part))
                {
                    ObjectGroup grp = part.ObjectGroup;
                    if (null != grp)
                    {
                        grp.AgentSitting.Sit(this, m_AgentRequestedSitOffset, grp.RootPart != part ? part.LinkNumber : -1);
                    }
                }
            }
        }

        [PacketHandler(MessageType.CompleteAgentMovement)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleCompleteAgentMovement(Message m)
        {
            Messages.Circuit.CompleteAgentMovement cam = (Messages.Circuit.CompleteAgentMovement)m;
            AgentCircuit circuit;
            if(cam.SessionID != cam.CircuitSessionID ||
                cam.AgentID != cam.CircuitAgentID)
            {
                m_Log.InfoFormat("Unexpected CompleteAgentMovement with invalid details");
            }
            else if (Circuits.TryGetValue(cam.CircuitSceneID, out circuit))
            {
                SceneInterface scene = circuit.Scene;
                if(null == scene)
                {
                    return;
                }

                /* switch agent region */
                if (m_IsActiveGod && !scene.IsPossibleGod(Owner))
                {
                    /* revoke god powers when changing region and new region has a different owner */
                    Messages.God.GrantGodlikePowers gm = new Messages.God.GrantGodlikePowers();
                    gm.AgentID = ID;
                    gm.SessionID = circuit.SessionID;
                    gm.GodLevel = 0;
                    gm.Token = UUID.Zero;
                    SendMessageIfRootAgent(gm, SceneID);
                    m_IsActiveGod = false;
                }
                SceneID = scene.ID;
                scene.TriggerAgentChangedScene(this);

                if(circuit.LastTeleportFlags.NeedsInitialPosition())
                {
                    try
                    {
                        scene.DetermineInitialAgentLocation(this, circuit.LastTeleportFlags, GlobalPosition, LookAt);
                    }
                    catch (Exception e)
                    {
                        /* TODO: how to do this? */
                        return;
                    }

                }

                Messages.Circuit.AgentMovementComplete amc = new Messages.Circuit.AgentMovementComplete();
                amc.AgentID = cam.AgentID;
                amc.ChannelVersion = VersionInfo.SimulatorVersion;
                amc.LookAt = circuit.Agent.LookAt;
                amc.Position = GlobalPosition;
                amc.SessionID = cam.SessionID;
                amc.GridPosition = circuit.Scene.GridPosition;
                amc.Timestamp = (uint)Date.GetUnixTime();

#if DEBUG
                m_Log.DebugFormat("sending AgentMovementComplete at {0} / {1} for {2}", amc.Position.ToString(), amc.LookAt.ToString(), Owner.FullName);
#endif

                circuit.SendMessage(amc);

                SendAgentDataUpdate(circuit);

                scene.SendAgentObjectToAllAgents(this);

                CoarseLocationUpdate clu = new CoarseLocationUpdate();
                clu.You = 0;
                clu.Prey = -1;
                CoarseLocationUpdate.AgentDataEntry ad = new CoarseLocationUpdate.AgentDataEntry();
                ad.X = (byte)(uint)GlobalPosition.X;
                ad.Y = (byte)(uint)GlobalPosition.Y;
                ad.Z = (byte)(uint)GlobalPosition.Z;
                ad.AgentID = ID;
                clu.AgentData.Add(ad);
                circuit.SendMessage(clu);

                scene.Environment.UpdateWindlightProfileToClientNoReset(this);
                scene.Environment.SendSimulatorTimeMessageToClient(this);

                foreach (ITriggerOnRootAgentActions action in circuit.m_TriggerOnRootAgentActions)
                {
                    action.TriggerOnRootAgent(ID, scene);
                }
            }
        }

        [PacketHandler(MessageType.LogoutRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleLogoutRequest(Message m)
        {
            Messages.Circuit.LogoutRequest lr = (Messages.Circuit.LogoutRequest)m;
            /* agent wants to logout */
            m_Log.InfoFormat("Agent {0} {1} ({0}) wants to logout", FirstName, LastName, ID);
            foreach (AgentCircuit c in Circuits.Values)
            {
                SceneInterface scene = c.Scene;
                if(scene == null)
                {
                    continue;
                }
                scene.Remove(this);
                if (scene.ID != lr.CircuitSceneID)
                {
                    c.Stop();
                    Circuits.Remove(scene.ID);
                    ((UDPCircuitsManager)scene.UDPServer).RemoveCircuit(c);
                }
                else
                {
                    Messages.Circuit.LogoutReply lrep = new Messages.Circuit.LogoutReply();
                    lrep.AgentID = lr.AgentID;
                    lrep.SessionID = lr.SessionID;
                    c.SendMessage(lrep);
                }
            }
        }

        [PacketHandler(MessageType.MuteListRequest)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleMuteListRequest(Message m)
        {
            Messages.MuteList.MuteListRequest req = (Messages.MuteList.MuteListRequest)m;
            if (req.AgentID != ID || req.SessionID != m.CircuitSessionID)
            {
                return;
            }
            Messages.MuteList.UseCachedMuteList res = new Messages.MuteList.UseCachedMuteList();
            res.AgentID = req.AgentID;
            SendMessageAlways(res, m.CircuitSceneID);
        }

        #region Enable Simulator call for Teleport handling
        public override void EnableSimulator(UUID originSceneID, uint circuitCode, string capsURI, DestinationInfo destinationInfo)
        {
            Messages.Circuit.EnableSimulator ensim = new Messages.Circuit.EnableSimulator();
            Messages.Circuit.EstablishAgentCommunication estagent = new Messages.Circuit.EstablishAgentCommunication();
            ensim.RegionSize = destinationInfo.Size;
            ensim.SimIP = ((IPEndPoint)destinationInfo.SimIP).Address;
            ensim.SimPort = (ushort)destinationInfo.ServerPort;
            ensim.GridPosition = destinationInfo.Location;
            estagent.AgentID = ID;
            estagent.GridPosition = destinationInfo.Location;
            estagent.RegionSize = destinationInfo.Size;
            estagent.SeedCapability = capsURI;
            estagent.SimIpAndPort = new System.Net.IPEndPoint(((IPEndPoint)destinationInfo.SimIP).Address, (int)destinationInfo.ServerPort);
            SendMessageIfRootAgent(ensim, originSceneID);
            SendMessageIfRootAgent(estagent, originSceneID);
        }
        #endregion

        public override void HandleMessage(ChildAgentUpdate m)
        {
        }

        public override void HandleMessage(ChildAgentPositionUpdate m)
        {

        }

        public override void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
        {
            AgentCircuit circuit;
            if(Circuits.TryGetValue(fromSceneID, out circuit))
            {
                circuit.ScheduleUpdate(info);
            }
        }

        public override void SendMessageIfRootAgent(Message m, UUID fromSceneID)
        {
            if (fromSceneID == SceneID)
            {
                SendMessageAlways(m, fromSceneID);
            }
        }

        public override void SendRegionNotice(UUI fromAvatar, string message, UUID fromSceneID)
        {
            GridInstantMessage im = new GridInstantMessage();
            im.FromAgent = fromAvatar;
            im.ToAgent = Owner;
            im.Dialog = GridInstantMessageDialog.MessageBox;
            im.IsOffline = false;
            im.Position = Vector3.Zero;
            im.Message = message;
            AgentCircuit circuit;
            if (Circuits.TryGetValue(fromSceneID, out circuit) &&
                IsInScene(circuit.Scene))
            {
                IMSend(im);
            }
        }

        public override void SendAlertMessage(string msg, UUID fromSceneID)
        {
            Messages.Alert.AlertMessage m = new Messages.Alert.AlertMessage(msg);
            SendMessageAlways(m, fromSceneID);
        }

        public override void SendMessageAlways(Message m, UUID fromSceneID)
        {
            AgentCircuit circuit;
            if(Circuits.TryGetValue(fromSceneID, out circuit))
            {
                circuit.SendMessage(m);
            }
        }

        public GridVector GetRootAgentGridPosition(GridVector defPos)
        {
            AgentCircuit circuit;
            if (Circuits.TryGetValue(SceneID, out circuit))
            {
                return circuit.Scene.GridPosition;
            }
            return defPos;
        }
    }
}
