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
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart : IPhysicalObject, ILocalIDAccessor
    {
        private static readonly ILog m_Log = LogManager.GetLogger("OBJECT PART");

        private readonly object m_DataLock = new object();

        #region Events
        public event Action<ObjectPart, UpdateChangedFlags> OnUpdate;
        public event Action<IObject> OnPositionChange;
        #endregion

        private UInt32 m_LocalID;
        UInt32 ILocalIDAccessor.this[UUID sceneID]
        {
            get { return m_LocalID; }

            set
            {
                bool incSerial;
                lock (m_DataLock)
                {
                    incSerial = m_LocalID != 0;
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.LocalID] = (byte)(value & 0xFF);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.LocalID + 1] = (byte)((value >> 8) & 0xFF);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.LocalID + 2] = (byte)((value >> 16) & 0xFF);
                    m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.LocalID + 3] = (byte)((value >> 24) & 0xFF);
                    UpdateInfo.LocalID = value;
                    m_LocalID = value;
                }
                UpdateData(UpdateDataFlags.All, incSerial);
            }
        }

        public ILocalIDAccessor LocalID => this;

        private UUID m_ID = UUID.Zero;
        private string m_Name = string.Empty;
        private string m_Description = string.Empty;
        private Vector3 m_LocalPosition = Vector3.Zero;
        private Vector3 m_SandboxOrigin = Vector3.Zero;
        private Quaternion m_LocalRotation = Quaternion.Identity;
        private Vector3 m_Slice = new Vector3(0, 1, 0);
        private PrimitivePhysicsShapeType m_PhysicsShapeType;
        private PrimitiveMaterial m_Material = PrimitiveMaterial.Wood;
        private Vector3 m_Size = new Vector3(0.5, 0.5, 0.5);
        private string m_SitText = string.Empty;
        private string m_TouchText = string.Empty;
        private bool m_IsSitTargetActive;
        private Vector3 m_SitTargetOffset = Vector3.Zero;
        private string m_SitAnimation = string.Empty;
        private Quaternion m_SitTargetOrientation = Quaternion.Identity;
        private bool m_IsAllowedDrop;
        private ClickActionType m_ClickAction;
        private PassEventMode m_PassCollisionMode;
        private PassEventMode m_PassTouchMode = PassEventMode.Always;
        private Vector3 m_AngularVelocity = Vector3.Zero;
        private Vector3 m_Velocity = Vector3.Zero;
        private UUI m_Creator = UUI.Unknown;
        private Date m_CreationDate = Date.Now;
        private Date m_RezDate = Date.Now;
        private PrimitiveFlags m_PrimitiveFlags;
        private Map m_DynAttrMap = new Map();
        public bool IsScripted { get; private set; }
        private bool m_AllowUnsit = true;
        private bool m_IsScriptedSitOnly;

        public Map DynAttrs => m_DynAttrMap;

        private InventoryPermissionsData m_Permissions = new InventoryPermissionsData();

        public int ScriptAccessPin;

        /** <summary>not authoritative, just for loading from XML</summary> */
        public uint LoadedLocalID;
        /** <summary>not authoritative, just for loading from XML</summary> */
        public int LoadedLinkNumber;

        private PathfindingType m_PathfindingType;

        public PathfindingType PathfindingType
        {
            get
            {
                ObjectGroup grp = ObjectGroup;
                if(grp?.IsAttached ?? false)
                {
                    return PathfindingType.Other;
                }
                return m_PathfindingType;
            }
            set
            {
                if (value != PathfindingType.Avatar)
                {
                    m_PathfindingType = value;
                    TriggerOnUpdate(UpdateChangedFlags.Physics);
                }
            }
        }

        private double m_WalkableCoefficientAvatar = 1;
        private double m_WalkableCoefficientA = 1;
        private double m_WalkableCoefficientB = 1;
        private double m_WalkableCoefficientC = 1;
        private double m_WalkableCoefficientD = 1;

        public double WalkableCoefficientAvatar
        {
            get
            {
                return m_WalkableCoefficientAvatar;
            }
            set
            {
                m_WalkableCoefficientAvatar = Math.Min(value, 0);
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public double WalkableCoefficientA
        {
            get
            {
                return m_WalkableCoefficientA;
            }
            set
            {
                m_WalkableCoefficientA = Math.Min(value, 0);
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public double WalkableCoefficientB
        {
            get
            {
                return m_WalkableCoefficientB;
            }
            set
            {
                m_WalkableCoefficientB = Math.Min(value, 0);
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public double WalkableCoefficientC
        {
            get
            {
                return m_WalkableCoefficientC;
            }
            set
            {
                m_WalkableCoefficientC = Math.Min(value, 0);
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public double WalkableCoefficientD
        {
            get
            {
                return m_WalkableCoefficientD;
            }
            set
            {
                m_WalkableCoefficientD = Math.Min(value, 0);
                TriggerOnUpdate(UpdateChangedFlags.Physics);
            }
        }

        public uint m_PhysicsParameterUpdateSerial = 1;
        public uint m_PhysicsShapeUpdateSerial = 1;

        public uint PhysicsParameterUpdateSerial => m_PhysicsParameterUpdateSerial;
        public uint PhysicsShapeUpdateSerial => m_PhysicsShapeUpdateSerial;

        private void IncrementPhysicsParameterUpdateSerial()
        {
            lock (m_DataLock)
            {
                uint serial = m_PhysicsParameterUpdateSerial + 1;
                if (serial == 0)
                {
                    serial = 1;
                }
                m_PhysicsParameterUpdateSerial = serial;
            }
        }

        private void IncrementPhysicsShapeUpdateSerial()
        {
            lock (m_DataLock)
            {
                uint serial = m_PhysicsShapeUpdateSerial + 1;
                if (serial == 0)
                {
                    serial = 1;
                }
                m_PhysicsShapeUpdateSerial = serial;
            }
        }

        public class OmegaParam
        {
            #region Fields
            public Vector3 Axis = Vector3.Zero;
            public double Spinrate;
            public double Gain;
            #endregion
        }

        public readonly ObjectAnimationController AnimationController;

        #region Constructor
        public ObjectPart()
        {
            m_Permissions.Base = InventoryPermissionsMask.All;
            m_Permissions.Current = InventoryPermissionsMask.All;
            m_Permissions.Group = InventoryPermissionsMask.None;
            m_Permissions.EveryOne = InventoryPermissionsMask.None;
            m_Permissions.NextOwner = InventoryPermissionsMask.All;
            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ObjectDataLength] = (byte)60;

            ObjectGroup = null;
            IsChanged = false;
            Inventory = new ObjectPartInventory();
            Inventory.OnChange += OnInventoryChange;
            Inventory.OnInventoryUpdate += OnInventoryUpdate;
            m_TextureEntryBytes = m_TextureEntry.GetBytes();
            UpdateInfo = new ObjectUpdateInfo(this);
            AnimationController = new ObjectAnimationController(this);

            ID = UUID.Random;
        }

        public ObjectPart(UUID id)
        {
            m_Permissions.Base = InventoryPermissionsMask.All;
            m_Permissions.Current = InventoryPermissionsMask.All;
            m_Permissions.Group = InventoryPermissionsMask.None;
            m_Permissions.EveryOne = InventoryPermissionsMask.None;
            m_Permissions.NextOwner = InventoryPermissionsMask.All;
            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ObjectDataLength] = (byte)60;

            ObjectGroup = null;
            IsChanged = false;
            Inventory = new ObjectPartInventory();
            Inventory.OnChange += OnInventoryChange;
            Inventory.OnInventoryUpdate += OnInventoryUpdate;
            m_TextureEntryBytes = m_TextureEntry.GetBytes();
            UpdateInfo = new ObjectUpdateInfo(this);
            AnimationController = new ObjectAnimationController(this);

            ID = id;
        }

        public ObjectPart(UUID id, ObjectPart fromPart)
        {
            m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ObjectDataLength] = (byte)60;

            ObjectGroup = null;
            IsChanged = false;
            Inventory = new ObjectPartInventory();
            Inventory.OnChange += OnInventoryChange;
            Inventory.OnInventoryUpdate += OnInventoryUpdate;
            m_TextureEntryBytes = m_TextureEntry.GetBytes();
            UpdateInfo = new ObjectUpdateInfo(this);
            AnimationController = new ObjectAnimationController(this);
            m_Shape = fromPart.Shape;
            m_AngularVelocity = fromPart.AngularVelocity;
            m_AttachmentLightLimitIntensity = fromPart.AttachmentLightLimitIntensity;
            BaseMask = fromPart.BaseMask;
            Buoyancy = fromPart.Buoyancy;
            CameraAtOffset = fromPart.CameraAtOffset;
            CameraEyeOffset = fromPart.CameraEyeOffset;
            CollisionSound = fromPart.CollisionSound;
            CreationDate = fromPart.CreationDate;
            Creator = fromPart.Creator;
            Name = fromPart.Name;
            Description = fromPart.Description;
            //this.DynAttrs = fromPart.DynAttrs;
            EveryoneMask = fromPart.EveryoneMask;
            ExtraParamsBytes = fromPart.ExtraParamsBytes;
            FacelightLimitIntensity = fromPart.FacelightLimitIntensity;
            Flags = fromPart.Flags;
            ForceMouselook = fromPart.ForceMouselook;
            LocalPosition = fromPart.LocalPosition;
            LocalRotation = fromPart.LocalRotation;
            Mass = fromPart.Mass;
            Material = fromPart.Material;
            UpdateMedia(fromPart.Media, UUID.Zero);
            MediaURL = fromPart.MediaURL;
            AllowUnsit = fromPart.AllowUnsit;
            IsAllowedDrop = fromPart.IsAllowedDrop;
            IsAttachmentLightsDisabled = fromPart.IsAttachmentLightsDisabled;
            IsBlockGrab = fromPart.IsBlockGrab;
            IsBlockGrabObject = fromPart.IsBlockGrabObject;
            IsDieAtEdge = fromPart.IsDieAtEdge;
            IsFacelightDisabled = fromPart.IsFacelightDisabled;
            IsPhantom = fromPart.IsPhantom;
            IsReturnAtEdge = fromPart.IsReturnAtEdge;
            IsRotateXEnabled = fromPart.IsRotateXEnabled;
            IsRotateYEnabled = fromPart.IsRotateYEnabled;
            IsRotateZEnabled = fromPart.IsRotateZEnabled;
            IsScripted = fromPart.IsScripted;
            IsScriptedSitOnly = fromPart.IsScriptedSitOnly;
            IsSitTargetActive = fromPart.IsSitTargetActive;
            IsSoundQueueing = fromPart.IsSoundQueueing;
            IsVolumeDetect = fromPart.IsVolumeDetect;
            NextOwnerMask = fromPart.NextOwnerMask;
            Omega = fromPart.Omega;
            Owner = fromPart.Owner;
            OwnerMask = fromPart.OwnerMask;
            ParticleSystemBytes = fromPart.ParticleSystemBytes;
            PassCollisionMode = fromPart.PassCollisionMode;
            PassTouchMode = fromPart.PassTouchMode;
            PathfindingType = fromPart.PathfindingType;
            PhysicsDensity = fromPart.PhysicsDensity;
            PhysicsFriction = fromPart.PhysicsFriction;
            PhysicsGravityMultiplier = fromPart.PhysicsGravityMultiplier;
            PhysicsRestitution = fromPart.PhysicsRestitution;
            PhysicsShapeType = fromPart.PhysicsShapeType;
            SandboxOrigin = fromPart.SandboxOrigin;
            ScriptAccessPin = fromPart.ScriptAccessPin;
            SitTargetOffset = fromPart.SitTargetOffset;
            SitTargetOrientation = fromPart.SitTargetOrientation;
            SitText = fromPart.SitText;
            Size = fromPart.Size;
            Slice = fromPart.Slice;
            Sound = fromPart.Sound;
            TextureAnimationBytes = fromPart.TextureAnimationBytes;
            TextureEntryBytes = fromPart.TextureEntryBytes;
            TouchText = fromPart.TouchText;
            VehicleType = fromPart.VehicleType;
            VehicleFlags = fromPart.VehicleFlags;
            VehicleParams = fromPart.VehicleParams;
            WalkableCoefficientA = fromPart.WalkableCoefficientA;
            WalkableCoefficientAvatar = fromPart.WalkableCoefficientAvatar;
            WalkableCoefficientB = fromPart.WalkableCoefficientB;
            WalkableCoefficientC = fromPart.WalkableCoefficientC;
            WalkableCoefficientD = fromPart.WalkableCoefficientD;

            /* only enable IsSandbox and IsPhysics after loading everything else */
            IsSandbox = fromPart.IsSandbox;
            IsPhysics = fromPart.IsPhysics;

            ID = id;
        }
        #endregion

        #region Resource costs
        public double PhysicsCost => 1;
        public double StreamingCost => 1;
        public double SimulationCost => 1;
        public double LinkCost => 1;
        public double ObjectCost => 1;
        #endregion

        #region Update Script Flags
        private void CheckInventoryScripts(ref bool hasTouchEvent, ref bool hasMoneyEvent)
        {
            foreach (ObjectPartInventoryItem item in Inventory.Values)
            {
                ScriptInstance instance = item.ScriptInstance;
                if (item.AssetType == AssetType.LSLText && instance != null && instance.IsRunning)
                {
                    if (instance.HasTouchEvent)
                    {
                        hasTouchEvent = true;
                    }
                    if (instance.HasMoneyEvent)
                    {
                        hasMoneyEvent = true;
                    }
                }
            }
        }

        public void UpdateScriptFlags()
        {
            ObjectPart rootPart = ObjectGroup.RootPart;
            List<ObjectPart> updateList;
            if (rootPart == this)
            {
                /* update all when root part changes */
                updateList = new List<ObjectPart>(ObjectGroup.Values);
            }
            else
            {
                updateList = new List<ObjectPart>
                {
                    this
                };
            }

            foreach (ObjectPart updatePart in updateList)
            {
                bool hasTouchEvent = false;
                bool hasMoneyEvent = false;

                updatePart.CheckInventoryScripts(ref hasTouchEvent, ref hasMoneyEvent);

                var setMask = PrimitiveFlags.None;
                var clrMask = PrimitiveFlags.None;

                if (hasTouchEvent)
                {
                    setMask |= PrimitiveFlags.Touch;
                }
                else
                {
                    clrMask |= PrimitiveFlags.Touch;
                }

                if (hasMoneyEvent)
                {
                    setMask |= PrimitiveFlags.TakesMoney;
                }
                else
                {
                    clrMask |= PrimitiveFlags.TakesMoney;
                }

                updatePart.SetClrFlagsMask(setMask, clrMask);
            }
        }
        #endregion

        #region Touch Handling
        public void PostTouchEvent(TouchEvent e)
        {
            if ((Flags & PrimitiveFlags.Touch) != 0)
            {
                PostEvent(e);
                if (PassTouchMode != PassEventMode.Always)
                {
                    return;
                }
            }
            else if(PassTouchMode == PassEventMode.Never)
            {
                return;
            }

            ObjectGroup grp = ObjectGroup;
            if (grp != null)
            {
                ObjectPart rootPart = grp.RootPart;
                if (rootPart != this && rootPart != null &&
                    (rootPart.Flags & PrimitiveFlags.Touch) != 0)
                {
                    rootPart.PostEvent(e);
                }
            }
        }
        #endregion

        #region Permissions
        public bool IsLocked => (m_Permissions.Current & InventoryPermissionsMask.Modify) == 0;

        public bool CheckPermissions(UUI accessor, UGI accessorgroup, InventoryPermissionsMask wanted) => (ObjectGroup.IsGroupOwned) ?
                m_Permissions.CheckGroupPermissions(Creator, ObjectGroup.Group, accessor, accessorgroup, wanted) :
                m_Permissions.CheckAgentPermissions(Creator, Owner, accessor, wanted);
        #endregion

        public void GetBoundingBox(out BoundingBox box)
        {
            box = new BoundingBox
            {
                CenterOffset = Vector3.Zero,
                Size = Size * Rotation
            };
        }

        public void SendKillObject()
        {
            UpdateInfo.KillObject();
            var grp = ObjectGroup;
            if (grp != null)
            {
                var scene = grp.Scene;
                scene?.ScheduleUpdate(UpdateInfo);
            }
        }

        public void SendObjectUpdate()
        {
            var grp = ObjectGroup;
            if (grp != null)
            {
                var scene = grp.Scene;
                scene?.ScheduleUpdate(UpdateInfo);
            }
        }

        public ObjectUpdateInfo UpdateInfo { get; }

        private void OnInventoryUpdate(ObjectInventoryUpdateInfo info)
        {
            var grp = ObjectGroup;
            if (grp != null)
            {
                var scene = grp.Scene;
                scene?.ScheduleUpdate(info);
            }
        }

        private void OnInventoryChange(ObjectPartInventory.ChangeAction action, UUID primID, UUID itemID)
        {
            if (action == ObjectPartInventory.ChangeAction.NextOwnerAssetID)
            {
                lock (m_UpdateDataLock)
                {
                    int invSerial = Inventory.InventorySerial;
                    m_PropUpdateFixedBlock[(int)PropertiesFixedBlockOffset.InventorySerial] = (byte)(invSerial % 256);
                    m_PropUpdateFixedBlock[(int)PropertiesFixedBlockOffset.InventorySerial + 1] = (byte)(invSerial / 256);
                }
                TriggerOnNextOwnerAssetIDChange();
            }
            else
            {
                IsChanged = m_IsChangedEnabled;
                lock (m_UpdateDataLock)
                {
                    int invSerial = Inventory.InventorySerial;
                    m_PropUpdateFixedBlock[(int)PropertiesFixedBlockOffset.InventorySerial] = (byte)(invSerial % 256);
                    m_PropUpdateFixedBlock[(int)PropertiesFixedBlockOffset.InventorySerial + 1] = (byte)(invSerial / 256);
                    IsScripted = Inventory.CountScripts != 0;
                }
                TriggerOnUpdate(UpdateChangedFlags.Inventory);
            }
        }

        internal void TriggerOnUpdate(UpdateChangedFlags flags)
        {
            /* we have to check the ObjectGroup during setup process before using it here */
            if (ObjectGroup == null)
            {
                return;
            }

            ObjectGroup.OriginalAssetID = UUID.Zero;

            foreach (Action<ObjectPart, UpdateChangedFlags> del in OnUpdate?.GetInvocationList().OfType<Action<ObjectPart, UpdateChangedFlags>>() ?? new Action<ObjectPart, UpdateChangedFlags>[0])
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

            UpdateData(UpdateDataFlags.All);
            ObjectGroup.Scene?.ScheduleUpdate(UpdateInfo);
        }

        internal void TriggerOnNextOwnerAssetIDChange()
        {
            /* we have to check the ObjectGroup during setup process before using it here */
            if (ObjectGroup == null)
            {
                return;
            }

            foreach (Action<ObjectPart, UpdateChangedFlags> del in OnUpdate?.GetInvocationList().OfType<Action<ObjectPart, UpdateChangedFlags>>() ?? new Action<ObjectPart, UpdateChangedFlags>[0])
            {
                try
                {
                    del(this, 0);
                }
                catch (Exception e)
                {
                    m_Log.DebugFormat("Exception {0}:{1} at {2}", e.GetType().Name, e.Message, e.StackTrace);
                }
            }

            UpdateData(UpdateDataFlags.All);
            ObjectGroup.Scene?.ScheduleUpdate(UpdateInfo);
        }

        private void TriggerOnPositionChange()
        {
            /* we have to check the ObjectGroup during setup process before using it here */
            if (ObjectGroup == null)
            {
                return;
            }

            foreach (Action<IObject> del in OnPositionChange?.GetInvocationList().OfType<Action<IObject>>() ?? new Action<IObject>[0])
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
            UpdateData(UpdateDataFlags.Full | UpdateDataFlags.Terse);
            ObjectGroup.Scene?.ScheduleUpdate(UpdateInfo);
        }

        public AssetServiceInterface AssetService
            => ObjectGroup.AssetService; /* specific for attachments usage */

        public DetectedTypeFlags DetectedType
        {
            get
            {
                DetectedTypeFlags flags = 0;
                if (IsScripted)
                {
                    flags |= DetectedTypeFlags.Scripted;
                }
                flags |= ObjectGroup.IsPhysics ?
                    DetectedTypeFlags.Active :
                    DetectedTypeFlags.Passive;
                return flags;
            }
        }
        #region Properties
        public UGI Group
        {
            get { return ObjectGroup.Group; }

            set { ObjectGroup.Group = value; }
        }

        public void SetClrBaseMask(InventoryPermissionsMask setflags, InventoryPermissionsMask clrflags)
        {
            InventoryPermissionsMask value;
            lock (m_DataLock)
            {
                value = (m_Permissions.Base | setflags) & ~clrflags;
                m_Permissions.Base = value;
                InventoryPermissionsMask ownerMask = m_Permissions.Base & InventoryPermissionsMask.ObjectPermissionsChangeable;

                const InventoryPermissionsMask lockBits = InventoryPermissionsMask.Move | InventoryPermissionsMask.Modify;
                m_Permissions.Current = (m_Permissions.Current & lockBits) | (ownerMask & ~lockBits);
            }

            lock (m_UpdateDataLock)
            {
                byte[] b = BitConverter.GetBytes((uint)value);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(b);
                }
                Buffer.BlockCopy(b, 0, m_PropUpdateFixedBlock, (int)PropertiesFixedBlockOffset.BaseMask, b.Length);
            }
            IsChanged = m_IsChangedEnabled;
            TriggerOnUpdate(0);
        }

        public InventoryPermissionsMask BaseMask
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_Permissions.Base;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_Permissions.Base = value;
                    InventoryPermissionsMask ownerMask = value & InventoryPermissionsMask.ObjectPermissionsChangeable;

                    const InventoryPermissionsMask lockBits = InventoryPermissionsMask.Move | InventoryPermissionsMask.Modify;
                    m_Permissions.Current = (m_Permissions.Current & lockBits) | (ownerMask & ~lockBits);
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

        public void SetClrOwnerMask(InventoryPermissionsMask setflags, InventoryPermissionsMask clrflags)
        {
            InventoryPermissionsMask value;
            lock (m_DataLock)
            {
                value = (m_Permissions.Current | setflags) & ~clrflags;
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

        public InventoryPermissionsMask OwnerMask
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_Permissions.Current;
                }
            }
            set
            {
                lock (m_DataLock)
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

        public void SetClrGroupMask(InventoryPermissionsMask setflags, InventoryPermissionsMask clrflags)
        {
            InventoryPermissionsMask value;
            lock (m_DataLock)
            {
                value = (m_Permissions.Group | setflags) & ~clrflags;
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

        public InventoryPermissionsMask GroupMask
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_Permissions.Group;
                }
            }
            set
            {
                lock (m_DataLock)
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

        public void SetClrEveryoneMask(InventoryPermissionsMask setflags, InventoryPermissionsMask clrflags)
        {
            InventoryPermissionsMask value;
            lock (m_DataLock)
            {
                value = (m_Permissions.EveryOne | setflags) & ~clrflags;
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

        public InventoryPermissionsMask EveryoneMask
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_Permissions.EveryOne;
                }
            }
            set
            {
                lock (m_DataLock)
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

        public void SetClrNextOwnerMask(InventoryPermissionsMask setflags, InventoryPermissionsMask clrflags)
        {
            InventoryPermissionsMask value;
            lock (m_DataLock)
            {
                value = (m_Permissions.NextOwner | setflags) & ~clrflags;
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

        public bool IsScriptedSitOnly
        {
            get { return m_IsScriptedSitOnly; }

            set
            {
                m_IsScriptedSitOnly = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public bool AllowUnsit
        {
            get { return m_AllowUnsit; }

            set
            {
                m_AllowUnsit = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public InventoryPermissionsMask NextOwnerMask
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_Permissions.NextOwner;
                }
            }
            set
            {
                lock (m_DataLock)
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

        public Date RezDate
        {
            get
            {
                lock(m_DataLock)
                {
                    return new Date(m_RezDate);
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_RezDate = new Date(value);
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Date CreationDate
        {
            get
            {
                lock (m_DataLock)
                {
                    return new Date(m_CreationDate);
                }
            }
            set
            {
                lock (m_DataLock)
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
                lock(m_DataLock)
                {
                    return m_PrimitiveFlags;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_PrimitiveFlags = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public void SetClrFlagsMask(PrimitiveFlags setMask, PrimitiveFlags clrMask)
        {
            lock (m_DataLock)
            {
                m_PrimitiveFlags = (m_PrimitiveFlags  & ~clrMask) | setMask;
            }
            IsChanged = m_IsChangedEnabled;
            TriggerOnUpdate(0);
        }

        public UUI Creator
        {
            get
            {
                lock (m_DataLock)
                {
                    return new UUI(m_Creator);
                }
            }
            set
            {
                lock (m_DataLock)
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

        public ObjectGroup ObjectGroup { get; set; }
        public ObjectPartInventory Inventory { get; }

        public bool IsChanged { get; private set; }

        private bool m_IsChangedEnabled;

        public bool IsChangedEnabled
        {
            get { return m_IsChangedEnabled; }

            set { m_IsChangedEnabled = m_IsChangedEnabled || value; }
        }

        public ClickActionType ClickAction
        {
            get { return m_ClickAction; }

            set
            {
                m_ClickAction = value;
                m_FullUpdateFixedBlock1[(int)FullFixedBlock1Offset.ClickAction] = (byte)value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public PassEventMode PassCollisionMode
        {
            get { return m_PassCollisionMode; }

            set
            {
                m_PassCollisionMode = value;
                IsChanged = m_IsChangedEnabled;
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(0);
            }
        }

        public PassEventMode PassTouchMode
        {
            get { return m_PassTouchMode; }
            set
            {
                m_PassTouchMode = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Vector3 Velocity
        {
            get { return m_Velocity; }

            set
            {
                lock (m_DataLock)
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
                lock (m_DataLock)
                {
                    return m_AngularVelocity;
                }
            }
            set
            {
                lock (m_DataLock)
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
                return (ObjectGroup != null) ?
                    ObjectGroup.Acceleration :
                    Vector3.Zero;
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
                return (ObjectGroup != null) ?
                    ObjectGroup.AngularAcceleration :
                    Vector3.Zero;
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
            get { return m_IsSoundQueueing; }

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
                        foreach(KeyValuePair<int, ObjectPart> kvp in grp.Key1ValuePairs)
                        {
                            if (kvp.Value == this)
                            {
                                throw new ReturnValueException<int>(kvp.Key);
                            }
                        }
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
            get { return m_IsAllowedDrop; }
            set
            {
                m_IsAllowedDrop = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.AllowedDrop);
            }
        }

        public bool IsSitTargetActive
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_IsSitTargetActive;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_IsSitTargetActive = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public string SitAnimation
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_SitAnimation;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_SitAnimation = value ?? string.Empty;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        private Vector3 m_UnSitTargetOffset = Vector3.Zero;
        private Quaternion m_UnSitTargetOrientation = Quaternion.Identity;
        private bool m_IsUnSitTargetActive;

        public Vector3 UnSitTargetOffset
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_UnSitTargetOffset;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_UnSitTargetOffset = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Quaternion UnSitTargetOrientation
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_UnSitTargetOrientation;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_UnSitTargetOrientation = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public bool IsUnSitTargetActive
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_IsUnSitTargetActive;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_IsUnSitTargetActive = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Vector3 SitTargetOffset
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_SitTargetOffset;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_SitTargetOffset = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Quaternion SitTargetOrientation
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_SitTargetOrientation;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_SitTargetOrientation = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public string SitText
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_SitText;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_SitText = value;
                }
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
                lock (m_DataLock)
                {
                    return m_TouchText;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    m_TouchText = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public PrimitivePhysicsShapeType PhysicsShapeType
        {
            get { return m_PhysicsShapeType; }

            set
            {
                m_PhysicsShapeType = value;
                IsChanged = m_IsChangedEnabled;
                IncrementPhysicsShapeUpdateSerial();
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Shape);
            }
        }

        public PrimitiveMaterial Material
        {
            get { return m_Material; }

            set
            {
                m_Material = value;
                switch (value)
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
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(0);
            }
        }

        public Vector3 Size
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_Size;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_Size = value;
                }
                lock(m_UpdateDataLock)
                {
                    value.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.Scale);
                }
                IsChanged = m_IsChangedEnabled;
                IncrementPhysicsShapeUpdateSerial();
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Scale);
            }
        }

        public Vector3 Slice
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_Slice;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    m_Slice = value;
                }
                IsChanged = m_IsChangedEnabled;
                IncrementPhysicsShapeUpdateSerial();
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(UpdateChangedFlags.Shape);
            }
        }

        public OmegaParam Omega
        {
            get
            {
                Vector3 angvel = AngularVelocity;
                return new OmegaParam
                {
                    Axis = angvel.Normalize(),
                    Spinrate = angvel.Length,
                    Gain = 1f
                };
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
                lock(m_DataLock)
                {
                    return m_ID;
                }
            }
            private set
            {
                lock(m_DataLock)
                {
                    m_ID = value;
                    UpdateInfo.ID = value;
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
            get { return m_Name; }

            set
            {
                m_Name = value.FilterToAscii7Printable().TrimToMaxLength(63);
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public string Description
        {
            get { return m_Description; }

            set
            {
                m_Description = value.FilterToNonControlChars().TrimToMaxLength(127);
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }
        #endregion

        public bool IsInScene(SceneInterface scene) => true;

        #region Position Properties
        private bool HasHitSandboxLimit(Vector3 newPos)
        {
            if((newPos - m_SandboxOrigin).Length > 10)
            {
                return true;
            }

            if(newPos.X < 0 || newPos.Y < 0)
            {
                return true;
            }
            SceneInterface scene = ObjectGroup?.Scene;
            if(scene == null)
            {
                return true;
            }

            return scene.SizeX <= newPos.X || scene.SizeY <= newPos.Y;
        }

        public Vector3 Position
        {
            get
            {
                lock(m_DataLock)
                {
                    return m_LocalPosition;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    if(m_IsSandbox && 
                        ObjectGroup != null && ObjectGroup.RootPart == this &&
                        HasHitSandboxLimit(value))
                    {
                        goto hitsandboxlimit;
                    }
                    m_LocalPosition = value;
                }
                lock(m_UpdateDataLock)
                {
                    value.ToBytes(m_FullUpdateFixedBlock1, (int)FullFixedBlock1Offset.ObjectData_Position);
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
                TriggerOnPositionChange();
                return;

                hitsandboxlimit:
                if (IsPhysics)
                {
                    IsPhysics = false;
                }
                throw new HitSandboxLimitException();
            }
        }

        public Vector3 GlobalPosition
        {
            get
            {
                lock(m_DataLock)
                {
                    if(ObjectGroup != null && ObjectGroup.RootPart != this)
                    {
                        return m_LocalPosition + ObjectGroup.RootPart.GlobalPosition;
                    }
                    return m_LocalPosition;
                }
            }
            set
            {
                lock(m_DataLock)
                {
                    if (ObjectGroup != null)
                    {
                        if (ObjectGroup.RootPart != this)
                        {
                            value -= ObjectGroup.RootPart.GlobalPosition;
                        }
                        else if(m_IsSandbox && (value - m_SandboxOrigin).Length > 10)
                        {
                            goto hitsandboxlimit;
                        }
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
                return;

                hitsandboxlimit:
                if (IsPhysics)
                {
                    IsPhysics = false;
                }
                throw new HitSandboxLimitException();
            }
        }

        public Vector3 LocalPosition
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_LocalPosition;
                }
            }
            set
            {
                lock (m_DataLock)
                {
                    if (m_IsSandbox &&
                        ObjectGroup != null && ObjectGroup.RootPart == this &&
                        HasHitSandboxLimit(value))
                    {
                        goto hitsandboxlimit;
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
                return;

                hitsandboxlimit:
                if (IsPhysics)
                {
                    IsPhysics = false;
                }
                throw new HitSandboxLimitException();
            }
        }
        #endregion

        #region Rotation Properties
        public Quaternion Rotation
        {
            get
            {
                lock (m_DataLock)
                {
                    return m_LocalRotation;
                }
            }
            set
            {
                lock (m_DataLock)
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
                lock (m_DataLock)
                {
                    return (ObjectGroup != null && this != ObjectGroup.RootPart) ?
                        m_LocalRotation * ObjectGroup.RootPart.GlobalRotation :
                        m_LocalRotation;
                }
            }
            set
            {
                lock (m_DataLock)
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
                lock (m_DataLock)
                {
                    return m_LocalRotation;
                }
            }
            set
            {
                lock (m_DataLock)
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
            lock(m_DataLock)
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
            lock (m_DataLock)
            {
                ObjectGroup = null;
            }
            UpdateData(UpdateDataFlags.All);
        }
        #endregion

        #region Object Details Methods
        public void GetObjectDetails(AnArray.Enumerator enumerator, AnArray paramList)
        {
            while (enumerator.MoveNext())
            {
                /* LSL ignores non-integer parameters, see http://wiki.secondlife.com/wiki/LlGetObjectDetails. */
                if (enumerator.Current.LSL_Type != LSLValueType.Integer)
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
                        paramList.Add(Rotation);
                        break;

                    case ObjectDetailsType.Velocity:
                        paramList.Add(Velocity);
                        break;

                    case ObjectDetailsType.Owner:
                        paramList.Add(Owner.ID);
                        break;

                    case ObjectDetailsType.Group:
                        paramList.Add(Group.ID);
                        break;

                    case ObjectDetailsType.Creator:
                        paramList.Add(Creator.ID);
                        break;

                    case ObjectDetailsType.RunningScriptCount:
                        paramList.Add(Inventory.CountRunningScripts);
                        break;

                    case ObjectDetailsType.TotalScriptCount:
                        paramList.Add(Inventory.CountScripts);
                        break;

                    case ObjectDetailsType.ScriptMemory:
                    case ObjectDetailsType.CharacterTime:
                    case ObjectDetailsType.RenderWeight:
                    case ObjectDetailsType.HoverHeight:
                    case ObjectDetailsType.AttachedSlotsAvailable:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.PathfindingType:
                        paramList.Add((int)PathfindingType);
                        break;

                    case ObjectDetailsType.ServerCost:
                    case ObjectDetailsType.StreamingCost:
                    case ObjectDetailsType.PhysicsCost:
                    case ObjectDetailsType.ScriptTime:
                        paramList.Add(0f);
                        break;

                    case ObjectDetailsType.PrimEquivalence:
                        paramList.Add(ObjectGroup.Count);
                        break;

                    case ObjectDetailsType.Root:
                        paramList.Add(ObjectGroup.ID);
                        break;

                    case ObjectDetailsType.AttachedPoint:
                        paramList.Add((int)ObjectGroup.AttachPoint);
                        break;

                    case ObjectDetailsType.Physics:
                        paramList.Add(ObjectGroup.IsPhysics);
                        break;

                    case ObjectDetailsType.Phantom:
                        paramList.Add(ObjectGroup.IsPhantom);
                        break;

                    case ObjectDetailsType.TempOnRez:
                        paramList.Add(ObjectGroup.IsTempOnRez);
                        break;

                    case ObjectDetailsType.LastOwner:
                        paramList.Add(ObjectGroup.LastOwner.ID);
                        break;

                    case ObjectDetailsType.ClickAction:
                        paramList.Add((int)ClickAction);
                        break;

                    case ObjectDetailsType.Omega:
                        paramList.Add(AngularVelocity);
                        break;

                    case ObjectDetailsType.PrimCount:
                        if(ObjectGroup.IsAttached)
                        {
                            paramList.Add(0);
                        }
                        else
                        {
                            paramList.Add(ObjectGroup.Count);
                        }
                        break;

                    case ObjectDetailsType.TotalInventoryCount:
                        paramList.Add(Inventory.Count);
                        break;

                    case ObjectDetailsType.RezzerKey:
                        paramList.Add(ObjectGroup.RezzingObjectID);
                        break;

                    case ObjectDetailsType.GroupTag:
                        paramList.Add(string.Empty);
                        break;

                    case ObjectDetailsType.TempAttached:
                        paramList.Add(ObjectGroup.IsTempAttached);
                        break;

                    case ObjectDetailsType.CreationTime:
                        paramList.Add(CreationDate.AsULong.ToString());
                        break;

                    case ObjectDetailsType.SelectCount:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.SitCount:
                        paramList.Add(ObjectGroup.AgentSitting.Count);
                        break;

                    case ObjectDetailsType.BodyShapeType:
                    default:
                        paramList.Add(-1);
                        break;
                }
            }
        }
        #endregion

        #region Script Events
        private void PostCollisionEvent(CollisionEvent ev)
        {
            foreach(ObjectPartInventoryItem item in Inventory.Values)
            {
                ObjectPartInventoryItem.CollisionFilterParam filter = item.CollisionFilter;
                if(string.IsNullOrEmpty(filter.Name) && filter.ID == UUID.Zero && filter.Type == ObjectPartInventoryItem.CollisionFilterEnum.Accept)
                {
                    /* unfiltered so leave it unmodified */
                    item.ScriptInstance?.PostEvent(ev);
                }
                else
                {
                    /* filtered so we need to check */
                    CollisionEvent evnew = new CollisionEvent
                    {
                        Type = ev.Type
                    };

                    foreach(DetectInfo info in ev.Detected)
                    {
                        bool match = filter.ID == UUID.Zero || info.Key == filter.ID;
                        match = match && (string.IsNullOrEmpty(filter.Name) || info.Name == filter.Name);
                        if((filter.Type == ObjectPartInventoryItem.CollisionFilterEnum.Accept && match) ||
                            (filter.Type == ObjectPartInventoryItem.CollisionFilterEnum.Reject && !match))
                        {
                            continue;
                        }
                        evnew.Detected.Add(info);
                    }

                    if(evnew.Detected.Count != 0)
                    {
                        /* only post event if at least one passed the filter */
                        item.ScriptInstance?.PostEvent(evnew);
                    }
                }
            }
        }

        public void PostEvent(IScriptEvent ev)
        {
            if (ev.GetType() == typeof(CollisionEvent))
            {
                PostCollisionEvent((CollisionEvent)ev);
            }
            else
            {
                foreach (ObjectPartInventoryItem item in Inventory.Values)
                {
                    item.ScriptInstance?.PostEvent(ev);
                }
            }
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
                var data = new byte[44];
                {
                    byte[] b = BitConverter.GetBytes(LocalID[ID]);
                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(b);
                    }
                    Buffer.BlockCopy(b, 0, data, pos, 4);
                    pos += 4;
                }

                data[pos++] = (ObjectGroup != null) ?
                    (byte)ObjectGroup.AttachPoint :
                    (byte)0;

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

                return data;
            }
        }

        public void MoveToTarget(Vector3 target, double tau, UUID notifyPrimId, UUID itemId)
        {
            ObjectGroup.MoveToTarget(target, tau, notifyPrimId, itemId);
        }

        public void StopMoveToTarget()
        {
            ObjectGroup.StopMoveToTarget();
        }

    }
}
