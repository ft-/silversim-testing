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

using SilverSim.LL.Messages;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Types.IM;
using System;
using ThreadedClasses;

namespace SilverSim.LL.Core
{
    public class LLAgent : IAgent, IDisposable
    {
        public event Action<IObject> OnPositionChange;

        #region Agent fields
        private readonly AgentAttachments m_Attachments = new AgentAttachments();
        private UUID m_AgentID;
        private UUID m_CurrentSceneID;
        private Vector3 m_AvatarSize = Vector3.Zero;
        #endregion

        #region LLAgent Properties
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

        public Vector3 Size
        {
            get
            {
                lock (this)
                {
                    return m_AvatarSize;
                }
            }
            set
            {
                throw new NotImplementedException();
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

        public AgentAttachments Attachments
        {
            get
            {
                return m_Attachments;
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
        public bool IMSend(GridInstantMessage im)
        {
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

        public void HandleAgentMessage(Message m)
        {
            switch (m.Number)
            {
                case MessageType.RegionHandshakeReply:
                    {
                        Messages.Region.RegionHandshakeReply rhr = (Messages.Region.RegionHandshakeReply)m;
                        Circuit circuit;
                        if (Circuits.TryGetValue(rhr.ReceivedOnCircuitCode, out circuit))
                        {
                            /* Add our agent to scene */
                            circuit.Scene.Add(this);
                        }
                    }
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
                                m_CurrentSceneID = circuit.Scene.ID;

                                Messages.Circuit.AgentMovementComplete amc = new Messages.Circuit.AgentMovementComplete();
                                amc.AgentID = cam.AgentID;
                                amc.ChannelVersion = VersionInfo.SimulatorVersion;
                                amc.LookAt = new Vector3(1, 1, 0); /* TODO: extract from agent */
                                amc.Position = new Vector3(128, 128, 23);
                                amc.SessionID = cam.SessionID;

                                circuit.SendMessage(amc);
                            }
                        }
                    }
                    break;

                case MessageType.LogoutRequest:
                    Messages.Circuit.LogoutRequest lr = (Messages.Circuit.LogoutRequest)m;
                    /* agent wants to logout */
                    break;

                case MessageType.AgentDataUpdateRequest:
                    Messages.Agent.AgentDataUpdateRequest adur = (Messages.Agent.AgentDataUpdateRequest)m;
                    if(adur.AgentID == ID && adur.SessionID == adur.CircuitSessionID)
                    {
                        Circuit circuit;
                        if (Circuits.TryGetValue(adur.ReceivedOnCircuitCode, out circuit))
                        {
                            Messages.Agent.AgentDataUpdate adu = new Messages.Agent.AgentDataUpdate();
                            //TODO: adu.ActiveGroupID;
                            adu.AgentID = ID;
                            adu.FirstName = FirstName;
                            adu.LastName = LastName;
                            //TODO: adu.GroupTitle;
                            circuit.SendMessage(adu);
                        }
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

        public void HandleInventoryMessage(Message m)
        {

        }
    }
}
