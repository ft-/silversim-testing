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
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Xml;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart : IObject, IDisposable
    {
        private static readonly ILog m_Log = LogManager.GetLogger("OBJECT PART");

        #region Events
        public delegate void OnUpdateDelegate(ObjectPart part, ChangedEvent.ChangedFlags changed);
        public event OnUpdateDelegate OnUpdate;
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
                lock(this)
                {
                    m_ObjectUpdateInfo.LocalID = value;
                    m_LocalID = value;
                }
            }
        }

        private ObjectUpdateInfo m_ObjectUpdateInfo;
        private UUID m_ID = UUID.Zero;
        private string m_Name = string.Empty;
        private string m_Description = string.Empty;
        private Vector3 m_GlobalPosition = Vector3.Zero;
        private Quaternion m_GlobalRotation = Quaternion.Identity;
        private Vector3 m_Slice = new Vector3(0, 1, 0);
        private PrimitivePhysicsShapeType m_PhysicsShapeType = PrimitivePhysicsShapeType.Prim;
        private PrimitiveMaterial m_Material = PrimitiveMaterial.Wood;
        private Vector3 m_Size = new Vector3(0.5, 0.5, 0.5);
        private string m_SitText = string.Empty;
        private string m_TouchText = string.Empty;
        private Vector3 m_SitTargetOffset = Vector3.Zero;
        private Quaternion m_SitTargetOrientation = Quaternion.Identity;
        private bool m_IsAllowedDrop = false;
        private ClickActionType m_ClickAction = ClickActionType.None;
        private bool m_IsPassCollisions = false;
        private bool m_IsPassTouches = false;
        private Vector3 m_LastAttachedPos = Vector3.Zero;
        private Vector3 m_AngularVelocity = Vector3.Zero;
        private Vector3 m_Velocity = Vector3.Zero;
        private UUI m_Creator = UUI.Unknown;
        private Date m_CreationDate = new Date();

        private SilverSim.Types.Inventory.InventoryPermissionsData m_Permissions = new SilverSim.Types.Inventory.InventoryPermissionsData();

        public int ScriptAccessPin = 0;


        public class OmegaParam
        {
            #region Constructor
            public OmegaParam()
            {
            }
            #endregion

            #region Fields
            public Vector3 Axis = Vector3.Zero;
            public double Spinrate = 0;
            public double Gain = 0;
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

            ObjectGroup = null;
            IsChanged = false;
            Inventory = new ObjectPartInventory();
            m_ObjectUpdateInfo = new ObjectUpdateInfo(this);
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            m_ObjectUpdateInfo.KillObject();
            ObjectGroup.Scene.ScheduleUpdate(m_ObjectUpdateInfo);
        }
        #endregion

        #region Permissions
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
        IPhysicsObject m_PhysicsActor = DummyPhysicsObject.SharedInstance;

        public IPhysicsObject PhysicsActor
        {
            get
            {
                lock(this)
                {
                    return m_PhysicsActor;
                }
            }
            set
            {
                if(value == null)
                {
                    throw new ArgumentNullException();
                }
                lock(this)
                {
                    if(m_PhysicsActor.IsPhysicsActive)
                    {
                        throw new InvalidOperationException("PhysicsObject must be deactivated before removing");
                    }
                    m_PhysicsActor = value;
                }
            }
        }
        #endregion

        public void SendKillObject()
        {
            m_ObjectUpdateInfo.KillObject();
            ObjectGroup.Scene.ScheduleUpdate(m_ObjectUpdateInfo);
        }

        public void SendObjectUpdate()
        {
            ObjectGroup.Scene.ScheduleUpdate(m_ObjectUpdateInfo);
        }

        internal void TriggerOnUpdate(ChangedEvent.ChangedFlags flags)
        {
            ObjectGroup.OriginalAssetID = UUID.Zero;

            var ev = OnUpdate; /* events are not exactly thread-safe, so copy the reference first */
            if (ev != null)
            {
                foreach (OnUpdateDelegate del in ev.GetInvocationList())
                {
                    try
                    {
                        del(this, flags);
                    }
                    catch (Exception e)
                    {
                        m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace.ToString());
                    }
                }
            }

            m_ObjectUpdateInfo.IncSerialNumber();
            ObjectGroup.Scene.ScheduleUpdate(m_ObjectUpdateInfo);
        }

        private void TriggerOnPositionChange()
        {
            var ev = OnPositionChange; /* events are not exactly thread-safe, so copy the reference first */
            if (ev != null)
            {
                foreach(Action<IObject> del in ev.GetInvocationList())
                {
                    try
                    {
                        del(this);
                    }
                    catch (Exception e)
                    {
                        m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace.ToString());
                    }
                }
            }
            ObjectGroup.Scene.ScheduleUpdate(m_ObjectUpdateInfo);
        }

        public AssetServiceInterface AssetService /* specific for attachments usage */
        {
            get
            {
                return ObjectGroup.AssetService;
            }
        }


        #region Properties
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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
                TriggerOnUpdate(0);
            }
        }


        public ObjectGroup ObjectGroup { get; private set; }
        public ObjectPartInventory Inventory { get; private set; }

        public bool IsChanged { get; private set; }

        public ClickActionType ClickAction
        {
            get
            {
                return m_ClickAction;
            }
            set
            {
                m_ClickAction = value;
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
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
                m_Velocity = value;
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
                IsChanged = true;
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
                IsChanged = true;
                TriggerOnUpdate(ChangedEvent.ChangedFlags.AllowedDrop);
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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
                TriggerOnUpdate(ChangedEvent.ChangedFlags.Shape);
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
                IsChanged = true;
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
                IsChanged = true;
                TriggerOnUpdate(ChangedEvent.ChangedFlags.Scale);
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
                IsChanged = true;
                TriggerOnUpdate(ChangedEvent.ChangedFlags.Shape);
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
                IsChanged = true;
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
                }
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
                IsChanged = true;
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
                IsChanged = true;
                TriggerOnUpdate(0);
            }
        }
        #endregion

        public bool IsInScene(SceneInterface scene)
        {
            return true;
        }

        #region Media Properties
        public PrimitiveMedia Media = null;
        #endregion

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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
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
                    if(ObjectGroup != null)
                    {
                        if(this != ObjectGroup.RootPart)
                        {
                            return m_GlobalPosition - ObjectGroup.RootPart.GlobalPosition;
                        }
                        else
                        {
                            return m_GlobalPosition;
                        }
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
                    if (ObjectGroup != null)
                    {
                        if (this != ObjectGroup.RootPart)
                        {
                            m_GlobalPosition = value + ObjectGroup.RootPart.GlobalPosition;
                        }
                        else
                        {
                            m_GlobalPosition = value;
                        }
                    }
                    else
                    {
                        m_GlobalPosition = value;
                    }
                }
                IsChanged = true;
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
                    return m_GlobalPosition;
                }
            }
            set
            {
                lock(this)
                {
                    m_GlobalPosition = value;
                }
                IsChanged = true;
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
                    if (ObjectGroup != null)
                    {
                        if (this != ObjectGroup.RootPart)
                        {
                            return m_GlobalPosition - ObjectGroup.RootPart.GlobalPosition;
                        }
                        else
                        {
                            return m_GlobalPosition;
                        }
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
                    if (ObjectGroup != null)
                    {
                        if (this != ObjectGroup.RootPart)
                        {
                            m_GlobalPosition = value + ObjectGroup.RootPart.GlobalPosition;
                        }
                        else
                        {
                            m_GlobalPosition = value;
                        }
                    }
                    else
                    {
                        m_GlobalPosition = value;
                    }
                }
                IsChanged = true;
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
                    if (ObjectGroup != null)
                    {
                        if (this != ObjectGroup.RootPart)
                        {
                            return m_GlobalRotation / ObjectGroup.RootPart.GlobalRotation;
                        }
                        else
                        {
                            return m_GlobalRotation;
                        }
                    }
                    else
                    {
                        return m_GlobalRotation;
                    }
                }
            }
            set
            {
                lock (this)
                {
                    if (ObjectGroup != null)
                    {
                        if (this != ObjectGroup.RootPart)
                        {
                            m_GlobalRotation = value * ObjectGroup.RootPart.GlobalRotation;
                        }
                        else
                        {
                            m_GlobalRotation = value;
                        }
                    }
                    else
                    {
                        m_GlobalRotation = value;
                    }
                }
                IsChanged = true;
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
                    return m_GlobalRotation;
                }
            }
            set
            {
                lock (this)
                {
                    m_GlobalRotation = value;
                }
                IsChanged = true;
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
                    if (ObjectGroup != null)
                    {
                        if (this != ObjectGroup.RootPart)
                        {
                            return m_GlobalRotation / ObjectGroup.RootPart.GlobalRotation;
                        }
                        else
                        {
                            return m_GlobalRotation;
                        }
                    }
                    else
                    {
                        return m_GlobalRotation;
                    }
                }
            }
            set
            {
                lock (this)
                {
                    if (ObjectGroup != null)
                    {
                        if (this != ObjectGroup.RootPart)
                        {
                            m_GlobalRotation = value * ObjectGroup.RootPart.GlobalRotation;
                        }
                        else
                        {
                            m_GlobalRotation = value;
                        }
                    }
                    else
                    {
                        m_GlobalRotation = value;
                    }
                }
                IsChanged = true;
                TriggerOnUpdate(0);
                TriggerOnPositionChange();
            }
        }
        #endregion

        #region Link / Unlink
        protected internal void LinkToObjectGroup(ObjectGroup group)
        {
            lock(this)
            {
                if(ObjectGroup != null)
                {
                    throw new ArgumentException();
                }
                ObjectGroup = group;
            }
        }

        protected internal void UnlinkFromObjectGroup()
        {
            lock (this)
            {
                ObjectGroup = null;
            }
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

                data[pos++] = (byte)ObjectGroup.AttachPoint;
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
            ToXml(writer, UUID.Zero, options);
        }

        public void ToXml(XmlTextWriter writer, UUID nextOwner, XmlSerializationOptions options = XmlSerializationOptions.None)
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
                    writer.WriteNamedValue("PassTouches", IsPassTouches);
                    writer.WriteNamedValue("PassCollisions", IsPassCollisions);
                    writer.WriteNamedValue("RegionHandle", ObjectGroup.Scene.RegionData.Location.RegionHandle);
                    writer.WriteNamedValue("ScriptAccessPin", ScriptAccessPin);
                    writer.WriteNamedValue("GroupPosition", LocalPosition);
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
                        writer.WriteNamedValue("ProfileShape", (int)shape.Type);
                        writer.WriteNamedValue("HollowShape", (int)shape.ProfileHollow);
                        writer.WriteUUID("SculptTexture", shape.SculptMap);
                        writer.WriteNamedValue("SculptType", (int)shape.SculptType);

                        FlexibleParam fp = Flexible;
                        PointLightParam plp = PointLight;
                        ProjectionParam pp = Projection;

                        writer.WriteNamedValue("FlexiSoftness", fp.Softness);
                        writer.WriteNamedValue("FlexiTension", fp.Tension);
                        writer.WriteNamedValue("FlexiDrag", fp.Friction);
                        writer.WriteNamedValue("FlexiGravity", fp.Gravity);
                        writer.WriteNamedValue("FlexiWind", fp.Wind);
                        writer.WriteNamedValue("FlexiForce", fp.Force);

                        writer.WriteNamedValue("LightColor", plp.LightColor);
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
                    //writer.WriteNamedValue("Category");
                    //writer.WriteNamedValue("SalePrice", );
                    //writer.WriteNamedValue("ObjectSaleType", );
                    //writer.WriteNamedValue("OwnershipCost", );
                    if (XmlSerializationOptions.None != (options & XmlSerializationOptions.WriteOwnerInfo))
                    {
                        writer.WriteUUID("GroupID", ObjectGroup.Group.ID);
                        writer.WriteUUID("OwnerID", ObjectGroup.Owner.ID);
                        writer.WriteUUID("LastOwnerID", ObjectGroup.LastOwner.ID);
                    }
                    else if(XmlSerializationOptions.None != (options & XmlSerializationOptions.AdjustForNextOwner))
                    {
                        writer.WriteUUID("GroupID", UUID.Zero);
                        writer.WriteUUID("OwnerID", nextOwner);
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
                    writer.WriteNamedValue("Flags", "None");
                    CollisionSoundParam sp = CollisionSound;
                    writer.WriteUUID("CollisionSound", sp.ImpactSound);
                    writer.WriteNamedValue("CollisionSoundVolume", sp.ImpactVolume);
                    if (!string.IsNullOrEmpty(MediaURL))
                    {
                        writer.WriteNamedValue("MediaUrl", MediaURL);
                    }
                    writer.WriteNamedValue("AttachedPos", ObjectGroup.AttachedPos);
                    //DynAttrs
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
        public static ObjectPart FromXml(XmlTextReader reader)
        {
            ObjectGroup group = new ObjectGroup();
            ObjectPart rootPart = null;
            if(reader.IsEmptyElement)
            {
                throw new InvalidObjectXmlException();
            }

            for(;;)
            {
                if(!reader.Read())
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
                        reader.Skip();
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "SceneObjectPart")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return null;

                    default:
                        break;
                }
        }
        #endregion
    }
}
