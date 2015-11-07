// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types.StructuredData.Json;
using SilverSim.Types.StructuredData.Llsd;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ThreadedClasses;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Scene.Types.Object
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    public partial class ObjectPart : IObject
    {
        private static readonly ILog m_Log = LogManager.GetLogger("OBJECT PART");

        #region Events
        public event Action<ObjectPart, UpdateChangedFlags> OnUpdate;
        public event Action<IObject> OnPositionChange;
        #endregion

        private UInt32 m_LocalID;
        public UInt32 LocalID 
        {
            get
            {
                return m_LocalID;
            }
            set
            {
                bool incSerial;
                lock(this)
                {
                    incSerial = m_LocalID != 0;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.LocalID] = (byte)(value & 0xFF);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.LocalID + 1] = (byte)((value >> 8) & 0xFF);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.LocalID + 2] = (byte)((value >> 16) & 0xFF);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.LocalID + 3] = (byte)((value >> 24) & 0xFF);
                    m_ObjectUpdateInfo.LocalID = value;
                    m_LocalID = value;
                }
                UpdateData(UpdateDataFlags.All, incSerial);
            }
        }

        readonly ObjectUpdateInfo m_ObjectUpdateInfo;
        private UUID m_ID = UUID.Zero;
        private string m_Name = string.Empty;
        private string m_Description = string.Empty;
        private Vector3 m_LocalPosition = Vector3.Zero;
        private Quaternion m_LocalRotation = Quaternion.Identity;
        private Vector3 m_Slice = new Vector3(0, 1, 0);
        private PrimitivePhysicsShapeType m_PhysicsShapeType;
        private PrimitiveMaterial m_Material = PrimitiveMaterial.Wood;
        private Vector3 m_Size = new Vector3(0.5, 0.5, 0.5);
        private string m_SitText = string.Empty;
        private string m_TouchText = string.Empty;
        private Vector3 m_SitTargetOffset = Vector3.Zero;
        private Quaternion m_SitTargetOrientation = Quaternion.Identity;
        private bool m_IsAllowedDrop;
        private ClickActionType m_ClickAction;
        private bool m_IsPassCollisions;
        private bool m_IsPassTouches;
        private Vector3 m_AngularVelocity = Vector3.Zero;
        private Vector3 m_Velocity = Vector3.Zero;
        private UUI m_Creator = UUI.Unknown;
        private Date m_CreationDate = new Date();
        private PrimitiveFlags m_PrimitiveFlags;
        private Map m_DynAttrMap = new Map();

        public Map DynAttrs
        {
            get
            {
                return m_DynAttrMap;
            }
        }

        private SilverSim.Types.Inventory.InventoryPermissionsData m_Permissions = new SilverSim.Types.Inventory.InventoryPermissionsData();

        public int ScriptAccessPin;

        public int LoadedLinkNumber; /* not authoritative, just for loading from XML */

        public class OmegaParam
        {
            #region Constructor
            public OmegaParam()
            {
            }
            #endregion

            #region Fields
            public Vector3 Axis = Vector3.Zero;
            public double Spinrate;
            public double Gain;
            #endregion
        }

        #region Constructor
        public ObjectPart()
        {
            m_Permissions.Base = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
            m_Permissions.Current = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
            m_Permissions.Group = SilverSim.Types.Inventory.InventoryPermissionsMask.None;
            m_Permissions.EveryOne = SilverSim.Types.Inventory.InventoryPermissionsMask.None;
            m_Permissions.NextOwner = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ObjectDataLength] = (byte)60;

            ObjectGroup = null;
            IsChanged = false;
            Inventory = new ObjectPartInventory();
            Inventory.OnChange += OnInventoryChange;
            m_ObjectUpdateInfo = new ObjectUpdateInfo(this);
        }
        #endregion

        #region Permissions
        public bool IsLocked
        {
            get
            {
                return (m_Permissions.Current & InventoryPermissionsMask.Modify) == 0;
            }
        }

        public bool CheckPermissions(UUI accessor, UGI accessorgroup, InventoryPermissionsMask wanted)
        {
            if (ObjectGroup.IsGroupOwned)
            {
                return m_Permissions.CheckGroupPermissions(Creator, ObjectGroup.Group, accessor, accessorgroup, wanted);
            }
            else
            {
                return m_Permissions.CheckAgentPermissions(Creator, Owner, accessor, wanted);
            }
        }
        #endregion

        #region Physics Linkage
        public RwLockedDictionary<UUID, IPhysicsObject> PhysicsActors
        {
            get
            {
                throw new NotSupportedException();
            }
        }
        public IPhysicsObject PhysicsActor
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        public PhysicsStateData PhysicsUpdate
        {
            set
            {

            }
        }
        #endregion

        public void SendKillObject()
        {
            m_ObjectUpdateInfo.KillObject();
            ObjectGroup grp = ObjectGroup;
            if (null != grp)
            {
                SceneInterface scene = grp.Scene;
                if (null != scene)
                {
                    scene.ScheduleUpdate(m_ObjectUpdateInfo);
                }
            }
        }

        public void SendObjectUpdate()
        {
            ObjectGroup grp = ObjectGroup;
            if (null != grp)
            {
                SceneInterface scene = grp.Scene;
                if (null != scene)
                {
                    scene.ScheduleUpdate(m_ObjectUpdateInfo);
                }
            }
        }

        public ObjectUpdateInfo UpdateInfo
        {
            get
            {
                return m_ObjectUpdateInfo;
            }
        }

        void OnInventoryChange(ObjectPartInventory.ChangeAction action, UUID primID, UUID itemID)
        {
            IsChanged = m_IsChangedEnabled;
            lock (m_UpdateDataLock)
            {
                int invSerial = Inventory.InventorySerial;
                m_PropUpdateFixedBlock[(int)PropertiesFixedBlockOffset.InventorySerial] = (byte)(invSerial % 256);
                m_PropUpdateFixedBlock[(int)PropertiesFixedBlockOffset.InventorySerial + 1] = (byte)(invSerial / 256);
            }
            TriggerOnUpdate(UpdateChangedFlags.Inventory);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        internal void TriggerOnUpdate(UpdateChangedFlags flags)
        {
            /* we have to check the ObjectGroup during setup process before using it here */
            if (null == ObjectGroup)
            {
                return;
            }

            ObjectGroup.OriginalAssetID = UUID.Zero;

            var ev = OnUpdate; /* events are not exactly thread-safe, so copy the reference first */
            if (ev != null)
            {
                Action<ObjectPart, UpdateChangedFlags>[] invocationList = (Action<ObjectPart, UpdateChangedFlags>[])ev.GetInvocationList();
                foreach (Action<ObjectPart, UpdateChangedFlags> del in invocationList)
                {
                    try
                    {
                        del(this, flags);
                    }
                    catch (Exception e)
                    {
                        m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace);
                    }
                }
            }

            UpdateData(UpdateDataFlags.All);
            if (ObjectGroup.Scene != null)
            {
                ObjectGroup.Scene.ScheduleUpdate(m_ObjectUpdateInfo);
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        private void TriggerOnPositionChange()
        {
            /* we have to check the ObjectGroup during setup process before using it here */
            if (ObjectGroup == null)
            {
                return;
            }

            var ev = OnPositionChange; /* events are not exactly thread-safe, so copy the reference first */
            if (ev != null)
            {
                Action<IObject>[] invocationList = (Action<IObject>[])ev.GetInvocationList();
                foreach(Action<IObject> del in invocationList)
                {
                    try
                    {
                        del(this);
                    }
                    catch (Exception e)
                    {
                        m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace);
                    }
                }
            }
            UpdateData(UpdateDataFlags.Full | UpdateDataFlags.Terse);
            if (ObjectGroup.Scene != null)
            {
                ObjectGroup.Scene.ScheduleUpdate(m_ObjectUpdateInfo);
            }
        }

        public AssetServiceInterface AssetService /* specific for attachments usage */
        {
            get
            {
                return ObjectGroup.AssetService;
            }
        }


        #region Properties
        public UGI Group
        {
            get
            {
                return ObjectGroup.Group;
            }
            set
            {
                ObjectGroup.Group = value;
            }
        }

        public InventoryPermissionsMask BaseMask
        {
            get
            {
                lock (this)
                {
                    return m_Permissions.Base;
                }
            }
            set
            {
                lock (this)
                {
                    m_Permissions.Base = value;
                }
                lock(m_UpdateDataLock)
                {
                    byte[] b = BitConverter.GetBytes((uint)value);
                    if(!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(b);
                    }
                    Buffer.BlockCopy(b, 0, m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.BaseMask, b.Length);
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public InventoryPermissionsMask OwnerMask
        {
            get
            {
                lock (this)
                {
                    return m_Permissions.Current;
                }
            }
            set
            {
                lock (this)
                {
                    m_Permissions.Current = value;
                }
                lock (m_UpdateDataLock)
                {
                    byte[] b = BitConverter.GetBytes((uint)value);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(b);
                    }
                    Buffer.BlockCopy(b, 0, m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.OwnerMask, b.Length);
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public InventoryPermissionsMask GroupMask
        {
            get
            {
                lock (this)
                {
                    return m_Permissions.Group;
                }
            }
            set
            {
                lock (this)
                {
                    m_Permissions.Group = value;
                }
                lock (m_UpdateDataLock)
                {
                    byte[] b = BitConverter.GetBytes((uint)value);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(b);
                    }
                    Buffer.BlockCopy(b, 0, m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.GroupMask, b.Length);
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public InventoryPermissionsMask EveryoneMask
        {
            get
            {
                lock (this)
                {
                    return m_Permissions.EveryOne;
                }
            }
            set
            {
                lock (this)
                {
                    m_Permissions.EveryOne = value;
                }
                lock (m_UpdateDataLock)
                {
                    byte[] b = BitConverter.GetBytes((uint)value);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(b);
                    }
                    Buffer.BlockCopy(b, 0, m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.EveryoneMask, b.Length);
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public InventoryPermissionsMask NextOwnerMask
        {
            get
            {
                lock (this)
                {
                    return m_Permissions.NextOwner;
                }
            }
            set
            {
                lock (this)
                {
                    m_Permissions.NextOwner = value;
                }
                lock (m_UpdateDataLock)
                {
                    byte[] b = BitConverter.GetBytes((uint)value);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(b);
                    }
                    Buffer.BlockCopy(b, 0, m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.NextOwnerMask, b.Length);
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Date CreationDate
        {
            get
            {
                lock (this)
                {
                    return new Date(m_CreationDate);
                }
            }
            set
            {
                lock (this)
                {
                    m_CreationDate = new Date(value);
                }
                byte[] b = BitConverter.GetBytes(value.DateTimeToUnixTime());
                if(!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }
                lock(m_UpdateDataLock)
                {
                    Buffer.BlockCopy(b, 0, m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.CreationDate, 8);
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public PrimitiveFlags Flags
        {
            get
            {
                lock(this)
                {
                    return m_PrimitiveFlags;
                }
            }
            set
            {
                lock(this)
                {
                    m_PrimitiveFlags = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public UUI Creator
        {
            get
            {
                lock (this)
                {
                    return new UUI(m_Creator);
                }
            }
            set
            {
                lock (this)
                {
                    m_Creator = value;
                }
                lock(m_UpdateDataLock)
                {
                    value.ID.ToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.CreatorID);
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }


        public ObjectGroup ObjectGroup { get; internal set; }
        public ObjectPartInventory Inventory { get; private set; }
        public int SerialNumberLoadedFromDatabase; /* <summary>only to be set within DB modules</summary> */

        public bool IsChanged { get; private set; }

        bool m_IsChangedEnabled;

        public bool IsChangedEnabled
        {
            get
            {
                return m_IsChangedEnabled;
            }
            set
            {
                m_IsChangedEnabled = m_IsChangedEnabled || value;
            }
        }

        public ClickActionType ClickAction
        {
            get
            {
                return m_ClickAction;
            }
            set
            {
                m_ClickAction = value;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ClickAction] = (byte)value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public bool IsPassCollisions
        {
            get
            {
                return m_IsPassCollisions;
            }
            set
            {
                m_IsPassCollisions = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public bool IsPassTouches
        {
            get
            {
                return m_IsPassTouches;
            }
            set
            {
                m_IsPassTouches = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Vector3 Velocity
        {
            get
            {
                return m_Velocity;
            }
            set
            {
                lock (this)
                {
                    m_Velocity = value;
                }
                lock (m_UpdateDataLock)
                {
                    value.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Velocity);
                }
                UpdateData(UpdateDataFlags.Full);
            }
        }

        public Vector3 AngularVelocity
        {
            get
            {
                lock (this)
                {
                    return m_AngularVelocity;
                }
            }
            set
            {
                lock (this)
                {
                    m_AngularVelocity = value;
                }
                lock (m_UpdateDataLock)
                {
                    value.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_AngularVelocity);
                }
                TriggerOnUpdate(0);
            }
        }

        public Vector3 Acceleration
        {
            get
            {
                if(ObjectGroup != null)
                {
                    return ObjectGroup.Acceleration;
                }
                else
                {
                    return Vector3.Zero;
                }
            }
            set
            {
                if(ObjectGroup != null)
                {
                    ObjectGroup.Acceleration = value;
                }
            }
        }

        public Vector3 AngularAcceleration
        {
            get
            {
                if (ObjectGroup != null)
                {
                    return ObjectGroup.AngularAcceleration;
                }
                else
                {
                    return Vector3.Zero;
                }
            }
            set
            {
                if (ObjectGroup != null)
                {
                    ObjectGroup.AngularAcceleration = value;
                }
            }
        }

        public bool IsSoundQueueing
        {
            get
            {
                return m_IsSoundQueueing;
            }
            set
            {
                m_IsSoundQueueing = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int LinkNumber
        {
            get
            {
                ObjectGroup grp = ObjectGroup;
                if(grp != null)
                {
                    try
                    {
                        grp.ForEach(delegate(KeyValuePair<int, ObjectPart> kvp)
                        {
                            if (kvp.Value == this)
                            {
                                throw new ReturnValueException<int>(kvp.Key);
                            }
                        });
                    }
                    catch(ReturnValueException<int> e)
                    {
                        return e.Value;
                    }
                }
                return -1;
            }
        }

        public bool IsAllowedDrop
        {
            get
            {
                return m_IsAllowedDrop;
            }
            set
            {
                m_IsAllowedDrop = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.AllowedDrop);
            }
        }

        public Vector3 SitTargetOffset
        {
            get
            {
                lock (this) return m_SitTargetOffset;
            }
            set
            {
                lock (this) m_SitTargetOffset = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate( 0);
            }
        }

        public Quaternion SitTargetOrientation
        {
            get
            {
                lock (this) return m_SitTargetOrientation;
            }
            set
            {
                lock (this) m_SitTargetOrientation = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public string SitText
        {
            get
            {
                lock(this) return m_SitText;
            }
            set
            {
                lock(this) m_SitText = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public UUI Owner
        {
            get
            {
                if (ObjectGroup != null)
                {
                    return ObjectGroup.Owner;
                }
                return UUI.Unknown;
            }
            set
            {
                if(ObjectGroup != null)
                {
                    ObjectGroup.Owner = value;
                }
            }
        }

        public string TouchText
        {
            get
            {
                lock (this) return m_TouchText;
            }
            set
            {
                lock (this) m_TouchText = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public PrimitivePhysicsShapeType PhysicsShapeType
        {
            get
            {
                return m_PhysicsShapeType;
            }
            set
            {
                m_PhysicsShapeType = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Shape);
            }
        }

        public PrimitiveMaterial Material
        {
            get
            {
                return m_Material;
            }
            set
            {
                m_Material = value;
                switch(value)
                {
                    case PrimitiveMaterial.Stone:
                        m_PhysicsFriction = 0.8;
                        m_PhysicsRestitution = 0.4;
                        break;

                    case PrimitiveMaterial.Metal:
                        m_PhysicsFriction = 0.3;
                        m_PhysicsRestitution = 0.4;
                        break;

                    case PrimitiveMaterial.Glass:
                        m_PhysicsFriction = 0.2;
                        m_PhysicsRestitution = 0.7;
                        break;

                    case PrimitiveMaterial.Wood:
                        m_PhysicsFriction = 0.6;
                        m_PhysicsRestitution = 0.5;
                        break;

                    case PrimitiveMaterial.Flesh:
                        m_PhysicsFriction = 0.9;
                        m_PhysicsRestitution = 0.3;
                        break;

                    case PrimitiveMaterial.Plastic:
                        m_PhysicsFriction = 0.4;
                        m_PhysicsRestitution = 0.7;
                        break;

                    case PrimitiveMaterial.Rubber:
                        m_PhysicsFriction = 0.9;
                        m_PhysicsRestitution = 0.9;
                        break;

                    case PrimitiveMaterial.Light:
                        break;

                    default:
                        break;
                }
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.Material] = (byte)value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Vector3 Size
        {
            get
            {
                lock(this)
                {
                    return m_Size;
                }
            }
            set
            {
                lock(this)
                {
                    m_Size = value;
                }
                lock(m_UpdateDataLock)
                {
                    value.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.Scale);
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Scale);
            }
        }

        public Vector3 Slice
        {
            get
            {
                lock (this)
                {
                    return m_Slice;
                }
            }
            set
            {
                lock(this)
                {
                    m_Slice = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Shape);
            }
        }


        public OmegaParam Omega
        {
            get
            {
                OmegaParam res = new OmegaParam();
                Vector3 angvel = AngularVelocity;
                res.Axis = angvel.Normalize();
                res.Spinrate = angvel.Length;
                res.Gain = 1f;
                return res;
            }
            set
            {
                AngularVelocity = value.Axis * value.Spinrate;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public UUID ID
        {
            get 
            {
                return m_ID; 
            }
            set
            {
                lock(this)
                {
                    m_ID = value;
                    Inventory.PartID = value;
                }
                lock(m_UpdateDataLock)
                {
                    value.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.FullID);
                    value.ToBytes(m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.ObjectID);
                }
                UpdateData(UpdateDataFlags.All);
            }
        }

        public string Name
        {
            get 
            {
                return m_Name; 
            }
            set 
            { 
                m_Name = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public string Description
        {
            get
            {
                return m_Description; 
            }
            set
            {
                m_Description = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }
        #endregion

        public bool IsInScene(SceneInterface scene)
        {
            return true;
        }

        #region Physics Properties

        double m_PhysicsDensity = 1000f;
        double m_PhysicsFriction = 0.6f;
        double m_PhysicsRestitution = 0.5f;
        double m_PhysicsGravityMultiplier = 1f;

        public double PhysicsDensity
        {
            get
            {
                lock (this)
                {
                    return m_PhysicsDensity;
                }
            }
            set
            {
                lock (this)
                {
                    m_PhysicsDensity = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public double PhysicsFriction
        {
            get
            {
                lock (this)
                {
                    return m_PhysicsFriction;
                }
            }
            set
            {
                lock (this)
                {
                    m_PhysicsFriction = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public double PhysicsRestitution
        {
            get
            {
                lock (this)
                {
                    return m_PhysicsRestitution;
                }
            }
            set
            {
                lock (this)
                {
                    m_PhysicsRestitution = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }


        public double PhysicsGravityMultiplier
        {
            get
            {
                lock (this)
                {
                    return m_PhysicsGravityMultiplier;
                }
            }
            set
            {
                lock (this)
                {
                    m_PhysicsGravityMultiplier = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }
        #endregion

        #region Position Properties
        public Vector3 Position
        {
            get
            {
                lock(this)
                {
                    return m_LocalPosition;
                }
            }
            set
            {
                lock(this)
                {
                    m_LocalPosition = value;
                }
                lock(m_UpdateDataLock)
                {
                    value.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Position);
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
                TriggerOnPositionChange();
            }
        }

        public Vector3 GlobalPosition
        {
            get
            {
                lock(this)
                {
                    if(null != ObjectGroup && ObjectGroup.RootPart != this)
                    {
                        return m_LocalPosition + ObjectGroup.RootPart.GlobalPosition;
                    }
                    return m_LocalPosition;
                }
            }
            set
            {
                lock(this)
                {
                    if (null != ObjectGroup && ObjectGroup.RootPart != this)
                    {
                        value -= ObjectGroup.RootPart.GlobalPosition;
                    }
                    m_LocalPosition = value;
                }
                lock (m_UpdateDataLock)
                {
                    value.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Position);
                }

                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
                TriggerOnPositionChange();
            }
        }

        public Vector3 LocalPosition
        {
            get
            {
                lock (this)
                {
                    return m_LocalPosition;
                }
            }
            set
            {
                lock (this)
                {
                    m_LocalPosition = value;
                }
                lock (m_UpdateDataLock)
                {
                    value.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Position);
                }

                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
                TriggerOnPositionChange();
            }
        }
        #endregion

        #region Rotation Properties
        public Quaternion Rotation
        {
            get
            {
                lock (this)
                {
                    return m_LocalRotation;
                }
            }
            set
            {
                lock (this)
                {
                    m_LocalRotation = value;
                }
                lock (m_UpdateDataLock)
                {
                    value.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Rotation);
                }

                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
                TriggerOnPositionChange();
            }
        }

        public Quaternion GlobalRotation
        {
            get
            {
                lock (this)
                {
                    if (ObjectGroup != null && this != ObjectGroup.RootPart)
                    {
                        return m_LocalRotation * ObjectGroup.RootPart.GlobalRotation;
                    }
                    else
                    {
                        return m_LocalRotation;
                    }
                }
            }
            set
            {
                lock (this)
                {
                    if (ObjectGroup != null && this != ObjectGroup.RootPart)
                    {
                        value /= ObjectGroup.RootPart.GlobalRotation;
                    }
                    m_LocalRotation = value;
                }
                lock (m_UpdateDataLock)
                {
                    value.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Rotation);
                }

                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
                TriggerOnPositionChange();
            }
        }

        public Quaternion LocalRotation
        {
            get
            {
                lock (this)
                {
                    return m_LocalRotation;
                }
            }
            set
            {
                lock (this)
                {
                    m_LocalRotation = value;
                }
                lock (m_UpdateDataLock)
                {
                    value.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Rotation);
                }

                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
                TriggerOnPositionChange();
            }
        }
        #endregion

        #region Link / Unlink
        protected internal void Link(ObjectGroup group)
        {
            lock(this)
            {
                if(ObjectGroup != null)
                {
                    throw new ArgumentException("ObjectGroup is already set");
                }
                ObjectGroup = group;
                UpdateData(UpdateDataFlags.All);
            }
        }

        protected internal void Unlink()
        {
            lock (this)
            {
                ObjectGroup = null;
            }
            UpdateData(UpdateDataFlags.All);
        }
        #endregion

        #region Object Details Methods
        public void GetObjectDetails(AnArray.Enumerator enumerator, ref AnArray paramList)
        {
            ObjectGroup.GetObjectDetails(enumerator, ref paramList);
        }
        #endregion

        #region Script Events
        public void PostEvent(IScriptEvent ev)
        {
            Inventory.ForEach(delegate(ObjectPartInventoryItem item)
            {
                if (item.ScriptInstance != null)
                {
                    item.ScriptInstance.PostEvent(ev);
                }
            });
        }
        #endregion

        private void ToUInt16Bytes(double val, double min, double max, byte[] buf, int pos)
        {
            if(val < min)
            {
                val = min;
            }
            else if(val > max)
            {
                val = max;
            }
            val -= min;
            val = val * 65535 / (max - min);
            byte[] b = BitConverter.GetBytes((UInt16)val);
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }
            Buffer.BlockCopy(b, 0, buf, pos, 2);
        }

        public byte[] TerseData
        {
            get
            {
                int pos = 0;
                byte[] data = new byte[44];
                {
                    byte[] b = BitConverter.GetBytes(LocalID);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(b);
                    }
                    Buffer.BlockCopy(b, 0, data, pos, 4);
                    pos += 4;
                }

                if (ObjectGroup != null)
                {
                    data[pos++] = (byte)ObjectGroup.AttachPoint;
                }
                else
                {
                    data[pos++] = 0;
                }
                data[pos++] = 0;
                Position.ToBytes(data, pos);
                pos += 12;
                Vector3 vel = Velocity;
                ToUInt16Bytes(vel.X, -128f, 128f, data, pos);
                pos += 2;
                ToUInt16Bytes(vel.Y, -128f, 128f, data, pos);
                pos += 2;
                ToUInt16Bytes(vel.Z, -128f, 128f, data, pos);
                pos += 2;
                Vector3 accel = Acceleration;
                ToUInt16Bytes(accel.X, -64f, 64f, data, pos);
                pos += 2;
                ToUInt16Bytes(accel.Y, -64f, 64f, data, pos);
                pos += 2;
                ToUInt16Bytes(accel.Z, -64f, 64f, data, pos);
                pos += 2;
                Quaternion rot = Rotation;
                ToUInt16Bytes(rot.X, -1f, 1f, data, pos);
                pos += 2;
                ToUInt16Bytes(rot.Y, -1f, 1f, data, pos);
                pos += 2;
                ToUInt16Bytes(rot.Z, -1f, 1f, data, pos);
                pos += 2;
                ToUInt16Bytes(rot.W, -1f, 1f, data, pos);
                pos += 2;
                Vector3 angvel = AngularVelocity;
                ToUInt16Bytes(angvel.X, -64f, 64f, data, pos);
                pos += 2;
                ToUInt16Bytes(angvel.Y, -64f, 64f, data, pos);
                pos += 2;
                ToUInt16Bytes(angvel.Z, -64f, 64f, data, pos);
                pos += 2;

                return data;
            }
        }

        #region XML Serialization
        public void ToXml(XmlTextWriter writer,XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            ToXml(writer, UUI.Unknown, options);
        }

        public void ToXml(XmlTextWriter writer, UUI nextOwner, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            lock (this)
            {
                writer.WriteStartElement("SceneObjectPart");
                {
                    writer.WriteNamedValue("AllowedDrop", IsAllowedDrop);
                    writer.WriteUUID("CreatorID", Creator.ID);
                    if (!string.IsNullOrEmpty(Creator.CreatorData))
                    {
                        writer.WriteNamedValue("CreatorData", Creator.CreatorData);
                    }

                    writer.WriteUUID("FolderID", UUID.Zero);

                    Inventory.ToXml(writer, nextOwner, options);

                    writer.WriteUUID("UUID", ID);
                    writer.WriteNamedValue("LocalId", LocalID);
                    writer.WriteNamedValue("Name", Name);
                    writer.WriteNamedValue("Material", (int)Material);
                    writer.WriteNamedValue("PassTouch", IsPassTouches);
                    writer.WriteNamedValue("PassCollisions", IsPassCollisions);
                    writer.WriteNamedValue("RegionHandle", ObjectGroup.Scene.RegionData.Location.RegionHandle);
                    writer.WriteNamedValue("ScriptAccessPin", ScriptAccessPin);
                    writer.WriteNamedValue("GroupPosition", GlobalPosition);
                    writer.WriteNamedValue("OffsetPosition", LocalPosition);
                    writer.WriteNamedValue("RotationOffset", LocalRotation);
                    writer.WriteNamedValue("Velocity", Velocity);
                    writer.WriteNamedValue("AngularVelocity", AngularVelocity);
                    writer.WriteNamedValue("Acceleration", Acceleration);
                    writer.WriteNamedValue("Description", Description);
                    TextParam tp = Text;
                    writer.WriteNamedValue("Color", tp.TextColor);
                    writer.WriteNamedValue("Text", tp.Text);
                    writer.WriteNamedValue("SitName", SitText);
                    writer.WriteNamedValue("TouchName", TouchText);
                    writer.WriteNamedValue("LinkNum", LinkNumber);
                    writer.WriteNamedValue("ClickAction", (int)ClickAction);

                    writer.WriteStartElement("Shape");
                    {
                        PrimitiveShape shape = Shape;

                        writer.WriteNamedValue("ProfileCurve", shape.ProfileCurve);
                        writer.WriteNamedValue("TextureEntry", TextureEntryBytes);
                        writer.WriteNamedValue("ExtraParams", ExtraParamsBytes);
                        writer.WriteNamedValue("PathBegin", shape.PathBegin);
                        writer.WriteNamedValue("PathCurve", shape.PathCurve);
                        writer.WriteNamedValue("PathEnd", shape.PathEnd);
                        writer.WriteNamedValue("PathRadiusOffset", shape.PathRadiusOffset);
                        writer.WriteNamedValue("PathRevolutions", shape.PathRevolutions);
                        writer.WriteNamedValue("PathScaleX", shape.PathScaleX);
                        writer.WriteNamedValue("PathScaleY", shape.PathScaleY);
                        writer.WriteNamedValue("PathShearX", shape.PathShearX);
                        writer.WriteNamedValue("PathShearY", shape.PathShearY);
                        writer.WriteNamedValue("PathSkew", shape.PathSkew);
                        writer.WriteNamedValue("PathTaperX", shape.PathTaperX);
                        writer.WriteNamedValue("PathTaperY", shape.PathTaperY);
                        writer.WriteNamedValue("PathTwist", shape.PathTwist);
                        writer.WriteNamedValue("PathTwistBegin", shape.PathTwistBegin);
                        writer.WriteNamedValue("PCode", (int)shape.PCode);
                        writer.WriteNamedValue("ProfileBegin", shape.ProfileBegin);
                        writer.WriteNamedValue("ProfileEnd", shape.ProfileEnd);
                        writer.WriteNamedValue("ProfileHollow", shape.ProfileHollow);
                        writer.WriteNamedValue("State", (int)shape.State);
                        writer.WriteNamedValue("LastAttachPoint", (int)ObjectGroup.AttachPoint);
                        byte profilecurve = shape.ProfileCurve;
                        writer.WriteNamedValue("ProfileShape", ((PrimitiveProfileShape)(profilecurve & 0x0F)).ToString());
                        writer.WriteNamedValue("HollowShape", ((PrimitiveProfileHollowShape)(profilecurve & 0xF0)).ToString());
                        writer.WriteUUID("SculptTexture", shape.SculptMap);
                        writer.WriteNamedValue("SculptType", (int)shape.SculptType);

                        FlexibleParam fp = Flexible;
                        PointLightParam plp = PointLight;

                        writer.WriteNamedValue("FlexiSoftness", fp.Softness);
                        writer.WriteNamedValue("FlexiTension", fp.Tension);
                        writer.WriteNamedValue("FlexiDrag", fp.Friction);
                        writer.WriteNamedValue("FlexiGravity", fp.Gravity);
                        writer.WriteNamedValue("FlexiWind", fp.Wind);
                        writer.WriteNamedValue("FlexiForce", fp.Force, true);

                        writer.WriteNamedValue("LightColor", plp.LightColor, true);
                        writer.WriteNamedValue("LightRadius", plp.Radius);
                        writer.WriteNamedValue("LightCutoff", plp.Cutoff);
                        writer.WriteNamedValue("LightFalloff", plp.Falloff);
                        writer.WriteNamedValue("LightIntensity", plp.Intensity);

                        writer.WriteNamedValue("FlexiEntry", fp.IsFlexible);
                        writer.WriteNamedValue("LightEntry", plp.IsLight);
                        writer.WriteNamedValue("SculptEntry", shape.Type == PrimitiveShapeType.Sculpt);
                        PrimitiveMedia media = Media;
                        if (null != media)
                        {
                            Media.ToXml(writer);
                        }
                    }
                    writer.WriteEndElement();

                    writer.WriteNamedValue("Scale", Size);
                    writer.WriteNamedValue("SitTargetOrientation", SitTargetOrientation);
                    writer.WriteNamedValue("SitTargetPosition", SitTargetOffset);
                    writer.WriteNamedValue("SitTargetPositionLL", SitTargetOffset);
                    writer.WriteNamedValue("SitTargetOrientationLL", SitTargetOrientation);
                    writer.WriteNamedValue("ParentID", ObjectGroup.RootPart.ID);
                    writer.WriteNamedValue("CreationDate", CreationDate.AsUInt);
                    writer.WriteNamedValue("Category", ObjectGroup.Category);
                    if (this == ObjectGroup.RootPart)
                    {
                        writer.WriteNamedValue("SalePrice", ObjectGroup.SalePrice);
                        writer.WriteNamedValue("ObjectSaleType", (int)ObjectGroup.SaleType);
                    }
                    else
                    {
                        writer.WriteNamedValue("SalePrice", 10);
                        writer.WriteNamedValue("ObjectSaleType", (int)ObjectPartInventoryItem.SaleInfoData.SaleType.NoSale);
                    }
                    writer.WriteNamedValue("OwnershipCost", ObjectGroup.OwnershipCost);
                    if (XmlSerializationOptions.None != (options & XmlSerializationOptions.WriteOwnerInfo))
                    {
                        writer.WriteUUID("GroupID", ObjectGroup.Group.ID);
                        writer.WriteUUID("OwnerID", ObjectGroup.Owner.ID);
                        writer.WriteUUID("LastOwnerID", ObjectGroup.LastOwner.ID);
                    }
                    else if(XmlSerializationOptions.None != (options & XmlSerializationOptions.AdjustForNextOwner))
                    {
                        writer.WriteUUID("GroupID", UUID.Zero);
                        writer.WriteUUID("OwnerID", nextOwner.ID);
                        writer.WriteUUID("LastOwnerID", ObjectGroup.Owner.ID);
                    }
                    else
                    {
                        writer.WriteUUID("GroupID", UUID.Zero);
                        writer.WriteUUID("OwnerID", UUID.Zero);
                        writer.WriteUUID("LastOwnerID", ObjectGroup.LastOwner.ID);
                    }
                    writer.WriteNamedValue("BaseMask", (uint)BaseMask);
                    if(XmlSerializationOptions.None != (options & XmlSerializationOptions.AdjustForNextOwner))
                    {
                        writer.WriteNamedValue("OwnerMask", (uint)OwnerMask);
                    }
                    else
                    {
                        writer.WriteNamedValue("OwnerMask", (uint)NextOwnerMask);
                    }
                    writer.WriteNamedValue("GroupMask", (uint)GroupMask);
                    writer.WriteNamedValue("EveryoneMask", (uint)EveryoneMask);
                    writer.WriteNamedValue("NextOwnerMask", (uint)NextOwnerMask);
                    PrimitiveFlags flags = Flags;
                    if(Inventory.CountScripts != 0)
                    {
                        flags |= PrimitiveFlags.Scripted;
                    }
                    writer.WriteNamedValue("Flags", flags.ToString().Replace(",", string.Empty));
                    CollisionSoundParam sp = CollisionSound;
                    writer.WriteUUID("CollisionSound", sp.ImpactSound);
                    writer.WriteNamedValue("CollisionSoundVolume", sp.ImpactVolume);
                    if (!string.IsNullOrEmpty(MediaURL))
                    {
                        writer.WriteNamedValue("MediaUrl", MediaURL);
                    }
                    writer.WriteNamedValue("AttachedPos", ObjectGroup.AttachedPos);

                    writer.WriteStartElement("DynAttrs");
                    LlsdXml.Serialize(DynAttrs, writer);
                    writer.WriteEndElement();

                    writer.WriteNamedValue("TextureAnimation", TextureAnimationBytes);
                    writer.WriteNamedValue("ParticleSystem", ParticleSystemBytes);
                    writer.WriteNamedValue("PayPrice0", 0);
                    writer.WriteNamedValue("PayPrice1", 0);
                    writer.WriteNamedValue("PayPrice2", 0);
                    writer.WriteNamedValue("PayPrice3", 0);
                    writer.WriteNamedValue("PayPrice4", 0);
                    writer.WriteNamedValue("PhysicsShapeType", (int)PhysicsShapeType);
                    writer.WriteNamedValue("Density", (float)PhysicsDensity);
                    writer.WriteNamedValue("Friction", (float)PhysicsFriction);
                    writer.WriteNamedValue("Bounce", (float)PhysicsRestitution);
                    writer.WriteNamedValue("GravityModifier", (float)PhysicsGravityMultiplier);
                }
                writer.WriteEndElement();
            }
        }
        #endregion

        #region XML Deserialization
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        static void ShapeFromXml(ObjectPart part, ObjectGroup rootGroup, XmlTextReader reader)
        {
            PrimitiveShape shape = new PrimitiveShape();
            bool have_attachpoint = false;

            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        switch (reader.Name)
                        {
                            case "ProfileCurve":
                                shape.ProfileCurve = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "TextureEntry":
                                part.TextureEntryBytes = reader.ReadContentAsBase64();
                                break;

                            case "ExtraParams":
                                part.ExtraParamsBytes = reader.ReadContentAsBase64();
                                break;

                            case "PathBegin":
                                shape.PathBegin = (ushort)reader.ReadElementValueAsUInt();
                                break;

                            case "PathCurve":
                                shape.PathCurve = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "PathEnd":
                                shape.PathEnd = (ushort)reader.ReadElementValueAsUInt();
                                break;

                            case "PathRadiusOffset":
                                shape.PathRadiusOffset = (sbyte)reader.ReadElementValueAsUInt();
                                break;

                            case "PathRevolutions":
                                shape.PathRevolutions = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "PathScaleX":
                                shape.PathScaleX = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "PathScaleY":
                                shape.PathScaleY = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "PathShearX":
                                shape.PathShearX = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "PathShearY":
                                shape.PathShearY = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "PathSkew":
                                shape.PathSkew = (sbyte)reader.ReadElementValueAsInt();
                                break;

                            case "PathTaperX":
                                shape.PathTaperX = (sbyte)reader.ReadElementValueAsInt();
                                break;

                            case "PathTaperY":
                                shape.PathTaperY = (sbyte)reader.ReadElementValueAsInt();
                                break;

                            case "PathTwist":
                                shape.PathTwist = (sbyte)reader.ReadElementValueAsInt();
                                break;

                            case "PathTwistBegin":
                                shape.PathTwistBegin = (sbyte)reader.ReadElementValueAsInt();
                                break;

                            case "PCode":
                                shape.PCode = (PrimitiveCode)reader.ReadElementValueAsUInt();
                                break;

                            case "ProfileBegin":
                                shape.ProfileBegin = (ushort)reader.ReadElementValueAsUInt();
                                break;

                            case "ProfileEnd":
                                shape.ProfileEnd = (ushort)reader.ReadElementValueAsUInt();
                                break;

                            case "ProfileHollow":
                                shape.ProfileHollow = (ushort)reader.ReadElementValueAsUInt();
                                break;

                            case "State":
                                shape.State = (byte)reader.ReadElementValueAsUInt();
                                break;

                            case "LastAttachPoint":
                                if(null != rootGroup)
                                {
                                    have_attachpoint = true;
                                    rootGroup.AttachPoint = (AttachmentPoint)reader.ReadElementValueAsUInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "ProfileShape":
                                { 
                                    byte p =  (byte)reader.ReadContentAsEnumValue<PrimitiveProfileShape>();
                                    shape.ProfileCurve = (byte)((shape.ProfileCurve & (byte)0xF0) | p);
                                }
                                break;

                            case "HollowShape":
                                {
                                    byte p = (byte)reader.ReadContentAsEnumValue<PrimitiveProfileHollowShape>();
                                    shape.ProfileCurve = (byte)((shape.ProfileCurve & (byte)0x0F) | p);
                                }
                                break;

                            case "SculptTexture":
                                shape.SculptMap = reader.ReadContentAsUUID();
                                break;

                            case "FlexiSoftness":
                                {
                                    FlexibleParam flexparam = part.Flexible;
                                    flexparam.Softness = reader.ReadElementValueAsInt();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiTension":
                                {
                                    FlexibleParam flexparam = part.Flexible;
                                    flexparam.Tension = reader.ReadElementValueAsDouble();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiDrag":
                                {
                                    FlexibleParam flexparam = part.Flexible;
                                    flexparam.Friction = reader.ReadElementValueAsDouble();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiGravity":
                                {
                                    FlexibleParam flexparam = part.Flexible;
                                    flexparam.Gravity = reader.ReadElementValueAsDouble();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiWind":
                                {
                                    FlexibleParam flexparam = part.Flexible;
                                    flexparam.Wind = reader.ReadElementValueAsDouble();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiForceX":
                                {
                                    FlexibleParam flexparam = part.Flexible;
                                    Vector3 v = flexparam.Force;
                                    v.X = reader.ReadElementValueAsDouble();
                                    flexparam.Force = v;
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiForceY":
                                {
                                    FlexibleParam flexparam = part.Flexible;
                                    Vector3 v = flexparam.Force;
                                    v.Y = reader.ReadElementValueAsDouble();
                                    flexparam.Force = v;
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiForceZ":
                                {
                                    FlexibleParam flexparam = part.Flexible;
                                    Vector3 v = flexparam.Force;
                                    v.Z = reader.ReadElementValueAsDouble();
                                    flexparam.Force = v;
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "LightColorR":
                                {
                                    PointLightParam lightparam = part.PointLight;
                                    Color c = lightparam.LightColor;
                                    c.R = reader.ReadElementValueAsDouble().Clamp(0, 1);
                                    lightparam.LightColor = c;
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightColorG":
                                {
                                    PointLightParam lightparam = part.PointLight;
                                    Color c = lightparam.LightColor;
                                    c.G = reader.ReadElementValueAsDouble().Clamp(0, 1);
                                    lightparam.LightColor = c;
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightColorB":
                                {
                                    PointLightParam lightparam = part.PointLight;
                                    Color c = lightparam.LightColor;
                                    c.B = reader.ReadElementValueAsDouble().Clamp(0, 1);
                                    lightparam.LightColor = c;
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightRadius":
                                {
                                    PointLightParam lightparam = part.PointLight;
                                    lightparam.Radius = reader.ReadElementValueAsDouble();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightCutoff":
                                {
                                    PointLightParam lightparam = part.PointLight;
                                    lightparam.Cutoff = reader.ReadElementValueAsDouble();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightFalloff":
                                {
                                    PointLightParam lightparam = part.PointLight;
                                    lightparam.Falloff = reader.ReadElementValueAsDouble();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightIntensity":
                                {
                                    PointLightParam lightparam = part.PointLight;
                                    lightparam.Intensity = reader.ReadElementValueAsDouble();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "FlexiEntry":
                                {
                                    FlexibleParam flexparam = part.Flexible;
                                    flexparam.IsFlexible = reader.ReadElementValueAsBoolean();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "LightEntry":
                                {
                                    PointLightParam lightparam = part.PointLight;
                                    lightparam.IsLight = reader.ReadElementValueAsBoolean();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "SculptEntry":
                                reader.ReadElementValueAsBoolean();
                                break;

                            case "Media":
                                part.m_Media = PrimitiveMedia.FromXml(reader);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "Shape")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        if (!have_attachpoint && null != rootGroup)
                        {
                            rootGroup.AttachPoint = (AttachmentPoint)shape.State;
                        }
                        part.Shape = shape;
                        return;

                    default:
                        break;
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public static ObjectPart FromXml(XmlTextReader reader, ObjectGroup rootGroup, UUI currentOwner)
        {
            ObjectPart part = new ObjectPart();
            part.Owner = currentOwner;
            int InventorySerial = 1;
            if(reader.IsEmptyElement)
            {
                throw new InvalidObjectXmlException();
            }

            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        switch (reader.Name)
                        {
                            case "AllowedDrop":
                                part.IsAllowedDrop = reader.ReadElementValueAsBoolean();
                                break;

                            case "ForceMouselook": /* boolean field */
                                part.ForceMouselook = reader.ReadElementValueAsBoolean();
                                break;

                            case "CreatorID":
                                {
                                    UUI creator = part.Creator;
                                    creator.ID = reader.ReadContentAsUUID();
                                    part.Creator = creator;
                                }
                                break;

                            case "CreatorData":
                                {
                                    UUI creator = part.Creator;
                                    creator.CreatorData = reader.ReadElementValueAsString();
                                    part.Creator = creator;
                                }
                                break;

                            case "FolderID":
                                reader.ReadToEndElement();
                                break;

                            case "InventorySerial":
                                InventorySerial = reader.ReadElementValueAsInt();
                                break;

                            case "TaskInventory":
                                part.Inventory.FillFromXml(reader, currentOwner);
                                break;

                            case "UUID":
                                part.ID = reader.ReadContentAsUUID();
                                break;

                            case "LocalId":
                                part.LocalID = reader.ReadElementValueAsUInt();
                                break;

                            case "Name":
                                part.Name = reader.ReadElementValueAsString();
                                break;

                            case "Material":
                                part.Material = (PrimitiveMaterial)reader.ReadElementValueAsInt();
                                break;

                            case "UpdateFlag":
                                reader.ReadToEndElement();
                                break;

                            case "PassTouch":
                            case "PassTouches":
                                part.IsPassTouches = reader.ReadElementValueAsBoolean();
                                break;

                            case "PassCollisions":
                                part.IsPassCollisions = reader.ReadElementValueAsBoolean();
                                break;

                            case "RegionHandle":
                                reader.ReadToEndElement(); /* why was this ever serialized, it breaks partly the deduplication attempt */
                                break;

                            case "ScriptAccessPin":
                                part.ScriptAccessPin = reader.ReadElementValueAsInt();
                                break;

                            case "GroupPosition":
                                /* needed in case of attachments */
                                if (null != rootGroup)
                                {
                                    rootGroup.AttachedPos = reader.ReadElementChildsAsVector3();
                                    part.LocalPosition = rootGroup.AttachedPos; /* needs to be restored for load oar */
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "CameraEyeOffset": /* extension, not defined in OpenSim format but in WhiteCore format */
                                part.CameraEyeOffset = reader.ReadElementChildsAsVector3();
                                break;

                            case "CameraAtOffset": /* extension, not defined in OpenSim format but in WhiteCore format */
                                part.CameraAtOffset = reader.ReadElementChildsAsVector3();
                                break;

                            case "OffsetPosition":
                                if (null == rootGroup)
                                {
                                    part.LocalPosition = reader.ReadElementChildsAsVector3();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "RotationOffset":
                                part.LocalRotation = reader.ReadElementChildsAsQuaternion();
                                break;

                            case "Velocity":
                                part.Velocity = reader.ReadElementChildsAsVector3();
                                break;

                            case "RotationalVelocity":
                                part.AngularVelocity = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularVelocity":
                                part.AngularVelocity = reader.ReadElementChildsAsVector3();
                                break;

                            case "SitTargetAvatar":
                                reader.ReadToEndElement();
                                break;

                            case "Acceleration":
                                part.Acceleration = reader.ReadElementChildsAsVector3();
                                break;

                            case "Description":
                                part.Description = reader.ReadElementValueAsString();
                                break;

                            case "Color":
                                {
                                    TextParam tp = part.Text;
                                    tp.TextColor = reader.ReadElementChildsAsColorAlpha();
                                    part.Text = tp;
                                }
                                break;

                            case "Text":
                                {
                                    TextParam tp = part.Text;
                                    tp.Text = reader.ReadElementValueAsString();
                                    part.Text = tp;
                                }
                                break;

                            case "SitName":
                                part.SitText = reader.ReadElementValueAsString();
                                break;

                            case "TouchName":
                                part.TouchText = reader.ReadElementValueAsString();
                                break;

                            case "LinkNum":
                                part.LoadedLinkNumber = reader.ReadElementValueAsInt();
                                break;

                            case "ClickAction":
                                part.ClickAction = (ClickActionType)reader.ReadElementValueAsUInt();
                                break;

                            case "Shape":
                                ShapeFromXml(part, rootGroup, reader);
                                break;

                            case "Scale":
                                part.Size = reader.ReadElementChildsAsVector3();
                                break;

                            case "SitTargetOrientation":
                                part.SitTargetOrientation = reader.ReadElementChildsAsQuaternion();
                                break;

                            case "SitTargetPosition":
                                part.SitTargetOffset = reader.ReadElementChildsAsVector3();
                                break;

                            case "ParentID":
                                reader.ReadToEndElement();
                                break;

                            case "CreationDate":
                                part.CreationDate = Date.UnixTimeToDateTime(reader.ReadElementValueAsULong());
                                break;

                            case "Category":
                                if (null != rootGroup)
                                {
                                    rootGroup.Category = (UInt32)reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "SalePrice":
                                if(null != rootGroup)
                                {
                                    rootGroup.SalePrice = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "ObjectSaleType":
                                if (null != rootGroup)
                                {
                                    rootGroup.SaleType = (InventoryItem.SaleInfoData.SaleType)reader.ReadElementValueAsUInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "OwnershipCost":
                                if (null != rootGroup)
                                {
                                    rootGroup.OwnershipCost = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "GroupID":
                                if (rootGroup != null)
                                {
                                    rootGroup.Group.ID = reader.ReadContentAsUUID();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "OwnerID":
                                /* do not trust this thing ever! */
                                reader.ReadToEndElement();
                                break;

                            case "LastOwnerID":
                                if (null != rootGroup)
                                {
                                    rootGroup.LastOwner.ID = reader.ReadContentAsUUID();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "BaseMask":
                                part.BaseMask = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "OwnerMask":
                                part.OwnerMask = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "GroupMask":
                                part.GroupMask = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "EveryoneMask":
                                part.EveryoneMask = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "NextOwnerMask":
                                part.NextOwnerMask = (InventoryPermissionsMask)reader.ReadElementValueAsUInt();
                                break;

                            case "Flags":
                                part.Flags = reader.ReadContentAsEnum<PrimitiveFlags>();
                                break;

                            case "ObjectFlags":
                                part.Flags = (PrimitiveFlags)reader.ReadElementValueAsUInt();
                                break;

                            case "CollisionSound":
                                {
                                    CollisionSoundParam sp = part.CollisionSound;
                                    sp.ImpactSound = reader.ReadContentAsUUID();
                                    part.CollisionSound = sp;
                                }
                                break;

                            case "CollisionSoundVolume":
                                {
                                    CollisionSoundParam sp = part.CollisionSound;
                                    sp.ImpactVolume = reader.ReadElementValueAsDouble();
                                    part.CollisionSound = sp;
                                }
                                break;

                            case "MediaUrl":
                                part.MediaURL = reader.ReadElementValueAsString();
                                break;

                            case "AttachedPos":
                                if (null != rootGroup)
                                {
                                    rootGroup.AttachedPos = reader.ReadElementChildsAsVector3();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "TextureAnimation":
                                part.TextureAnimationBytes = reader.ReadContentAsBase64();
                                break;

                            case "ParticleSystem":
                                part.ParticleSystemBytes = reader.ReadContentAsBase64();
                                break;

                            case "PayPrice0":
                                if (null != rootGroup)
                                {
                                    rootGroup.PayPrice0 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PayPrice1":
                                if (null != rootGroup)
                                {
                                    rootGroup.PayPrice1 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PayPrice2":
                                if (null != rootGroup)
                                {
                                    rootGroup.PayPrice2 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PayPrice3":
                                if (null != rootGroup)
                                {
                                    rootGroup.PayPrice3 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PayPrice4":
                                if (null != rootGroup)
                                {
                                    rootGroup.PayPrice4 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PhysicsShapeType":
                                part.PhysicsShapeType = (PrimitivePhysicsShapeType)reader.ReadElementValueAsInt();
                                break;

                            case "Density":
                                part.PhysicsDensity = reader.ReadElementValueAsDouble();
                                break;

                            case "Friction":
                                part.PhysicsFriction = reader.ReadElementValueAsDouble();
                                break;

                            case "Bounce":
                                part.PhysicsRestitution = reader.ReadElementValueAsDouble();
                                break;

                            case "GravityModifier":
                                part.PhysicsGravityMultiplier = reader.ReadElementValueAsDouble();
                                break;

                            case "DynAttrs":
                                {
                                    Map damap = LlsdXml.Deserialize(reader) as Map;
                                    if(null != damap)
                                    {
                                        foreach(string key in damap.Keys)
                                        {
                                            if(!(damap[key] is Map))
                                            {
                                                /* remove everything that is not a map */
                                                damap.Remove(key);
                                            }
                                        }
                                        part.m_DynAttrMap = damap;
                                    }
                                }
                                break;

                            case "SitTargetOrientationLL":
                                reader.ReadToEndElement();
                                break;

                            case "SitTargetPositionLL":
                                reader.ReadToEndElement();
                                break;

                            case "Components":
                                {
                                    string json = reader.ReadElementValueAsString();
                                    try
                                    {
                                        if (!string.IsNullOrEmpty(json))
                                        {
                                            using (MemoryStream ms = new MemoryStream(UTF8NoBOM.GetBytes(json)))
                                            {
                                                Map m = Json.Deserialize(ms) as Map;
                                                if (null != m)
                                                {
                                                    if (m.ContainsKey("SavedAttachedPos") && m["SavedAttachedPos"] is AnArray && rootGroup != null)
                                                    {
                                                        AnArray a = (AnArray)(m["SavedAttachedPos"]);
                                                        if (a.Count == 3)
                                                        {
                                                            rootGroup.AttachedPos = new Vector3(a[0].AsReal, a[1].AsReal, a[2].AsReal);
                                                        }
                                                    }

                                                    if (m.ContainsKey("SavedAttachmentPoint") && rootGroup != null)
                                                    {
                                                        rootGroup.AttachPoint = (AttachmentPoint)(m["SavedAttachmentPoint"].AsInt);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {

                                    }
                                }
                                break;

                            default:
                                m_Log.DebugFormat("Unknown element {0} encountered in object xml", reader.Name);
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "SceneObjectPart")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        /* get rid of every flag, we do create internally */
                        if (null != rootGroup)
                        {
                            if ((part.Flags & PrimitiveFlags.Physics) != 0)
                            {
                                rootGroup.IsPhysics = true;
                            }
                            if((part.Flags & PrimitiveFlags.Temporary) != 0)
                            {
                                rootGroup.IsTemporary = true;
                            }
                            if ((part.Flags & PrimitiveFlags.TemporaryOnRez) != 0)
                            {
                                rootGroup.IsTempOnRez = true;
                            }
                        }

                        if(part.Inventory.CountScripts == 0)
                        {
                            part.Flags &= ~(PrimitiveFlags.Touch | PrimitiveFlags.Money);
                        }
                        
                        part.Flags &= ~(
                            PrimitiveFlags.InventoryEmpty | PrimitiveFlags.Physics | PrimitiveFlags.Temporary | PrimitiveFlags.TemporaryOnRez |
                            PrimitiveFlags.AllowInventoryDrop | PrimitiveFlags.ZlibCompressed | PrimitiveFlags.Scripted |
                            PrimitiveFlags.ObjectGroupOwned | PrimitiveFlags.ObjectYouOfficer | PrimitiveFlags.ObjectYouOwner | PrimitiveFlags.ObjectOwnerModify);
                        part.Inventory.InventorySerial = InventorySerial;
                        return part;

                    default:
                        break;
                }
            }
        }
        #endregion

        static UTF8Encoding UTF8NoBOM = new UTF8Encoding(false);
    }
}
