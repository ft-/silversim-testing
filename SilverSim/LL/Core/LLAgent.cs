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
using SilverSim.LL.Messages;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using System;
using System.Collections.Generic;
using ThreadedClasses;

namespace SilverSim.LL.Core
{
    public partial class LLAgent : IAgent, IDisposable
    {
        private static readonly ILog m_Log = LogManager.GetLogger("LL AGENT");
        public event Action<IObject> OnPositionChange;

        #region Agent fields
        private UUID m_AgentID;
        private UUID m_CurrentSceneID;
        #endregion

        public Vector4 CollisionPlane { get; set; }

        #region LLAgent Properties
        public UInt32 LocalID { get; set; }
        public Uri HomeURI { get; private set; }

        public TeleportFlags TeleportFlags = TeleportFlags.None;
        #endregion

        /* Circuits: UUID is SceneID */
        public readonly RwLockedDoubleDictionary<UInt32, UUID, Circuit> Circuits = new RwLockedDoubleDictionary<UInt32, UUID, Circuit>();
        public readonly RwLockedDictionary<GridVector, string> KnownChildAgentURIs = new RwLockedDictionary<GridVector, string>();

        #region IObject Calls
        public void InvokeOnPositionUpdate(IObject obj)
        {
            var e = OnPositionChange; /* events are not exactly thread-safe, so copy the reference first */
            if (e != null)
            {
                foreach (Action<IObject> del in e.GetInvocationList())
                {
                    del(this);
                }
            }
        }
        #endregion

        #region IObject Properties

        private IObject m_SittingOnObject;

        public IObject SittingOnObject
        {
            /* we need to guard against our position routines and so on */
            get
            {
                lock(this)
                {
                    return m_SittingOnObject;
                }
            }
            set
            {
                lock (this)
                {
                    m_SittingOnObject = value;
                }
            }
        }

        public UUID ID
        {
            get
            {
                return m_AgentID;
            }
        }

        public string Name
        {
            get
            {
                return string.Format("{0} {1}", FirstName, LastName);
            }
            set
            {
                string[] parts = value.Split(new char[] { ' ' }, 2);
                FirstName = parts[0];
                if (parts.Length > 1)
                {
                    LastName = parts[1];
                }
            }
        }

        public UUI Group { get; set;  }

        public UUI Owner
        {
            get
            {
                UUI n = new UUI();
                n.FirstName = FirstName;
                n.LastName = LastName;
                n.ID = ID;
                n.HomeURI = HomeURI;
                return n;
            }
            set
            {
            }
        }

        public string Description
        {
            get
            {
                return string.Empty;
            }
            set
            {

            }
        }

        private Vector3 m_GlobalPosition = Vector3.Zero;

        public Vector3 Position
        {
            get
            {
                lock(this)
                {
                    if (m_SittingOnObject != null)
                    {
                        return m_GlobalPosition - m_SittingOnObject.Position;
                    }
                    else
                    {
                        return m_GlobalPosition;
                    }
                }
            }
            set
            {
                lock (this)
                {
                    if (m_SittingOnObject != null)
                    {
                        m_GlobalPosition = value + m_SittingOnObject.Position;
                    }
                    else
                    {
                        m_GlobalPosition = value;
                    }
                }
                InvokeOnPositionUpdate(this);
            }
        }

        private Vector3 m_Velocity = Vector3.Zero;
        public Vector3 Velocity
        {
            get
            {
                lock(this)
                {
                    return m_Velocity;
                }
            }
            set
            {
                lock(this)
                {
                    m_Velocity = value;
                }
            }
        }

        private Vector3 m_AngularVelocity = Vector3.Zero;
        public Vector3 AngularVelocity
        {
            get
            {
                lock(this)
                {
                    return m_AngularVelocity;
                }
            }
            set
            {
                lock(this)
                {
                    m_AngularVelocity = value;
                }
            }
        }

        public Vector3 GlobalPosition
        {
            get
            {
                lock(this)
                {
                    return m_GlobalPosition;
                }
            }
            set
            {
                lock(this)
                {
                    m_GlobalPosition = value;
                }
                InvokeOnPositionUpdate(this);
            }
        }

        public Vector3 LocalPosition
        {
            get
            {
                lock(this)
                {
                    if(m_SittingOnObject != null)
                    {
                        return m_GlobalPosition - m_SittingOnObject.Position;
                    }
                    else
                    {
                        return m_GlobalPosition;
                    }
                }
            }
            set
            {
                lock(this)
                {
                    if(m_SittingOnObject != null)
                    {
                        m_GlobalPosition = value + m_SittingOnObject.Position;
                    }
                    else
                    {
                        m_GlobalPosition = value;
                    }
                }
                InvokeOnPositionUpdate(this);
            }
        }

        private Vector3 m_Acceleration = Vector3.Zero;

        public Vector3 Acceleration
        {
            get
            {
                lock(this)
                {
                    return m_Acceleration;
                }
            }
            set
            {
                lock(this)
                {
                    m_Acceleration = value;
                }
            }
        }

        private Quaternion m_GlobalRotation = Quaternion.Identity;

        public Quaternion GlobalRotation
        {
            get
            {
                lock (this)
                {
                    if (m_SittingOnObject != null)
                    {
                        return m_GlobalRotation * m_SittingOnObject.Rotation;
                    }
                    else
                    {
                        return m_GlobalRotation;
                    }
                }
            }
            set
            {
                lock(this)
                {
                    if(m_SittingOnObject != null)
                    {
                        m_GlobalRotation = value / m_SittingOnObject.Rotation;
                    }
                    else
                    {
                        m_GlobalRotation = value;
                    }
                }
                InvokeOnPositionUpdate(this);
            }
        }

        public Quaternion LocalRotation
        {
            get
            {
                if (m_SittingOnObject != null)
                {
                    return m_GlobalRotation / m_SittingOnObject.Rotation;
                }
                else
                {
                    return m_GlobalRotation;
                }
            }
            set
            {
                if(m_SittingOnObject != null)
                {
                    m_GlobalRotation = value * m_SittingOnObject.Rotation;
                }
                else
                {
                    m_GlobalRotation = value;
                }
                InvokeOnPositionUpdate(this);
            }
        }

        public Quaternion Rotation
        {
            get
            {
                return LocalRotation;
            }
            set
            {
                LocalRotation = value;
            }
        }

        public bool IsInScene(SceneInterface scene)
        {
            lock (this)
            {
                return m_CurrentSceneID == scene.ID;
            }
        }

        public UUID SceneID
        {
            get
            {
                return m_CurrentSceneID;
            }
            set
            {
                m_CurrentSceneID = value;
            }
        }
        #endregion

        #region IObject Methods
        public void GetPrimitiveParams(AnArray.Enumerator enumerator, ref AnArray paramList)
        {
            switch (ParamsHelper.GetPrimParamType(enumerator))
            {
                case PrimitiveParamsType.Name:
                    paramList.Add(Name);
                    break;

                case PrimitiveParamsType.Desc:
                    paramList.Add(Description);
                    break;

                case PrimitiveParamsType.Type:
                    throw new ArgumentException("PRIM_TYPE not allowed for agents");

                case PrimitiveParamsType.Slice:
                    throw new ArgumentException("PRIM_SLICE not allowed for agents");

                case PrimitiveParamsType.PhysicsShapeType:
                    throw new ArgumentException("PRIM_PHYSICSSHAPETYPE not allowed for agents");

                case PrimitiveParamsType.Material:
                    throw new ArgumentException("PRIM_MATERIAL not allowed for agents");

                case PrimitiveParamsType.Position:
                    paramList.Add(Position);
                    break;

                case PrimitiveParamsType.PosLocal:
                    paramList.Add(LocalPosition);
                    break;

                case PrimitiveParamsType.Rotation:
                    paramList.Add(Rotation);
                    break;

                case PrimitiveParamsType.RotLocal:
                    paramList.Add(LocalRotation);
                    break;

                case PrimitiveParamsType.Size:
                    paramList.Add(Size);
                    break;

                case PrimitiveParamsType.Texture:
                    throw new ArgumentException("PRIM_TEXTURE not allowed for agents");

                case PrimitiveParamsType.Text:
                    throw new ArgumentException("PRIM_TEXT not allowed for agents");

                case PrimitiveParamsType.Color:
                    throw new ArgumentException("PRIM_COLOR not allowed for agents");

                case PrimitiveParamsType.BumpShiny:
                    throw new ArgumentException("PRIM_BUMPSHINY not allowed for agents");

                case PrimitiveParamsType.PointLight:
                    throw new ArgumentException("PRIM_POINTLIGHT not allowed for agents");

                case PrimitiveParamsType.FullBright:
                    throw new ArgumentException("PRIM_FULLBRIGHT not allowed for agents");

                case PrimitiveParamsType.Flexible:
                    throw new ArgumentException("PRIM_FLEXIBLE not allowed for agents");

                case PrimitiveParamsType.TexGen:
                    throw new ArgumentException("PRIM_TEXGEN not allowed for agents");

                case PrimitiveParamsType.Glow:
                    throw new ArgumentException("PRIM_GLOW not allowed for agents");

                case PrimitiveParamsType.Omega:
                    throw new ArgumentException("PRIM_OMEGA not allowed for agents");

                default:
                    throw new ArgumentException(String.Format("Invalid primitive parameter type {0}", enumerator.Current.AsUInt));
            }
        }

        public void SetPrimitiveParams(AnArray.MarkEnumerator enumerator)
        {
            switch (ParamsHelper.GetPrimParamType(enumerator))
            {
                case PrimitiveParamsType.Name:
                    Name = ParamsHelper.GetString(enumerator, "PRIM_NAME");
                    break;

                case PrimitiveParamsType.Desc:
                    Description = ParamsHelper.GetString(enumerator, "PRIM_DESC");
                    break;

                case PrimitiveParamsType.Type:
                    throw new ArgumentException("PRIM_TYPE not allowed for agents");

                case PrimitiveParamsType.Slice:
                    throw new ArgumentException("PRIM_SLICE not allowed for agents");

                case PrimitiveParamsType.PhysicsShapeType:
                    throw new ArgumentException("PRIM_PHYSICSSHAPETYPE not allowed for agents");

                case PrimitiveParamsType.Material:
                    throw new ArgumentException("PRIM_MATERIAL not allowed for agents");

                case PrimitiveParamsType.Position:
                    Position = ParamsHelper.GetVector(enumerator, "PRIM_POSITION");
                    break;

                case PrimitiveParamsType.PosLocal:
                    LocalPosition = ParamsHelper.GetVector(enumerator, "PRIM_POS_LOCAL");
                    break;

                case PrimitiveParamsType.Rotation:
                    Rotation = ParamsHelper.GetRotation(enumerator, "PRIM_ROTATION");
                    break;

                case PrimitiveParamsType.RotLocal:
                    LocalRotation = ParamsHelper.GetRotation(enumerator, "PRIM_ROT_LOCAL");
                    break;

                case PrimitiveParamsType.Size:
                    throw new ArgumentException("PRIM_SIZE not allowed for agents");

                case PrimitiveParamsType.Texture:
                    throw new ArgumentException("PRIM_TEXTURE not allowed for agents");

                case PrimitiveParamsType.Text:
                    throw new ArgumentException("PRIM_TEXT not allowed for agents");

                case PrimitiveParamsType.Color:
                    throw new ArgumentException("PRIM_COLOR not allowed for agents");

                case PrimitiveParamsType.BumpShiny:
                    throw new ArgumentException("PRIM_BUMPSHINY not allowed for agents");

                case PrimitiveParamsType.PointLight:
                    throw new ArgumentException("PRIM_POINTLIGHT not allowed for agents");

                case PrimitiveParamsType.FullBright:
                    throw new ArgumentException("PRIM_FULLBRIGHT not allowed for agents");

                case PrimitiveParamsType.Flexible:
                    throw new ArgumentException("PRIM_FLEXIBLE not allowed for agents");

                case PrimitiveParamsType.TexGen:
                    throw new ArgumentException("PRIM_TEXGEN not allowed for agents");

                case PrimitiveParamsType.Glow:
                    throw new ArgumentException("PRIM_GLOW not allowed for agents");

                case PrimitiveParamsType.Omega:
                    throw new ArgumentException("PRIM_OMEGA not allowed for agents");

                default:
                    throw new ArgumentException(String.Format("Invalid primitive parameter type {0}", enumerator.Current.AsInt));
            }
        }

        public void GetObjectDetails(AnArray.Enumerator enumerator, ref AnArray paramList)
        {
            while (enumerator.MoveNext())
            {
                switch (ParamsHelper.GetObjectDetailsType(enumerator))
                {
                    case ObjectDetailsType.Name:
                        paramList.Add(Name);
                        break;

                    case ObjectDetailsType.Desc:
                        paramList.Add(Description);
                        break;

                    case ObjectDetailsType.Pos:
                        paramList.Add(Position);
                        break;

                    case ObjectDetailsType.Rot:
                        paramList.Add(GlobalRotation);
                        break;

                    case ObjectDetailsType.Velocity:
                        paramList.Add(Velocity);
                        break;

                    case ObjectDetailsType.Owner:
                        paramList.Add(ID);
                        break;

                    case ObjectDetailsType.Group:
                        paramList.Add(Group.ID);
                        break;

                    case ObjectDetailsType.Creator:
                        paramList.Add(ID);
                        break;

                    case ObjectDetailsType.RunningScriptCount:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.TotalScriptCount:
                        {
                            int n = 0;
#if COUNT_AVATAR_SCRIPTS
                            foreach (ObjectPart obj in this.Values)
                            {
                                n += obj.Inventory.CountScripts();
                            }
#endif
                            paramList.Add(n);
                        }
                        break;

                    case ObjectDetailsType.ScriptMemory:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.ScriptTime:
                        paramList.Add(0f);
                        break;

                    case ObjectDetailsType.PrimEquivalence:
                        paramList.Add(1);
                        break;

                    case ObjectDetailsType.ServerCost:
                        paramList.Add(0f);
                        break;

                    case ObjectDetailsType.StreamingCost:
                        paramList.Add(0f);
                        break;

                    case ObjectDetailsType.PhysicsCost:
                        paramList.Add(0f);
                        break;

                    case ObjectDetailsType.CharacterTime:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.Root:
                        paramList.Add(ID);
                        break;

                    case ObjectDetailsType.AttachedPoint:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.PathfindingType:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.Physics:
                        paramList.Add(true);
                        break;

                    case ObjectDetailsType.Phantom:
                        paramList.Add(false);
                        break;

                    case ObjectDetailsType.TempOnRez:
                        paramList.Add(false);
                        break;

                    case ObjectDetailsType.RenderWeight:
                        paramList.Add(0);
                        break;

                    default:
                        throw new ArgumentException("Unknown Object Details Type");
                }
            }
        }

        public void PostEvent(IScriptEvent ev)
        {

        }
        #endregion

        #region IAgent Properties
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public Dictionary<string, string> ServiceURLs = new Dictionary<string, string>();

        private bool m_IsActiveGod = false;

        public bool IsActiveGod
        {
            get
            {
                return m_IsActiveGod;
            }
        }

        public int LastMeasuredLatencyTickCount /* info from Circuit ping measurement */
        {
            get;
            set;
        }

        public AssetServiceInterface AssetService
        {
            get
            {
                return m_AssetService;
            }
        }

        public InventoryServiceInterface InventoryService
        {
            get
            {
                return m_InventoryService;
            }
        }

        public GroupsServiceInterface GroupsService
        {
            get
            {
                return m_GroupsService;
            }
        }

        public ProfileServiceInterface ProfileService
        {
            get
            {
                return m_ProfileService;
            }
        }

        public FriendsServiceInterface FriendsService
        {
            get
            {
                return m_FriendsService;
            }
        }

        public UserAgentServiceInterface UserAgentService
        {
            get
            {
                return m_UserAgentService;
            }
        }

        public PresenceServiceInterface PresenceService
        {
            get
            {
                return m_PresenceService;
            }
        }

        public GridUserServiceInterface GridUserService
        {
            get
            {
                return m_GridUserService;
            }
        }

        public GridServiceInterface GridService
        {
            get
            {
                return m_GridService;
            }
        }
        #endregion

        #region IAgent Methods
        public bool IMSend(GridInstantMessage gim)
        {
            Circuit c;
            if(Circuits.TryGetValue(m_CurrentSceneID, out c))
            {
                Messages.IM.ImprovedInstantMessage im = new Messages.IM.ImprovedInstantMessage(gim);
                SendMessageAlways(im, m_CurrentSceneID);
                return true;
            }
            return false;
        }
        #endregion

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

        #endregion

        public LLAgent(UUID agentID,
            string firstName,
            string lastName,
            Uri homeURI,
            AssetServiceInterface assetService,
            InventoryServiceInterface inventoryService,
            GroupsServiceInterface groupsService,
            ProfileServiceInterface profileService,
            FriendsServiceInterface friendsService,
            UserAgentServiceInterface userAgentService,
            PresenceServiceInterface presenceService,
            GridUserServiceInterface gridUserService,
            GridServiceInterface gridService)
        {
            CollisionPlane = Vector4.UnitW;
            m_AgentID = agentID;
            m_AssetService = assetService;
            m_InventoryService = inventoryService;
            m_GroupsService = groupsService;
            m_ProfileService = profileService;
            m_FriendsService = friendsService;
            m_UserAgentService = userAgentService;
            m_PresenceService = presenceService;
            m_GridUserService = gridUserService;
            m_GridService = gridService;
            HomeURI = homeURI;
            FirstName = firstName;
            LastName = lastName;
            InitRouting();
        }

        ~LLAgent()
        {
            lock (this)
            {
                m_SittingOnObject = null;
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

        public void Dispose()
        {
            lock (this)
            {
                m_SittingOnObject = null;
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

        private delegate void HandleAgentMessageDelegate(Message m);
        private readonly Dictionary<MessageType, HandleAgentMessageDelegate> m_AgentMessageRouting = new Dictionary<MessageType, HandleAgentMessageDelegate>();

        void InitRouting()
        {
            m_AgentMessageRouting.Add(MessageType.MoneyBalanceRequest, HandleMoneyBalanceRequest);
            m_AgentMessageRouting.Add(MessageType.AgentDataUpdateRequest, HandleAgentDataUpdateRequest);
        }

        public void HandleAgentMessage(Message m)
        {
            switch (m.Number)
            {
                case MessageType.MoneyBalanceRequest:
                    {
                        Messages.Economy.MoneyBalanceRequest mbr = (Messages.Economy.MoneyBalanceRequest)m;
                        if (mbr.AgentID == ID && mbr.SessionID == mbr.CircuitSessionID)
                        {
                            Circuit circuit;
                            if (Circuits.TryGetValue(mbr.ReceivedOnCircuitCode, out circuit))
                            {
                                Messages.Economy.MoneyBalanceReply mbrep = new Messages.Economy.MoneyBalanceReply();
                                mbrep.ForceZeroFlag = true; /* lots of NUL at the end of the message */
                                mbrep.AgentID = mbr.AgentID;
                                mbrep.TransactionID = mbr.TransactionID;
                                try
                                {
                                    EconomyServiceInterface economyService = circuit.Scene.EconomyService;
                                    if(economyService != null)
                                    {
                                        mbrep.MoneyBalance = economyService.MoneyBalance[mbrep.AgentID, mbrep.CircuitSessionID];
                                    }
                                    else
                                    {
                                        mbrep.MoneyBalance = 0;
                                    }
                                    mbrep.TransactionSuccess = true;
                                }
                                catch
                                {
                                    mbrep.TransactionSuccess = false;
                                }
                                circuit.SendMessage(mbrep);
                            }
                        }
                    }
                    break;

                case MessageType.RegionHandshakeReply:
                    {
                        Messages.Region.RegionHandshakeReply rhr = (Messages.Region.RegionHandshakeReply)m;
                        Circuit circuit;
                        if (Circuits.TryGetValue(rhr.ReceivedOnCircuitCode, out circuit))
                        {
                            /* Add our agent to scene */
                            circuit.Scene.SendAllParcelOverlaysTo(this);
                            circuit.Scene.Terrain.UpdateTerrainDataToSingleClient(this, true);
                            circuit.Scene.Environment.UpdateWindDataToSingleClient(this);
                            circuit.Scene.SendAgentObjectToAllAgents(this);
                        }
                    }
                    break;

                case MessageType.AgentSetAppearance:
                    HandleSetAgentAppearance((Messages.Appearance.AgentSetAppearance)m);
                    break;

                case MessageType.CompleteAgentMovement:
                    {
                        Messages.Circuit.CompleteAgentMovement cam = (Messages.Circuit.CompleteAgentMovement)m;
                        Circuit circuit;
                        if ((this.TeleportFlags & TeleportFlags.ViaLogin) != 0 && (this.TeleportFlags & TeleportFlags.ViaHGLogin) == 0)
                        {
                            if (Circuits.TryGetValue(cam.ReceivedOnCircuitCode, out circuit))
                            {
                                /* switch agent region */
                                if(m_IsActiveGod && !circuit.Scene.IsPossibleGod(new UUI(ID, FirstName, LastName, HomeURI)))
                                {
                                    /* revoke god powers when changing region and new region has a different owner */
                                    Messages.God.GrantGodlikePowers gm = new Messages.God.GrantGodlikePowers();
                                    gm.AgentID = ID;
                                    gm.SessionID = circuit.SessionID;
                                    gm.GodLevel = 0;
                                    gm.Token = UUID.Zero;
                                    SendMessageIfRootAgent(gm, m_CurrentSceneID);
                                    m_IsActiveGod = false;
                                }
                                m_CurrentSceneID = circuit.Scene.ID;

                                Messages.Circuit.AgentMovementComplete amc = new Messages.Circuit.AgentMovementComplete();
                                amc.AgentID = cam.AgentID;
                                amc.ChannelVersion = VersionInfo.SimulatorVersion;
                                amc.LookAt = new Vector3(1, 1, 0); /* TODO: extract from agent */
                                amc.Position = GlobalPosition;
                                amc.SessionID = cam.SessionID;
                                amc.GridPosition = circuit.Scene.GridPosition;

                                circuit.SendMessage(amc);

                                Messages.Agent.CoarseLocationUpdate clu = new Messages.Agent.CoarseLocationUpdate();
                                clu.You = 0;
                                clu.Prey = -1;
                                Messages.Agent.CoarseLocationUpdate.AgentDataEntry ad = new Messages.Agent.CoarseLocationUpdate.AgentDataEntry();
                                ad.X = (byte)(uint)GlobalPosition.X;
                                ad.Y = (byte)(uint)GlobalPosition.Y;
                                ad.Z = (byte)(uint)GlobalPosition.Z;
                                ad.AgentID = ID;
                                clu.AgentData.Add(ad);
                                circuit.SendMessage(clu);

                                circuit.Scene.Environment.UpdateWindlightProfileToClient(this);
                            }
                        }
                    }
                    break;

                case MessageType.LogoutRequest:
                    Messages.Circuit.LogoutRequest lr = (Messages.Circuit.LogoutRequest)m;
                    /* agent wants to logout */
                    m_Log.InfoFormat("Agent {0} {1} ({0}) wants to logout", FirstName, LastName, ID);
                    foreach(Circuit c in Circuits.Values)
                    {
                        c.Scene.Remove(this);
                        if (c.Scene.ID != lr.CircuitSceneID)
                        {
                            c.Stop();
                            Circuits.Remove(c.CircuitCode, c.Scene.ID);
                            ((LLUDPServer)c.Scene.UDPServer).RemoveCircuit(c);
                        }
                        else
                        {
                            Messages.Circuit.LogoutReply lrep = new Messages.Circuit.LogoutReply();
                            lrep.AgentID = lr.AgentID;
                            lrep.SessionID = lr.SessionID;
                            c.SendMessage(lrep);
                        }
                    }
                    break;

                case MessageType.AgentWearablesRequest:
                    HandleAgentWearablesRequest((Messages.Appearance.AgentWearablesRequest)m);
                    break;

                case MessageType.AgentIsNowWearing:
                    HandleAgentIsNowWearing((Messages.Appearance.AgentIsNowWearing)m);
                    break;

                case MessageType.AgentCachedTexture:
                    HandleAgentCachedTexture((Messages.Appearance.AgentCachedTexture)m);
                    break;

                case MessageType.RequestGodlikePowers:
                    HandleRequestGodlikePowers((Messages.God.RequestGodlikePowers)m);
                    break;

                case MessageType.MuteListRequest:
                    {
                        Messages.MuteList.MuteListRequest req = (Messages.MuteList.MuteListRequest)m;
                        if(req.AgentID != ID || req.SessionID != m.CircuitSessionID)
                        {
                            return;
                        }
                        Messages.MuteList.UseCachedMuteList res = new Messages.MuteList.UseCachedMuteList();
                        res.AgentID = req.AgentID;
                        SendMessageAlways(res, m.CircuitSceneID);
                    }
                    break;

                default:
                    /* all others are handled through routing dictionary */
                    HandleAgentMessageDelegate del;
                    if(m_AgentMessageRouting.TryGetValue(m.Number, out del))
                    {
                        del(m);
                    }
                    break;
            }
        }

        public void HandleAgentUpdateMessage(Message m)
        {
            /* only AgentUpdate is passed here */
            Messages.Agent.AgentUpdate au = (Messages.Agent.AgentUpdate)m;

            if(au.CircuitSceneID != m_CurrentSceneID)
            {
                return;
            }

            /* this is for the root agent */
        }

        public void SendMessageIfRootAgent(Message m, UUID fromSceneID)
        {
            if(fromSceneID == m_CurrentSceneID)
            {
                SendMessageAlways(m, fromSceneID);
            }
        }

        public void SendMessageAlways(Message m, UUID fromSceneID)
        {
            Circuit circuit;
            if(Circuits.TryGetValue(fromSceneID, out circuit))
            {
                circuit.SendMessage(m);
            }
        }

        public GridVector GetRootAgentGridPosition(GridVector defPos)
        {
            Circuit circuit;
            if(Circuits.TryGetValue(m_CurrentSceneID, out circuit))
            {
                return circuit.Scene.GridPosition;
            }
            return defPos;
        }

        private void ToUInt16Bytes(double val, double min, double max, byte[] buf, int pos)
        {
            if (val < min)
            {
                val = min;
            }
            else if (val > max)
            {
                val = max;
            }
            val -= min;
            val = val * 65535 / (max - min);
            byte[] b = BitConverter.GetBytes((UInt16)val);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            Buffer.BlockCopy(b, 0, buf, pos, 2);
        }

        public byte[] TerseData
        {
            get
            {
                Quaternion rotation = Rotation;
                if(SittingOnObject == null)
                {
                    rotation.X = 0;
                    rotation.Y = 0;
                }
                Vector3 angvel = AngularVelocity;
                Vector3 vel = Velocity;
                Vector3 accel = Acceleration;

                byte[] data = new byte[60];
                int pos = 0;
                {
                    byte[] b = BitConverter.GetBytes(LocalID);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(b);
                    }
                    Buffer.BlockCopy(b, 0, data, pos, 4);
                    pos += 4;
                }
                data[pos++] = 0; //State
                data[pos++] = 1;

                /* Collision Plane */
                Vector4 collPlane = CollisionPlane;
                if(collPlane == Vector4.Zero)
                {
                    collPlane = Vector4.UnitW;
                }
                collPlane.ToBytes(data, pos);
                pos += 16;

                Position.ToBytes(data, pos);
                pos += 12;

                ToUInt16Bytes(vel.X, -128f, 128f, data, pos);
                pos += 2;
                ToUInt16Bytes(vel.Y, -128f, 128f, data, pos);
                pos += 2;
                ToUInt16Bytes(vel.Z, -128f, 128f, data, pos);
                pos += 2;

                ToUInt16Bytes(accel.X, -64, 64f, data, pos);
                pos += 2;
                ToUInt16Bytes(accel.Y, -64, 64f, data, pos);
                pos += 2;
                ToUInt16Bytes(accel.Z, -64, 64f, data, pos);
                pos += 2;

                ToUInt16Bytes(rotation.X, -1f, 1f, data, pos);
                pos += 2;
                ToUInt16Bytes(rotation.Y, -1f, 1f, data, pos);
                pos += 2;
                ToUInt16Bytes(rotation.Z, -1f, 1f, data, pos);
                pos += 2;
                ToUInt16Bytes(rotation.W, -1f, 1f, data, pos);
                pos += 2;

                ToUInt16Bytes(angvel.X, -64f, 64f, data, pos);
                pos += 2;
                ToUInt16Bytes(angvel.Y, -64f, 64f, data, pos);
                pos += 2;
                ToUInt16Bytes(angvel.Z, -64f, 64f, data, pos);
                pos += 2;

                return data;
            }
        }
    }
}
