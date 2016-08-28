// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Main.Common;
using SilverSim.Scene.Management.Scene;
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
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Friends;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using SilverSim.Types.Parcel;
using SilverSim.Types.Primitive;
using SilverSim.Types.Script;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Agent;
using SilverSim.Viewer.Messages.Parcel;
using SilverSim.Viewer.Messages.Script;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;

namespace SilverSim.Viewer.Core
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    public partial class ViewerAgent : IAgent
    {
        private static readonly ILog m_Log = LogManager.GetLogger("VIEWER AGENT");
        public event Action<IObject> OnPositionChange;
        readonly object m_DataLock = new object();
        readonly SceneList m_Scenes;

        #region Agent fields
        readonly UUID m_AgentID;
        private UUID m_CurrentSceneID;
        double m_Health = 100f;
        #endregion

        readonly RwLockedDictionary<UUID, AgentChildInfo> m_ActiveChilds = new RwLockedDictionary<UUID, AgentChildInfo>();

        /** <summary>Key is region ID</summary> */
        public RwLockedDictionary<UUID, AgentChildInfo> ActiveChilds
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

        Vector4 m_CollisionPlane = new Vector4(0, 0, 1, -1);

        public Vector4 CollisionPlane
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_CollisionPlane;
                }
            }
            set
            {
                /* nothing to do for now */
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

        public void RemoveActiveTeleportService(IAgentTeleportServiceInterface service)
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
        public UInt32 LocalID { get; set; }
        public Uri HomeURI { get; private set; }
        public UUID SessionID { get; private set; }
        public double DrawDistance { get; private set; }
        #endregion

        #region AgentLanguage
        string m_AgentLanguage = string.Empty;
        CultureInfo m_AgentCultureInfo;
        readonly object m_AgentLanguageLock = new object();
        public string AgentLanguage
        {
            get
            {
                lock (m_AgentLanguageLock)
                {
                    return m_AgentLanguage;
                }
            }

            internal set
            {
                lock (m_AgentLanguageLock)
                {
                    m_AgentLanguage = value;
                    try
                    {
                        m_AgentCultureInfo = new CultureInfo(value);
#if DEBUG
                        m_Log.DebugFormat("Agent {0} selected CultureInfo {1}", Owner.FullName, value);
#endif
                    }
                    catch
                    {
                        m_AgentCultureInfo = EnUsCulture;
#if DEBUG
                        m_Log.DebugFormat("Agent {0} set to fallback CultureInfo en-US", Owner.FullName);
#endif
                    }
                }
            }
        }

        static readonly CultureInfo EnUsCulture = new CultureInfo("en-US");
        public CultureInfo CurrentCulture
        {
            get
            {
                lock (m_AgentLanguageLock)
                {
                    return m_AgentCultureInfo ?? EnUsCulture;
                }
            }
        }
        #endregion

        public void GetBoundingBox(out BoundingBox box)
        {
            box = new BoundingBox();
            box.CenterOffset = Vector3.Zero;
            box.Size = Size * Rotation;
        }

        /* Circuits: UUID is SceneID */
        public readonly RwLockedDictionary<UUID, AgentCircuit> Circuits = new RwLockedDictionary<UUID, AgentCircuit>();
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
                foreach (Action<IObject> del in e.GetInvocationList().OfType<Action<IObject>>())
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
                lock(m_DataLock)
                {
                    return m_SittingOnObject;
                }
            }
            set
            {
                lock (m_DataLock)
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
                throw new NotSupportedException();
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
                throw new NotSupportedException();
            }
        }

        private double m_HoverHeight;
        public double HoverHeight
        {
            get
            {
                return m_HoverHeight;
            }
            set
            {
                m_HoverHeight = value.Clamp(-2f, 2f);
            }
        }

        private Vector3 m_GlobalPosition = Vector3.Zero;

        public Vector3 Position
        {
            get
            {
                lock(m_DataLock)
                {
                    return (m_SittingOnObject != null) ?
                        m_GlobalPosition - m_SittingOnObject.Position :
                        m_GlobalPosition;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_GlobalPosition = (m_SittingOnObject != null) ?
                        value + m_SittingOnObject.Position :
                        value;
                }
                InvokeOnPositionUpdate();
            }
        }

        private Vector3 m_Velocity = Vector3.Zero;
        public Vector3 Velocity
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_Velocity;
                }
            }
            set
            {
                lock(m_DataLock)
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
                lock(m_DataLock)
                {
                    return m_AngularVelocity;
                }
            }
            set
            {
                lock(m_DataLock)
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
                lock(m_DataLock)
                {
                    return m_AngularAcceleration;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_AngularAcceleration = value;
                }
            }
        }

        public Vector3 GlobalPosition
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_GlobalPosition;
                }
            }
            set
            {
                lock(m_DataLock)
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
                lock(m_DataLock)
                {
                    return (m_SittingOnObject != null) ?
                        m_GlobalPosition - m_SittingOnObject.Position :
                        m_GlobalPosition;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_GlobalPosition = (m_SittingOnObject != null) ?
                        value + m_SittingOnObject.Position :
                        value;
                }
                InvokeOnPositionUpdate();
            }
        }

        private Vector3 m_Acceleration = Vector3.Zero;

        public Vector3 Acceleration
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_Acceleration;
                }
            }
            set
            {
                lock(m_DataLock)
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
                lock (m_DataLock)
                {
                    return (m_SittingOnObject != null) ?
                        m_GlobalRotation * m_SittingOnObject.Rotation :
                        m_GlobalRotation;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_GlobalRotation = (m_SittingOnObject != null) ?
                        value / m_SittingOnObject.Rotation :
                        value;
                }
                InvokeOnPositionUpdate();
            }
        }

        public Quaternion LocalRotation
        {
            get
            {
                lock (m_DataLock)
                {
                    return (m_SittingOnObject != null) ?
                        m_GlobalRotation / m_SittingOnObject.Rotation :
                        m_GlobalRotation;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_GlobalRotation = (m_SittingOnObject != null) ?
                        value * m_SittingOnObject.Rotation :
                        value;
                }
                InvokeOnPositionUpdate();
            }
        }

        public Quaternion Rotation
        {
            get
            {
                lock (m_DataLock)
                {
                    return LocalRotation;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    LocalRotation = value;
                }
            }
        }

        public bool IsInScene(SceneInterface scene)
        {
            lock (m_DataLock)
            {
                return SceneID == scene.ID;
            }
        }

        public UUID SceneID
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
                    foreach (KeyValuePair<Action<object, bool>, object> kvp in waitForRootList)
                    {
                        kvp.Key(kvp.Value, true);
                    }
                }
            }
        }

        public void AddWaitForRoot(SceneInterface scene, Action<object, bool> del, object o)
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

        #region IObject Methods
        public void GetPrimitiveParams(AnArray.Enumerator enumerator, AnArray paramList)
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

                case PrimitiveParamsType.Specular:
                    throw new ArgumentException("PRIM_SPECULAR not allowed for agents");

                case PrimitiveParamsType.Normal:
                    throw new ArgumentException("PRIM_NORMAL not allowed for agents");

                case PrimitiveParamsType.AlphaMode:
                    throw new ArgumentException("PRIM_ALPHA_MODE not allowed for agents");

                case PrimitiveParamsType.Alpha:
                    throw new ArgumentException("PRIM_ALPHA not allowed for agents");

                case PrimitiveParamsType.Projector:
                    throw new ArgumentException("PRIM_PROJECTOR not allowed for agents");

                case PrimitiveParamsType.ProjectorEnabled:
                    throw new ArgumentException("PRIM_PROJECTOR_ENABLED not allowed for agents");

                case PrimitiveParamsType.ProjectorTexture:
                    throw new ArgumentException("PRIM_PROJECTOR_TEXTURE not allowed for agents");

                case PrimitiveParamsType.ProjectorFov:
                    throw new ArgumentException("PRIM_PROJECTOR_FOV not allowed for agents");

                case PrimitiveParamsType.ProjectorFocus:
                    throw new ArgumentException("PRIM_PROJECTOR_FOCUS not allowed for agents");

                case PrimitiveParamsType.ProjectorAmbience:
                    throw new ArgumentException("PRIM_PROJECTOR_AMBIENCE not allowed for agents");

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

                case PrimitiveParamsType.Specular:
                    throw new ArgumentException("PRIM_SPECULAR not allowed for agents");

                case PrimitiveParamsType.Normal:
                    throw new ArgumentException("PRIM_NORMAL not allowed for agents");

                case PrimitiveParamsType.AlphaMode:
                    throw new ArgumentException("PRIM_ALPHA_MODE not allowed for agents");

                case PrimitiveParamsType.Alpha:
                    throw new ArgumentException("PRIM_ALPHA not allowed for agents");

                case PrimitiveParamsType.Projector:
                    throw new ArgumentException("PRIM_PROJECTOR not allowed for agents");

                case PrimitiveParamsType.ProjectorEnabled:
                    throw new ArgumentException("PRIM_PROJECTOR_ENABLED not allowed for agents");

                case PrimitiveParamsType.ProjectorTexture:
                    throw new ArgumentException("PRIM_PROJECTOR_TEXTURE not allowed for agents");

                case PrimitiveParamsType.ProjectorFov:
                    throw new ArgumentException("PRIM_PROJECTOR_FOV not allowed for agents");

                case PrimitiveParamsType.ProjectorFocus:
                    throw new ArgumentException("PRIM_PROJECTOR_FOCUS not allowed for agents");

                case PrimitiveParamsType.ProjectorAmbience:
                    throw new ArgumentException("PRIM_PROJECTOR_AMBIENCE not allowed for agents");

                default:
                    throw new ArgumentException(String.Format("Invalid primitive parameter type {0}", enumerator.Current.AsInt));
            }
        }

        public void GetObjectDetails(AnArray.Enumerator enumerator, AnArray paramList)
        {
            while (enumerator.MoveNext())
            {
                /* LSL ignores non-integer parameters, see http://wiki.secondlife.com/wiki/LlGetObjectDetails. */
                if(enumerator.Current.LSL_Type != LSLValueType.Integer)
                {
                    continue;
                }
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

                    case ObjectDetailsType.LastOwner:
                    case ObjectDetailsType.Owner:
                    case ObjectDetailsType.Creator:
                    case ObjectDetailsType.Root:
                        paramList.Add(ID);
                        break;

                    case ObjectDetailsType.Group:
                        paramList.Add(Group.ID);
                        break;

                    case ObjectDetailsType.RunningScriptCount:
                        {
                            int runningScriptCount = 0;
                            foreach (ObjectGroup grp in Attachments.All)
                            {
                                foreach (ObjectPart part in grp.Values)
                                {
                                    runningScriptCount += part.Inventory.CountRunningScripts;
                                }
                            }
                            paramList.Add(runningScriptCount);
                        }
                        break;

                    case ObjectDetailsType.TotalScriptCount:
                        {
                            int n = 0;
                            foreach (ObjectGroup grp in Attachments.All)
                            {
                                foreach (ObjectPart part in grp.Values)
                                {
                                    n += part.Inventory.CountScripts;
                                }
                            }
                            paramList.Add(n);
                        }
                        break;

                    case ObjectDetailsType.PrimEquivalence:
                        paramList.Add(1);
                        break;

                    case ObjectDetailsType.ScriptTime:
                    case ObjectDetailsType.ServerCost:
                    case ObjectDetailsType.StreamingCost:
                    case ObjectDetailsType.PhysicsCost:
                        paramList.Add(0f);
                        break;

                    case ObjectDetailsType.ScriptMemory:
                    case ObjectDetailsType.CharacterTime:
                    case ObjectDetailsType.AttachedPoint:
                    case ObjectDetailsType.PathfindingType:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.Physics:
                        paramList.Add(true);
                        break;

                    case ObjectDetailsType.Phantom:
                    case ObjectDetailsType.TempOnRez:
                        paramList.Add(false);
                        break;

                    case ObjectDetailsType.HoverHeight:
                        paramList.Add(HoverHeight);
                        break;

                    case ObjectDetailsType.BodyShapeType:
                        byte[] vp = VisualParams;
                        if (vp.Length > 31)
                        {
                            paramList.Add(vp[31] / 255f);
                        }
                        else
                        {
                            paramList.Add(-1f);
                        }   
                        break;

                    case ObjectDetailsType.ClickAction:
                        paramList.Add((int)ClickActionType.None);
                        break;

                    case ObjectDetailsType.Omega:
                        paramList.Add(AngularVelocity);
                        break;

                    case ObjectDetailsType.RenderWeight:
                    default:
                        paramList.Add(-1);
                        break;
                }
            }
        }

        public void PostEvent(IScriptEvent ev)
        {
            /* intentionally left empty */
        }
        #endregion

        #region IAgent Properties
        public bool IsNpc
        {
            get
            {
                return false;
            }
        }

        bool m_IsInMouselook;

        public bool IsInMouselook
        {
            get
            {
                return m_IsInMouselook;
            }
        }


        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

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

        public RwLockedDictionary<UUID, FriendStatus> KnownFriends
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

        public void ClearKnownFriends()
        {
            lock(m_KnownFriendsCacheLock)
            {
                m_KnownFriendsCached = false;
            }
        }


        public double Health
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_Health;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_Health = value.Clamp(0, 100);
#warning Implement death
                }
            }
        }

        public void IncreaseHealth(double v)
        {
            lock(m_DataLock)
            {
                if (v >= 0)
                {
                    m_Health = (m_Health + v).Clamp(0, 100);
                }
            }
        }

        public void DecreaseHealth(double v)
        {
            lock (m_DataLock)
            {
                if (v <= 0)
                {
                    m_Health = (m_Health - v).Clamp(0, 100);
#warning Implement death
                }
            }
        }

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

        public DetectedTypeFlags DetectedType
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

        public void KickUser(string msg)
        {
            Messages.User.KickUser req = new Messages.User.KickUser();
            req.AgentID = Owner.ID;
            req.SessionID = SessionID;
            req.Message = msg;
            req.OnSendCompletion += CloseAllCircuits;
            SendMessageAlways(req, m_CurrentSceneID);
        }

        public void KickUser(string msg, Action<bool> callbackDelegate)
        {
            Messages.User.KickUser req = new Messages.User.KickUser();
            req.OnSendCompletion += callbackDelegate;
            req.OnSendCompletion += CloseAllCircuits;
            req.AgentID = Owner.ID;
            req.SessionID = SessionID;
            req.Message = msg;
            SendMessageAlways(req, m_CurrentSceneID);
        }

        public bool TeleportTo(SceneInterface sceneInterface, string regionName, Vector3 position, Vector3 lookAt, TeleportFlags flags)
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

        public bool TeleportTo(SceneInterface sceneInterface, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags)
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

        public bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, GridVector location, Vector3 position, Vector3 lookAt, TeleportFlags flags)
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

        public bool TeleportTo(SceneInterface sceneInterface, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags)
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

        public bool TeleportTo(SceneInterface sceneInterface, string gatekeeperURI, UUID regionID, Vector3 position, Vector3 lookAt, TeleportFlags flags)
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
        public bool TeleportHome(SceneInterface sceneInterface)
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
        {
            m_Scenes = scenes;
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
            lock (m_DataLock)
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

        /* property here instead of a method. A lot more clear that we update something. */
        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public void PhysicsUpdate(PhysicsStateData value)
        {
            bool updateProcessed = false;
            lock (m_DataLock)
            {
                if (SceneID == value.SceneID && null == m_SittingOnObject)
                {
                    m_GlobalPosition = value.Position;
                    m_GlobalRotation = value.Rotation;
                    m_Velocity = value.Velocity;
                    m_AngularVelocity = value.AngularVelocity;
                    m_Acceleration = value.Acceleration;
                    m_AngularAcceleration = value.AngularAcceleration;
                    m_CollisionPlane = value.CollisionPlane;
                    updateProcessed = true;
                }
            }
            if (updateProcessed)
            {
                InvokeOnPositionUpdate();
            }
        }
        #endregion

        public RwLockedList<UUID> SelectedObjects(UUID scene)
        {
            AgentCircuit circuit;
            return (Circuits.TryGetValue(scene, out circuit)) ?
                circuit.SelectedObjects :
                new RwLockedList<UUID>();
        }

        int m_NextParcelSequenceId;

        public int NextParcelSequenceId
        {
            get
            {
                lock (m_DataLock)
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

        public bool UnSit()
        {
#warning Implement ViewerAgent.UnSit()
            return false;
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
            m.Questions = permissions;
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

        public void SendUpdatedParcelInfo(ParcelInfo pinfo, UUID fromSceneID)
        {
            AgentCircuit circuit;
            if (Circuits.TryGetValue(fromSceneID, out circuit))
            {
                ParcelProperties props = circuit.Scene.ParcelInfo2ParcelProperties(Owner.ID, pinfo, NextParcelSequenceId, ParcelProperties.RequestResultType.Single);
                circuit.SendMessage(props);
            }
        }

        public void RebakeAppearance(Action<string> logOutput = null)
        {
            AgentBakeAppearance.LoadAppearanceFromCurrentOutfit(this, AssetService, true, logOutput);
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
            if (Circuits.TryGetValue(fromSceneID, out circuit) &&
                IsInScene(circuit.Scene))
            {
                IMSend(im);
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

                return data;
            }
        }
    }
}
