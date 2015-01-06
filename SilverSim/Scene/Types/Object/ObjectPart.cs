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
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Threading;
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

        private SilverSim.Types.Inventory.InventoryPermissionsMask m_BaseMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
        private SilverSim.Types.Inventory.InventoryPermissionsMask m_OwnerMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;
        private SilverSim.Types.Inventory.InventoryPermissionsMask m_GroupMask = SilverSim.Types.Inventory.InventoryPermissionsMask.None;
        private SilverSim.Types.Inventory.InventoryPermissionsMask m_EveryoneMask = SilverSim.Types.Inventory.InventoryPermissionsMask.None;
        private SilverSim.Types.Inventory.InventoryPermissionsMask m_NextOwnerMask = SilverSim.Types.Inventory.InventoryPermissionsMask.All;

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
                    return m_BaseMask;
                }
            }
            set
            {
                lock (this)
                {
                    m_BaseMask = value;
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
                    return m_OwnerMask;
                }
            }
            set
            {
                lock (this)
                {
                    m_OwnerMask = value;
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
                    return m_GroupMask;
                }
            }
            set
            {
                lock (this)
                {
                    m_GroupMask = value;
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
                    return m_EveryoneMask;
                }
            }
            set
            {
                lock (this)
                {
                    m_EveryoneMask = value;
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
                    return m_NextOwnerMask;
                }
            }
            set
            {
                lock (this)
                {
                    m_NextOwnerMask = value;
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
    }
}
