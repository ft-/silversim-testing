// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Agent;
using SilverSim.Viewer.Messages.Parcel;
using SilverSim.Viewer.Messages.Script;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Neighbor;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
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
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using SilverSim.Types.Parcel;
using SilverSim.Types.Primitive;
using SilverSim.Types.Script;
using System;
using System.Collections.Generic;
using System.Net;
using ThreadedClasses;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Viewer.Core
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    public partial class ViewerAgent : IAgent
    {
        private static readonly ILog m_Log = LogManager.GetLogger("VIEWER AGENT");
        public event Action<IObject> OnPositionChange;

        #region Agent fields
        readonly UUID m_AgentID;
        private UUID m_CurrentSceneID;
        #endregion

        readonly Dictionary<UUID, AgentChildInfo> m_ActiveChilds = new Dictionary<UUID, AgentChildInfo>();

        public Dictionary<UUID, AgentChildInfo> ActiveChilds
        {
            get
            {
                return m_ActiveChilds;
            }
        }

        readonly ClientInfo m_ClientInfo;
        public ClientInfo Client 
        { 
            get
            {
                return m_ClientInfo;
            }
        }

        readonly UserAccount m_UntrustedAccountInfo;
        public UserAccount UntrustedAccountInfo
        { 
            get
            {
                return new UserAccount(m_UntrustedAccountInfo);
            }
        }

        public SessionInfo Session 
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

        public List<GridType> SupportedGridTypes 
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

        public Vector4 CollisionPlane
        {
            get
            {
                return Vector4.UnitW;
            }
            set
            {
            }
        }

        IAgentTeleportServiceInterface m_ActiveTeleportService;
        public IAgentTeleportServiceInterface ActiveTeleportService
        {
            get
            {
                return m_ActiveTeleportService;
            }
            set
            {
                lock(this)
                {
                    if(m_ActiveTeleportService != null && value != null)
                    {
                        throw new InvalidOperationException();
                    }
                    m_ActiveTeleportService = value;
                }
            }
        }

        #region LLAgent Properties
        public UInt32 LocalID { get; set; }
        public Uri HomeURI { get; private set; }
        public UUID SessionID { get; private set; }
        public double DrawDistance { get; private set; }

        public TeleportFlags TeleportFlags;
        #endregion

        /* Circuits: UUID is SceneID */
        public readonly RwLockedDoubleDictionary<UInt32, UUID, AgentCircuit> Circuits = new RwLockedDoubleDictionary<UInt32, UUID, AgentCircuit>();
        public readonly RwLockedDictionary<GridVector, string> KnownChildAgentURIs = new RwLockedDictionary<GridVector, string>();

        private readonly RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> m_TransmittedTerrainSerials = new RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>>(delegate() { return new RwLockedDictionary<uint, uint>(); });

        public RwLockedDictionaryAutoAdd<UUID, RwLockedDictionary<uint, uint>> TransmittedTerrainSerials
        {
            get
            {
                return m_TransmittedTerrainSerials;
            }
        }

        #region IObject Calls
        public void InvokeOnPositionUpdate()
        {
            var e = OnPositionChange; /* events are not exactly thread-safe, so copy the reference first */
            if (e != null)
            {
                Action<IObject>[] invocationList = (Action<IObject>[])e.GetInvocationList();
                foreach (Action<IObject> del in invocationList)
                {
                    del(this);
                }
            }
            AgentCircuit c;
            if(Circuits.TryGetValue(SceneID, out c))
            {
                c.Scene.SendAgentObjectToAllAgents(this);
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

        public UGI Group { get; set;  }


        public Vector3 LookAt
        {
            get
            {
                Vector3 angle = new Vector3(1, 0, 0);
                return angle * Rotation;
            }
            set
            {
                Vector3 delta = value.Normalize();
                Rotation = Quaternion.CreateFromEulers(new Vector3(0, 0, Math.Atan2(delta.Y, delta.X)));
            }
        }

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
                InvokeOnPositionUpdate();
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

        private Vector3 m_AngularAcceleration = Vector3.Zero;
        public Vector3 AngularAcceleration
        {
            get
            {
                lock(this)
                {
                    return m_AngularAcceleration;
                }
            }
            set
            {
                lock(this)
                {
                    m_AngularAcceleration = value;
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
                InvokeOnPositionUpdate();
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
                InvokeOnPositionUpdate();
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
                InvokeOnPositionUpdate();
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
                InvokeOnPositionUpdate();
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
                return SceneID == scene.ID;
            }
        }

        public UUID SceneID
        {
            get
            {
                lock (this)
                {
                    return m_CurrentSceneID;
                }
            }
            set
            {
                lock (this)
                {
                    m_CurrentSceneID = value;
                }
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

        private bool m_IsActiveGod;

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

        public OfflineIMServiceInterface OfflineIMService
        {
            get
            {
                return m_OfflineIMService;
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

        public EconomyServiceInterface EconomyService
        {
            get
            {
                return m_EconomyService;
            }
        }
        #endregion

        #region IAgent Methods
        public bool IMSend(GridInstantMessage gim)
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

        public ViewerAgent(UUID agentID,
            string firstName,
            string lastName,
            Uri homeURI,
            UUID sessionID,
            UUID secureSessionID,
            string serviceSessionID,
            ClientInfo clientInfo,
            UserAccount untrustedAccountInfo,
            AgentServiceList serviceList)
        {
            m_TeleportServices = serviceList.GetAll<IAgentTeleportServiceInterface>();
            CollisionPlane = Vector4.UnitW;
            m_AgentID = agentID;
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
            HomeURI = homeURI;
            FirstName = firstName;
            LastName = lastName;
            InitAnimations();
            if (m_EconomyService != null)
            {
                m_EconomyService.Login(Owner, SessionID, m_SecureSessionID);
            }
            OnPositionChange += ChildUpdateOnPositionChange;
        }

        ~ViewerAgent()
        {
            OnPositionChange -= ChildUpdateOnPositionChange;
            lock (this)
            {
                DetachAllAttachments();
                if (m_EconomyService != null)
                {
                    m_EconomyService.Logout(Owner, SessionID, m_SecureSessionID);
                }
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

        #region Physics Linkage
        readonly RwLockedDictionary<UUID, IPhysicsObject> m_PhysicsActors = new RwLockedDictionary<UUID, IPhysicsObject>();

        public RwLockedDictionary<UUID, IPhysicsObject> PhysicsActors
        {
            get
            {
                return m_PhysicsActors;
            }
        }

        public IPhysicsObject PhysicsActor
        {
            get
            {
                lock(this)
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

        /* property here instead of a method. A lot more clear that we update something. */
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public PhysicsStateData PhysicsUpdate
        {
            set
            {
                bool updateProcessed = false;
                lock (this)
                {
                    if (SceneID == value.SceneID && null == m_SittingOnObject)
                    {
                        m_GlobalPosition = value.Position;
                        m_GlobalRotation = value.Rotation;
                        m_Velocity = value.Velocity;
                        m_AngularVelocity = value.AngularVelocity;
                        m_Acceleration = value.Acceleration;
                        m_AngularAcceleration = value.AngularAcceleration;
                        updateProcessed = true;
                    }
                }
                if (updateProcessed)
                {
                    InvokeOnPositionUpdate();
                }
            }
        }
        #endregion

        public RwLockedList<UUID> SelectedObjects(UUID scene)
        {
            AgentCircuit circuit;
            if(Circuits.TryGetValue(scene, out circuit))
            {
                return circuit.SelectedObjects;
            }
            else
            {
                return new RwLockedList<UUID>();
            }
        }

        int m_NextParcelSequenceId;

        public int NextParcelSequenceId
        {
            get
            {
                lock (this)
                {
                    int seqid = ++m_NextParcelSequenceId;
                    if (seqid < 0)
                    {
                        seqid = 1;
                        m_NextParcelSequenceId = seqid;
                    }
                    return seqid;
                }
            }
        }

        public ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions)
        {
            return RequestPermissions(part, itemID, permissions, UUID.Zero);
        }

        public ScriptPermissions RequestPermissions(ObjectPart part, UUID itemID, ScriptPermissions permissions, UUID experienceID)
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
            m.Questions = (UInt32)permissions;
            m.TaskID = part.ID;
            SendMessageAlways(m, part.ObjectGroup.Scene.ID);
            return ScriptPermissions.None;
        }

        public void RevokePermissions(UUID sourceID, UUID itemID, ScriptPermissions permissions)
        {
            m_AnimationController.RevokePermissions(sourceID, permissions);
        }

        [PacketHandler(MessageType.RegionHandshakeReply)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleRegionHandshakeReply(Message m)
        {
            Messages.Region.RegionHandshakeReply rhr = (Messages.Region.RegionHandshakeReply)m;
            AgentCircuit circuit;
            if (Circuits.TryGetValue(rhr.ReceivedOnCircuitCode, out circuit))
            {
                /* Add our agent to scene */
                circuit.Scene.SendAllParcelOverlaysTo(this);
                circuit.Scene.Terrain.UpdateTerrainDataToSingleClient(this, true);
                circuit.Scene.Environment.UpdateWindDataToSingleClient(this);
                circuit.Scene.SendAgentObjectToAllAgents(this);
                ParcelInfo pinfo;
                try
                {
                    pinfo = circuit.Scene.Parcels[GlobalPosition];
                    ParcelProperties props = circuit.Scene.ParcelInfo2ParcelProperties(Owner.ID, pinfo, NextParcelSequenceId, ParcelProperties.RequestResultType.Single);
                    circuit.SendMessage(props);
                }
                catch
                {

                }
                circuit.ScheduleFirstUpdate();
                SendAnimations();
            }
        }

        [PacketHandler(MessageType.CompleteAgentMovement)]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUncalledPrivateCodeRule")]
        void HandleCompleteAgentMovement(Message m)
        {
            Messages.Circuit.CompleteAgentMovement cam = (Messages.Circuit.CompleteAgentMovement)m;
            AgentCircuit circuit;
            if ((this.TeleportFlags & TeleportFlags.ViaLogin) != 0 && (this.TeleportFlags & TeleportFlags.ViaHGLogin) == 0)
            {
                if (Circuits.TryGetValue(cam.ReceivedOnCircuitCode, out circuit))
                {
                    /* switch agent region */
                    if (m_IsActiveGod && !circuit.Scene.IsPossibleGod(new UUI(ID, FirstName, LastName, HomeURI)))
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
                    SceneID = circuit.Scene.ID;

                    Messages.Circuit.AgentMovementComplete amc = new Messages.Circuit.AgentMovementComplete();
                    amc.AgentID = cam.AgentID;
                    amc.ChannelVersion = VersionInfo.SimulatorVersion;
#warning TODO: extract from agent
                    amc.LookAt = new Vector3(1, 1, 0);
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

                    SceneInterface scene = circuit.Scene;
                    if (scene != null)
                    {
                        scene.Environment.UpdateWindlightProfileToClient(this);

                        foreach (ITriggerOnRootAgentActions action in circuit.m_TriggerOnRootAgentActions)
                        {
                            action.TriggerOnRootAgent(ID, scene);
                        }
                    }
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
                c.Scene.Remove(this);
                if (c.Scene.ID != lr.CircuitSceneID)
                {
                    c.Stop();
                    Circuits.Remove(c.CircuitCode, c.Scene.ID);
                    ((UDPCircuitsManager)c.Scene.UDPServer).RemoveCircuit(c);
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
        public void EnableSimulator(UUID originSceneID, uint circuitCode, string capsURI, DestinationInfo destinationInfo)
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

        public void HandleMessage(ChildAgentUpdate m)
        {

        }

        public void HandleMessage(ChildAgentPositionUpdate m)
        {

        }


        public void ScheduleUpdate(ObjectUpdateInfo info, UUID fromSceneID)
        {
            AgentCircuit circuit;
            if(Circuits.TryGetValue(fromSceneID, out circuit))
            {
                circuit.ScheduleUpdate(info);
            }
        }

        public void SendMessageIfRootAgent(Message m, UUID fromSceneID)
        {
            if (fromSceneID == SceneID)
            {
                SendMessageAlways(m, fromSceneID);
            }
        }

        public void SendRegionNotice(UUI fromAvatar, string message, UUID fromSceneID)
        {
            GridInstantMessage im = new GridInstantMessage();
            im.FromAgent = fromAvatar;
            im.ToAgent = Owner;
            im.Dialog = GridInstantMessageDialog.MessageBox;
            im.IsOffline = false;
            im.Position = Vector3.Zero;
            im.Message = message;
            AgentCircuit circuit;
            if (Circuits.TryGetValue(fromSceneID, out circuit))
            {
                if (IsInScene(circuit.Scene))
                {
                    IMSend(im);
                }
            }
        }

        public void SendAlertMessage(string msg, UUID fromSceneID)
        {
            SilverSim.Viewer.Messages.Alert.AlertMessage m = new Messages.Alert.AlertMessage(msg);
            SendMessageAlways(m, fromSceneID);
        }

        public void SendMessageAlways(Message m, UUID fromSceneID)
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
