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

#pragma warning disable IDE0018
#pragma warning disable RCS1029

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
using SilverSim.Types.Asset;
using SilverSim.Types.Friends;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using SilverSim.Types.Inventory;
using SilverSim.Types.Parcel;
using SilverSim.Types.Script;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Agent;
using SilverSim.Viewer.Messages.Alert;
using SilverSim.Viewer.Messages.Avatar;
using SilverSim.Viewer.Messages.CallingCard;
using SilverSim.Viewer.Messages.Circuit;
using SilverSim.Viewer.Messages.God;
using SilverSim.Viewer.Messages.Inventory;
using SilverSim.Viewer.Messages.Parcel;
using SilverSim.Viewer.Messages.Script;
using SilverSim.Viewer.Messages.User;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace SilverSim.Viewer.Core
{
    public partial class ViewerAgent : SilverSim.Scene.Agent.Agent, ILocalIDAccessor
    {
        private static readonly ILog m_Log = LogManager.GetLogger("VIEWER AGENT");
        private readonly SceneList m_Scenes;

        #region Agent fields
        private UUID m_CurrentSceneID;

        #endregion

        /** <summary>Key is region ID</summary> */
        public override RwLockedDictionary<UUID, AgentChildInfo> ActiveChilds { get; } = new RwLockedDictionary<UUID, AgentChildInfo>();
        public override ClientInfo Client { get; }

        private readonly UserAccount m_UntrustedAccountInfo;
        public override UserAccount UntrustedAccountInfo => new UserAccount(m_UntrustedAccountInfo);

        public override SessionInfo Session => new SessionInfo
        {
            SessionID = SessionID,
            SecureSessionID = m_SecureSessionID,
        };

        public override List<GridType> SupportedGridTypes
        {
            get
            {
                var gridTypes = new List<GridType>();
                foreach (var agentteleport in m_TeleportServices)
                {
                    gridTypes.Add(agentteleport.GridType);
                }
                return gridTypes;
            }
        }

        private IAgentTeleportServiceInterface m_ActiveTeleportService;
        public override IAgentTeleportServiceInterface ActiveTeleportService
        {
            get { return m_ActiveTeleportService; }

            set
            {
                lock (m_DataLock)
                {
                    if (m_ActiveTeleportService != null && value != null)
                    {
                        throw new InvalidOperationException();
                    }
                    m_ActiveTeleportService = value;
                }
            }
        }

        public override void RemoveActiveTeleportService(IAgentTeleportServiceInterface service)
        {
            lock (m_DataLock)
            {
                if (m_ActiveTeleportService == service)
                {
                    m_ActiveTeleportService = null;
                }
            }
        }

        public override AgentUpdateInfo GetUpdateInfo(UUID sceneID)
        {
            AgentCircuit circuit;
            return Circuits.TryGetValue(sceneID, out circuit) ? circuit.UpdateInfo : null;
        }

        public override void SendKillObject(UUID sceneID)
        {
            AgentCircuit circuit;
            if (Circuits.TryGetValue(sceneID, out circuit))
            {
                circuit.SendKillObject();
            }
        }

        public override ILocalIDAccessor LocalID => this;

        uint ILocalIDAccessor.this[UUID sceneID]
        {
            get
            {
                AgentCircuit circuit;
                return Circuits.TryGetValue(sceneID, out circuit) ? circuit.UpdateInfo.LocalID : 0;
            }
            set
            {
                AgentCircuit circuit;
                if (Circuits.TryGetValue(sceneID, out circuit))
                {
                    circuit.UpdateInfo.LocalID = value;
                }
                else
                {
                    m_Log.DebugFormat("Setting LocalID on agent {0} for region {1} has no circuit", ID, sceneID);
                }
            }
        }


        #region ViewerAgent Properties
        public UUID SessionID { get; }
        private double m_DrawDistance;
        public override double DrawDistance => m_DrawDistance;
        #endregion

        /* Circuits: UUID is SceneID */
        public readonly RwLockedDictionary<UUID, AgentCircuit> Circuits = new RwLockedDictionary<UUID, AgentCircuit>();
        public readonly RwLockedDictionary<GridVector, string> KnownChildAgentURIs = new RwLockedDictionary<GridVector, string>();

        public override RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> TransmittedTerrainSerials { get; } = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>>(() => new RwLockedDictionary<uint, uint>());

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

        protected override SceneInterface RootAgentScene
        {
            get
            {
                UUID id = SceneID;
                AgentCircuit circuit;
                if(Circuits.TryGetValue(id, out circuit))
                {
                    return circuit.Scene;
                }
                return null;
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
                    foreach (var kvp in waitForRootList)
                    {
                        kvp.Key(kvp.Value, true);
                    }
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
        public override bool IsNpc => false;

        private bool m_IsInMouselook;

        public override bool IsInMouselook => m_IsInMouselook;

        private readonly RwLockedDictionary<UUID, FriendStatus> m_KnownFriends = new RwLockedDictionary<UUID, FriendStatus>();
        private bool m_KnownFriendsCached;
        private readonly object m_KnownFriendsCacheLock = new object();

        private void CacheFriends()
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
                        var haveIDs = new List<UUID>(m_KnownFriends.Keys);
                        foreach (var fi in FriendsService[Owner])
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

                        foreach(var id in haveIDs)
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

        public override bool IsActiveGod => m_IsActiveGod;

        public override int LastMeasuredLatencyMsecs /* info from Circuit ping measurement */
        {
            get
            {
                int pingms = 0;
                int count = 0;
                foreach (AgentCircuit circuit in Circuits.Values)
                {
                    pingms += circuit.LastMeasuredLatencyMsecs;
                    ++count;
                }

                if (count > 0)
                {
                    pingms /= count;
                }

                return pingms;
            }
        }

        protected override AssetServiceInterface SceneAssetService
        {
            get
            {
                AgentCircuit circuit;
                if(Circuits.TryGetValue(m_CurrentSceneID, out circuit))
                {
                    return circuit.Scene.AssetService;
                }
                return AssetService;
            }
        }

        public override AssetServiceInterface AssetService => m_AssetService;

        public override InventoryServiceInterface InventoryService => m_InventoryService;

        public override OfflineIMServiceInterface OfflineIMService { get; }

        public override MuteListServiceInterface MuteListService { get; }

        public override GroupsServiceInterface GroupsService => m_GroupsService;

        public override ProfileServiceInterface ProfileService => m_ProfileService;

        public override FriendsServiceInterface FriendsService => m_FriendsService;

        public override UserAgentServiceInterface UserAgentService => m_UserAgentService;

        public override IPresenceServiceInterface PresenceService => m_PresenceService;

        public override EconomyServiceInterface EconomyService { get; }
        #endregion

        #region IAgent Methods
        public override bool IMSend(GridInstantMessage gim)
        {
            AgentCircuit c;
            var sceneID = SceneID;
            if (Circuits.TryGetValue(sceneID, out c))
            {
                if (gim.Dialog == GridInstantMessageDialog.OfferCallingCard)
                {
                    var m = new OfferCallingCard
                    {
                        AgentID = gim.FromAgent.ID,
                        SessionID = UUID.Zero,
                        DestID = ID,
                        TransactionID = CreateCallingCard(gim.FromAgent, false)
                    };
                    SendMessageAlways(m, sceneID);
                }
                else
                {
                    var im = new Messages.IM.ImprovedInstantMessage(gim);
                    if (gim.IsSystemMessage)
                    {
                        /* this is a system message, so we change its sender name */
                        im.FromAgentName = "System";
                        im.RegionID = UUID.Zero;
                        im.ParentEstateID = 0;
                        im.Position = Vector3.Zero;
                    }
                    SendMessageAlways(im, sceneID);
                }
                return true;
            }
            return false;
        }
        #endregion

        public UUID CreateCallingCard(UGUI agentid, bool isgod)
        {
            var item = new InventoryItem
            {
                AssetID = UUID.Zero,
                AssetType = AssetType.CallingCard,
                Creator = agentid,
                Owner = Owner,
                CreationDate = Date.Now,
                InventoryType = InventoryType.CallingCard,
                Flags = InventoryFlags.None,
                Name = Name,
            };

            item.Permissions.Base = InventoryPermissionsMask.Copy | InventoryPermissionsMask.Modify;
            if (isgod)
            {
                item.Permissions.Base |= InventoryPermissionsMask.Transfer | InventoryPermissionsMask.Modify;
            }

            item.Permissions.EveryOne = InventoryPermissionsMask.None;
            item.Permissions.Current = item.Permissions.Base;
            item.Permissions.NextOwner = InventoryPermissionsMask.Copy | InventoryPermissionsMask.Modify;

            item.ParentFolderID = InventoryService.Folder[ID, AssetType.CallingCard].ID;
            InventoryService.Item.Add(item);
            var m = new BulkUpdateInventory
            {
                AgentID = ID
            };
            m.AddInventoryItem(item, 0);
            SendMessageAlways(m, SceneID);
            return item.ID;
        }

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
        private IPresenceServiceInterface m_PresenceService;

        #endregion

        private readonly UUID m_SecureSessionID;
        private readonly string m_ServiceSessionID;
        private readonly List<IAgentTeleportServiceInterface> m_TeleportServices;

        private void CloseAllCircuits(bool result)
        {
            foreach(var info in ActiveChilds.Values)
            {
                info.ChildAgentUpdateService.Disconnect();
            }
            foreach(var circ in Circuits.Values)
            {
                circ.Stop();
            }
        }

        public override void KickUser(string msg)
        {
            var req = new KickUser
            {
                AgentID = Owner.ID,
                SessionID = SessionID,
                Message = msg
            };
            req.OnSendCompletion += CloseAllCircuits;
            SendMessageAlways(req, m_CurrentSceneID);
        }

        public override void KickUser(string msg, Action<bool> callbackDelegate)
        {
            var req = new KickUser
            {
                AgentID = Owner.ID,
                SessionID = SessionID,
                Message = msg
            };
            req.OnSendCompletion += callbackDelegate;
            req.OnSendCompletion += CloseAllCircuits;
            SendMessageAlways(req, m_CurrentSceneID);
        }

        [PacketHandler(MessageType.GodKickUser)]
        public void HandleGodKickUser(Message m)
        {
            var req = (GodKickUser)m;
            if(req.GodID != req.CircuitAgentID ||
                req.GodSessionID != req.CircuitSessionID)
            {
                return;
            }

            SceneInterface scene = Circuits[req.CircuitSceneID].Scene;

            if (IsActiveGod && IsInScene(scene))
            {
                IAgent targetAgent;
                if(scene.Agents.TryGetValue(req.AgentID, out targetAgent))
                {
                    targetAgent.KickUser(req.Reason);
                }
            }
        }

        public override bool TeleportTo(SceneInterface sceneInterface, string regionName, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            foreach(var service in m_TeleportServices)
            {
#if DEBUG
                m_Log.DebugFormat("Checking Teleport Service {0} for {1}", service.GetType().ToString(), NamedOwner.FullName);
#endif

                if (service.TeleportTo(sceneInterface, regionName, position, lookAt, flags))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool TeleportTo(SceneInterface sceneInterface, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            foreach (var service in m_TeleportServices)
            {
#if DEBUG
                m_Log.DebugFormat("Checking Teleport Service {0} for {1}", service.GetType().ToString(), NamedOwner.FullName);
#endif
                if (service.TeleportTo(sceneInterface, location, position, lookAt, flags))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            foreach (var service in m_TeleportServices)
            {
#if DEBUG
                m_Log.DebugFormat("Checking Teleport Service {0} for {1}", service.GetType().ToString(), NamedOwner.FullName);
#endif
                if (service.TeleportTo(sceneInterface, gatekeeperURI, location, position, lookAt, flags))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool TeleportTo(SceneInterface sceneInterface, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            foreach (var service in m_TeleportServices)
            {
#if DEBUG
                m_Log.DebugFormat("Checking Teleport Service {0} for {1}", service.GetType().ToString(), NamedOwner.FullName);
#endif
                if (service.TeleportTo(sceneInterface, regionID, position, lookAt, flags))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags)
        {
            foreach (var service in m_TeleportServices)
            {
#if DEBUG
                m_Log.DebugFormat("Checking Teleport Service {0} for {1}", service.GetType().ToString(), NamedOwner.FullName);
#endif
                if (service.TeleportTo(sceneInterface, gatekeeperURI, regionID, position, lookAt, flags))
                {
                    return true;
                }
            }
            return false;
        }

        protected override void DieAgent()
        {
            SceneInterface scene = RootAgentScene;
            if(scene == null || !TeleportHome(scene))
            {
                KickUser(this.GetLanguageString(CurrentCulture, "YouHaveDied", "You have died"));
            }
        }

        /* following function returns true if it accepts a teleport request or if it wants to distribute more specific error message except home location not available */
        public override bool TeleportHome(SceneInterface sceneInterface)
        {
            foreach(var service in m_TeleportServices)
            {
#if DEBUG
                m_Log.DebugFormat("Checking Teleport Service {0} for {1}", service.GetType().ToString(), NamedOwner.FullName);
#endif
                if (service.TeleportHome(sceneInterface))
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
            ClientInfo clientInfo,
            UserAccount untrustedAccountInfo,
            AgentServiceList serviceList)
            : base(agentID, homeURI)
        {
            m_Scenes = scenes;
            m_TeleportServices = serviceList.GetAll<IAgentTeleportServiceInterface>();
            foreach(IAgentTeleportServiceInterface service in m_TeleportServices)
            {
                service.Agent = this;
            }
            CollisionPlane = Vector4.UnitW;
            SessionID = sessionID;
            m_UntrustedAccountInfo = untrustedAccountInfo;
            m_SecureSessionID = secureSessionID;
            Client = clientInfo;
            m_AssetService = serviceList.Get<AssetServiceInterface>();
            m_InventoryService = serviceList.Get<InventoryServiceInterface>();
            m_GroupsService = serviceList.Get<GroupsServiceInterface>();
            m_ProfileService = serviceList.Get<ProfileServiceInterface>();
            m_FriendsService = serviceList.Get<FriendsServiceInterface>();
            m_UserAgentService = serviceList.Get<UserAgentServiceInterface>();
            m_PresenceService = serviceList.Get<IPresenceServiceInterface>();
            EconomyService = serviceList.Get<EconomyServiceInterface>();
            OfflineIMService = serviceList.Get<OfflineIMServiceInterface>();
            MuteListService = serviceList.Get<MuteListServiceInterface>();
            FirstName = firstName;
            LastName = lastName;
            OnPositionChange += ChildUpdateOnPositionChange;
            OnAppearanceUpdate += HandleAppearanceUpdate;
        }

        ~ViewerAgent()
        {
            OnPositionChange -= ChildUpdateOnPositionChange;
            lock (m_DataLock)
            {
                m_TeleportServices.Clear();
                m_AssetService = null;
                m_InventoryService = null;
                m_GroupsService = null;
                m_ProfileService = null;
                m_FriendsService = null;
                m_UserAgentService = null;
                m_PresenceService = null;
            }
        }

        #region Physics Linkage

        public override RwLockedDictionary<UUID, IPhysicsObject> PhysicsActors { get; } = new RwLockedDictionary<UUID, IPhysicsObject>();

        public override IPhysicsObject PhysicsActor
        {
            get
            {
                lock(m_DataLock)
                {
                    IPhysicsObject obj;
                    if(!PhysicsActors.TryGetValue(SceneID, out obj))
                    {
                        return DummyAgentPhysicsObject.SharedInstance;
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

        public override ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions) =>
            RequestPermissions(part, itemID, permissions, UUID.Zero);

        public override ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions, UUID experienceID)
        {
            var autoGrant = ScriptPermissions.None;
            ObjectGroup sitOn = SittingOnObject;
            if ((sitOn != null && sitOn.ID == itemID) || part.ObjectGroup.AttachPoint != Types.Agent.AttachmentPoint.NotAttached)
            {
                autoGrant |= ScriptPermissions.ControlCamera;
                autoGrant |= ScriptPermissions.TakeControls;
                autoGrant |= ScriptPermissions.TrackCamera;
                autoGrant |= ScriptPermissions.TriggerAnimation;
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

            AgentCircuit circuit;
            SceneInterface scene = part.ObjectGroup.Scene;
            if (experienceID != UUID.Zero && Circuits.TryGetValue(scene.ID, out circuit) &&
                !circuit.AddExperienceTimeout(part.ID, itemID))
            {
                return ScriptPermissions.None;
            }
            var m = new ScriptQuestion
            {
                ExperienceID = experienceID,
                ItemID = itemID,
                ObjectName = part.ObjectGroup.Name,
                ObjectOwner = scene.AvatarNameService.ResolveName(part.Owner).FullName,
                Questions = permissions,
                TaskID = part.ID
            };
            SendMessageAlways(m, part.ObjectGroup.Scene.ID);
            return ScriptPermissions.None;
        }

        public override void RevokePermissions(UUID sourceID, UUID itemID, ScriptPermissions permissions) =>
            RevokeAnimPermissions(sourceID, permissions);

        public override bool WaitsForExperienceResponse(ObjectPart part, UUID itemID)
        {
            SceneInterface scene = part.ObjectGroup?.Scene;
            if(scene == null)
            {
                return false;
            }
            AgentCircuit circuit;
            if(!Circuits.TryGetValue(scene.ID, out circuit))
            {
                return false;
            }
            return circuit.WaitsForExperienceResponse(part.ID, itemID);
        }

        [PacketHandler(MessageType.RegionHandshakeReply)]
        public void HandleRegionHandshakeReply(Message m)
        {
            var rhr = (Messages.Region.RegionHandshakeReply)m;
            AgentCircuit circuit;
            if (Circuits.TryGetValue(rhr.CircuitSceneID, out circuit))
            {
                var scene = circuit.Scene;
                /* Add our agent to scene */
                scene.SendAllParcelOverlaysTo(this);
                scene.Terrain.UpdateTerrainDataToSingleClient(this);
                scene.Environment.UpdateWindDataToSingleClient(this);
                scene.SendAgentObjectToAllAgents(this);
                scene.SendRegionInfo(this);
                ParcelInfo pinfo;
                if(scene.Parcels.TryGetValue(GlobalPosition, out pinfo))
                {
                    var props = scene.ParcelInfo2ParcelProperties(Owner.ID, pinfo, NextParcelSequenceId, ParcelProperties.RequestResultType.Single);
                    circuit.SendMessage(props);
                }
                circuit.ScheduleFirstUpdate();
            }
        }

        public override void SendUpdatedParcelInfo(ParcelInfo pinfo, UUID fromSceneID)
        {
            AgentCircuit circuit;
            if (Circuits.TryGetValue(fromSceneID, out circuit))
            {
                var props = circuit.Scene.ParcelInfo2ParcelProperties(Owner.ID, pinfo, NextParcelSequenceId, ParcelProperties.RequestResultType.Single);
                circuit.SendMessage(props);
            }
        }

        private UUID m_AgentSitTarget = UUID.Zero;
        private Vector3 m_AgentRequestedSitOffset = Vector3.Zero;

        [PacketHandler(MessageType.AgentRequestSit)]
        public void HandleAgentRequestSit(Message m)
        {
            AgentCircuit circuit;
            var sitreq = (AgentRequestSit)m;
            if(sitreq.SessionID != sitreq.CircuitSessionID ||
                sitreq.AgentID != sitreq.CircuitAgentID)
            {
                return;
            }

#if DEBUG
            m_Log.DebugFormat("Process AgentRequestSit for {0} at {1}", sitreq.AgentID, sitreq.TargetID);
#endif

            if (Circuits.TryGetValue(sitreq.CircuitSceneID, out circuit))
            {
                var scene = circuit.Scene;
                if(scene == null || scene.ID != SceneID)
                {
                    return;
                }
                ObjectPart part;
                var sittingOn = SittingOnObject;
                if(sittingOn != null)
                {
                    sittingOn.AgentSitting.UnSit(this);
                }

                if(scene.Primitives.TryGetValue(sitreq.TargetID, out part))
                {
                    var grp = part.ObjectGroup;
                    if(grp != null)
                    {
                        ObjectPart sitOnLink;
                        Vector3 sitOffset;
                        Quaternion sitRotation;
                        if(!grp.AgentSitting.CheckSittable(this, out sitOffset, out sitRotation, out sitOnLink, sitreq.Offset, grp.RootPart != part ? part.LinkNumber : -1))
                        {
                            return;
                        }
                        lock (m_DataLock)
                        {
                            m_AgentSitTarget = sitOnLink.ID;
                            m_AgentRequestedSitOffset = sitOffset;
                        }

                        grp.AgentSitting.Sit(this, m_AgentRequestedSitOffset, grp.RootPart != part ? part.LinkNumber : -1);

                        var sitres = new AvatarSitResponse
                        {
                            SitObject = sitOnLink.ID,
                            IsAutoPilot = false,
                            SitPosition = LocalPosition,
                            SitRotation = LocalRotation,
                            CameraEyeOffset = sitOnLink.CameraEyeOffset,
                            CameraAtOffset = sitOnLink.CameraAtOffset,
                            ForceMouselook = sitOnLink.ForceMouselook
                        };
                        SendMessageIfRootAgent(sitres, scene.ID);
                    }
                }
            }
        }

        [PacketHandler(MessageType.AgentSit)]
        public void HandleAgentSit(Message m)
        {
            AgentCircuit circuit;
            var sitreq = (AgentSit)m;
            if (sitreq.SessionID != sitreq.CircuitSessionID ||
                sitreq.AgentID != sitreq.CircuitAgentID)
            {
                return;
            }

#if DEBUG
            m_Log.DebugFormat("Process AgentSit for {0}", sitreq.AgentID);
#endif

            if (Circuits.TryGetValue(sitreq.CircuitSceneID, out circuit))
            {
                var scene = circuit.Scene;
                if (scene == null || scene.ID != SceneID)
                {
                    return;
                }
                ObjectPart part;
                if (scene.Primitives.TryGetValue(m_AgentSitTarget, out part))
                {
                    var grp = part.ObjectGroup;
                    if (grp != null)
                    {
                        grp.AgentSitting.Sit(this, m_AgentRequestedSitOffset, grp.RootPart != part ? part.LinkNumber : -1);
                    }
                }
            }
        }

        [PacketHandler(MessageType.CompleteAgentMovement)]
        public void HandleCompleteAgentMovement(Message m)
        {
            var cam = (CompleteAgentMovement)m;
            AgentCircuit circuit;
            if(cam.SessionID != cam.CircuitSessionID ||
                cam.AgentID != cam.CircuitAgentID)
            {
                m_Log.InfoFormat("Unexpected CompleteAgentMovement with invalid details");
            }
            else if (Circuits.TryGetValue(cam.CircuitSceneID, out circuit))
            {
                var scene = circuit.Scene;
                if(scene == null)
                {
                    return;
                }

                /* switch agent region */
                if (m_IsActiveGod && !scene.IsPossibleGod(Owner))
                {
                    /* revoke god powers when changing region and new region has a different owner */
                    var gm = new GrantGodlikePowers
                    {
                        AgentID = ID,
                        SessionID = circuit.SessionID,
                        GodLevel = 0,
                        Token = UUID.Zero
                    };
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
                    catch
                    {
                        /* TODO: how to do this? */
                        return;
                    }
                }

                var amc = new AgentMovementComplete
                {
                    AgentID = cam.AgentID,
                    ChannelVersion = VersionInfo.SimulatorVersion,
                    LookAt = circuit.Agent.LookAt,
                    Position = GlobalPosition,
                    SessionID = cam.SessionID,
                    GridPosition = circuit.Scene.GridPosition,
                    Timestamp = (uint)Date.GetUnixTime()
                };

                circuit.SendMessage(amc);

                SendAgentDataUpdate(circuit);

                scene.SendAgentObjectToAllAgents(this);

                var clu = new CoarseLocationUpdate
                {
                    You = 0,
                    Prey = -1
                };
                var ad = new CoarseLocationUpdate.AgentDataEntry
                {
                    X = (byte)(uint)GlobalPosition.X,
                    Y = (byte)(uint)GlobalPosition.Y,
                    Z = (byte)(uint)GlobalPosition.Z,
                    AgentID = ID
                };
                clu.AgentData.Add(ad);
                circuit.SendMessage(clu);

                scene.Environment.UpdateWindlightProfileToClientNoReset(this);
                scene.Environment.SendSimulatorTimeMessageToClient(this);

                foreach (var action in circuit.m_TriggerOnRootAgentActions)
                {
                    action.TriggerOnRootAgent(ID, scene);
                }
            }
        }

        public void DisableCircuit(UUID sceneid)
        {
            AgentCircuit circuit;
            if(Circuits.TryGetValue(sceneid, out circuit))
            {
                SceneInterface scene = circuit.Scene;
                if(scene == null)
                {
                    return;
                }

                var m = new DisableSimulator();
                m.OnSendCompletion += (bool success) =>
                {
                    if(Circuits?.TryGetValue(sceneid, out circuit) ?? false)
                    {
                        scene = circuit.Scene;
                        if (scene != null)
                        {
#if DEBUG
                            m_Log.DebugFormat("Removing agent {0}: Stop circuit {1}", ID, scene.ID);
#endif
                            circuit.Stop();
#if DEBUG
                            m_Log.DebugFormat("Removing agent {0} from circuit list ({1})", ID, scene.ID);
#endif
                            Circuits.Remove(scene.ID);
#if DEBUG
                            m_Log.DebugFormat("Removing agent {0} from scene {1}", ID, scene.ID);
#endif
                            ((UDPCircuitsManager)scene.UDPServer).RemoveCircuit(circuit);
                        }
                    }
                };
                circuit.SendMessage(m);
            }
        }

        [PacketHandler(MessageType.LogoutRequest)]
        public void HandleLogoutRequest(Message m)
        {
            var lr = (LogoutRequest)m;
            ThreadPool.QueueUserWorkItem(ProcessLogoutWorkItem, lr);
        }

        private void ProcessLogoutWorkItem(object o)
        {
            var lr = (LogoutRequest)o;
            /* agent wants to logout */
            m_Log.InfoFormat("Agent {0} {1} ({0}) wants to logout", FirstName, LastName, ID);
            foreach (var c in Circuits.Values)
            {
                SceneInterface scene = c.Scene;
                if (scene == null)
                {
                    continue;
                }
#if DEBUG
                m_Log.DebugFormat("Removing agent {0}: Removing from scene {1}", ID, scene.ID);
#endif
                scene.Remove(this);
                if (scene.ID != lr.CircuitSceneID)
                {
#if DEBUG
                    m_Log.DebugFormat("Removing agent {0}: Stop circuit {1}", ID, scene.ID);
#endif
                    c.Stop();
#if DEBUG
                    m_Log.DebugFormat("Removing agent {0} from circuit list ({1})", ID, scene.ID);
#endif
                    Circuits.Remove(scene.ID);
#if DEBUG
                    m_Log.DebugFormat("Removing agent {0} from scene {1}", ID, scene.ID);
#endif
                    ((UDPCircuitsManager)scene.UDPServer).RemoveCircuit(c);
                }
                else
                {
#if DEBUG
                    m_Log.DebugFormat("Removing agent {0}: Sending logout reply for {1}", ID, scene.ID);
#endif
                    var lrep = new LogoutReply
                    {
                        AgentID = lr.AgentID,
                        SessionID = lr.SessionID
                    };
                    c.SendMessage(lrep);
                }
            }
            m_Log.InfoFormat("Agent {0} {1} ({0}) logout request processed", FirstName, LastName, ID);
        }

        [PacketHandler(MessageType.TrackAgent)]
        public void HandleTrackAgent(Message m)
        {
            var req = (TrackAgent)m;
            if(req.AgentID != ID || req.SessionID != m.CircuitSessionID)
            {
                return;
            }
            TracksAgentID = req.PreyID;
            AgentCircuit circuit;
            if(Circuits.TryGetValue(m.CircuitSessionID, out circuit))
            {
                circuit.Scene?.SendCoarseLocationUpdateToSpecificAgent(this);
            }
        }

        #region Enable Simulator call for Teleport handling
        public override void EnableSimulator(UUID originSceneID, uint circuitCode, string capsURI, DestinationInfo destinationInfo)
        {
            var ensim = new EnableSimulator
            {
                RegionSize = destinationInfo.Size,
                SimIP = ((IPEndPoint)destinationInfo.SimIP).Address,
                SimPort = (ushort)destinationInfo.ServerPort,
                GridPosition = destinationInfo.Location
            };
            var estagent = new EstablishAgentCommunication
            {
                AgentID = ID,
                GridPosition = destinationInfo.Location,
                RegionSize = destinationInfo.Size,
                SeedCapability = capsURI,
                SimIpAndPort = new IPEndPoint(((IPEndPoint)destinationInfo.SimIP).Address, (int)destinationInfo.ServerPort)
            };
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

        public override void ScheduleUpdate(AgentUpdateInfo info, UUID fromSceneID)
        {
            AgentCircuit circuit;
            if (Circuits.TryGetValue(fromSceneID, out circuit))
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

        public override void SendRegionNotice(UGUI fromAvatar, string message, UUID fromSceneID)
        {
            var im = new GridInstantMessage
            {
                FromAgent = (UGUIWithName)fromAvatar,
                ToAgent = Owner,
                Dialog = GridInstantMessageDialog.MessageBox,
                IsOffline = false,
                Position = Vector3.Zero,
                Message = message
            };
            AgentCircuit circuit;
            if (Circuits.TryGetValue(fromSceneID, out circuit) &&
                IsInScene(circuit.Scene))
            {
                IMSend(im);
            }
        }

        public override void SendAlertMessage(string msg, UUID fromSceneID)
        {
            var m = new AlertMessage(msg);
            SendMessageAlways(m, fromSceneID);
        }

        public override void SendAlertMessage(string msg, string notification, IValue llsd, UUID fromSceneID)
        {
            var m = new AlertMessage(msg);
            var d = new AlertMessage.Data();
            d.Message = notification;
            byte[] b;
            using (var ms = new MemoryStream())
            {
                LlsdXml.Serialize(llsd, ms);
                b = ms.ToArray();
            }
            d.ExtraParams = b;
            m.AlertInfo.Add(d);
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
