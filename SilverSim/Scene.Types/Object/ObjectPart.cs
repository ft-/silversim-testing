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
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using SilverSim.Types.StructuredData.Json;
using SilverSim.Types.StructuredData.Llsd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

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

        public int LoadedLinkNumber; /* not authoritative, just for loading from XML */

        private PathfindingType m_PathfindingType;

        public PathfindingType PathfindingType
        {
            get
            {
                ObjectGroup grp = ObjectGroup;
                if(grp?.IsAttached == true)
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

        public uint PhysicsParameterUpdateSerial => m_PhysicsParameterUpdateSerial;

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

            foreach (Action<ObjectPart, UpdateChangedFlags> del in OnUpdate?.GetInvocationList() ?? new Delegate[0])
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

            foreach (Action<ObjectPart, UpdateChangedFlags> del in OnUpdate?.GetInvocationList() ?? new Delegate[0])
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

            foreach(Action<IObject> del in OnPositionChange?.GetInvocationList() ?? new Delegate[0])
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
        public void PostEvent(IScriptEvent ev)
        {
            foreach (ObjectPartInventoryItem item in Inventory.Values)
            {
                item.ScriptInstance?.PostEvent(ev);
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

        #region XML Serialization
        public void ToXml(XmlTextWriter writer,XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            ToXml(writer, UUI.Unknown, options);
        }

        public void ToXml(XmlTextWriter writer, UUI nextOwner, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            lock (m_DataLock)
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
                    writer.WriteNamedValue("LocalId", 0);
                    writer.WriteNamedValue("Name", Name);
                    writer.WriteNamedValue("Material", (int)Material);
                    writer.WriteNamedValue("IsRotateXEnabled", ObjectGroup.IsRotateXEnabled);
                    writer.WriteNamedValue("IsRotateYEnabled", ObjectGroup.IsRotateYEnabled);
                    writer.WriteNamedValue("IsRotateZEnabled", ObjectGroup.IsRotateZEnabled);
                    writer.WriteNamedValue("PassTouch", PassTouchMode != PassEventMode.Never);
                    writer.WriteNamedValue("PassTouchAlways", PassTouchMode == PassEventMode.Always);
                    writer.WriteNamedValue("PassCollisions", PassCollisionMode != PassEventMode.Never);
                    writer.WriteNamedValue("PassCollisionsAlways", PassCollisionMode != PassEventMode.Always);
                    writer.WriteNamedValue("RegionHandle", ObjectGroup.Scene.GridPosition.RegionHandle);
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
                        if (media != null)
                        {
                            writer.WriteStartElement("Media");
                            using (var ms = new MemoryStream())
                            {
                                using (XmlTextWriter innerWriter = ms.UTF8XmlTextWriter())
                                {
                                    Media.ToXml(writer);
                                }
                                writer.WriteValue(ms.ToArray().FromUTF8Bytes());
                            }
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();

                    writer.WriteNamedValue("Scale", Size);
                    writer.WriteNamedValue("SitTargetActive", IsSitTargetActive);
                    writer.WriteNamedValue("SitTargetOrientation", SitTargetOrientation);
                    writer.WriteNamedValue("SitTargetPosition", SitTargetOffset);
                    writer.WriteNamedValue("SitTargetPositionLL", SitTargetOffset);
                    writer.WriteNamedValue("SitTargetOrientationLL", SitTargetOrientation);
                    writer.WriteNamedValue("ParentID", ObjectGroup.RootPart.ID);
                    writer.WriteNamedValue("CreationDate", CreationDate.AsULong.ToString());
                    if ((options & XmlSerializationOptions.WriteRezDate) != XmlSerializationOptions.None)
                    {
                        writer.WriteNamedValue("RezDate", RezDate.AsULong.ToString());
                    }
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
                    else if (XmlSerializationOptions.None != (options & XmlSerializationOptions.AdjustForNextOwner))
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
                    if (XmlSerializationOptions.None == (options & XmlSerializationOptions.AdjustForNextOwner))
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
                    var flagsStrs = new List<string>();
                    if ((flags & PrimitiveFlags.Physics) != 0)
                    {
                        flagsStrs.Add("Physics");
                    }
                    if (IsScripted)
                    {
                        flagsStrs.Add("Scripted");
                    }
                    if ((flags & PrimitiveFlags.Touch) != 0)
                    {
                        flagsStrs.Add("Touch");
                    }
                    if ((flags & PrimitiveFlags.TakesMoney) != 0)
                    {
                        flagsStrs.Add("Money");
                    }
                    if ((flags & PrimitiveFlags.Phantom) != 0)
                    {
                        flagsStrs.Add("Phantom");
                    }
                    if ((flags & PrimitiveFlags.IncludeInSearch) != 0)
                    {
                        flagsStrs.Add("JointWheel"); /* yes, its name is a messed up naming in OpenSim object xml */
                    }
                    if ((flags & PrimitiveFlags.AllowInventoryDrop) != 0)
                    {
                        flagsStrs.Add("AllowInventoryDrop");
                    }
                    if ((flags & PrimitiveFlags.CameraDecoupled) != 0)
                    {
                        flagsStrs.Add("CameraDecoupled");
                    }
                    if ((flags & PrimitiveFlags.AnimSource) != 0)
                    {
                        flagsStrs.Add("AnimSource");
                    }
                    if ((flags & PrimitiveFlags.CameraSource) != 0)
                    {
                        flagsStrs.Add("CameraSource");
                    }
                    if ((flags & PrimitiveFlags.TemporaryOnRez) != 0)
                    {
                        flagsStrs.Add("TemporaryOnRez");
                    }
                    if ((flags & PrimitiveFlags.Temporary) != 0)
                    {
                        flagsStrs.Add("Temporary");
                    }
                    writer.WriteNamedValue("Flags", string.Join(",", flagsStrs));
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

                    if (XmlSerializationOptions.None != (options & XmlSerializationOptions.WriteOwnerInfo))
                    {
                        writer.WriteNamedValue("RezzerID", ObjectGroup.RezzingObjectID);
                    }
                    writer.WriteNamedValue("TextureAnimation", TextureAnimationBytes);
                    writer.WriteNamedValue("ParticleSystem", ParticleSystemBytes);
                    writer.WriteNamedValue("PayPrice0", ObjectGroup.PayPrice0);
                    writer.WriteNamedValue("PayPrice1", ObjectGroup.PayPrice1);
                    writer.WriteNamedValue("PayPrice2", ObjectGroup.PayPrice2);
                    writer.WriteNamedValue("PayPrice3", ObjectGroup.PayPrice3);
                    writer.WriteNamedValue("PayPrice4", ObjectGroup.PayPrice4);
                    writer.WriteNamedValue("Buoyancy", (float)Buoyancy);
                    writer.WriteNamedValue("PhysicsShapeType", (int)PhysicsShapeType);
                    writer.WriteNamedValue("VolumeDetectActive", IsVolumeDetect);
                    writer.WriteNamedValue("Density", (float)PhysicsDensity);
                    writer.WriteNamedValue("Friction", (float)PhysicsFriction);
                    writer.WriteNamedValue("Bounce", (float)PhysicsRestitution);
                    writer.WriteNamedValue("GravityModifier", (float)PhysicsGravityMultiplier);
                    writer.WriteNamedValue("AllowUnsit", AllowUnsit);
                    writer.WriteNamedValue("ScriptedSitOnly", IsScriptedSitOnly);
                    writer.WriteNamedValue("WalkableCoefficientAvatar", WalkableCoefficientAvatar);
                    writer.WriteNamedValue("WalkableCoefficientA", WalkableCoefficientA);
                    writer.WriteNamedValue("WalkableCoefficientB", WalkableCoefficientB);
                    writer.WriteNamedValue("WalkableCoefficientC", WalkableCoefficientC);
                    writer.WriteNamedValue("WalkableCoefficientD", WalkableCoefficientD);
                    if(VehicleType != VehicleType.None)
                    {
                        writer.WriteStartElement("Vehicle");
                        {
                            writer.WriteNamedValue("TYPE", (int)VehicleType);
                            writer.WriteNamedValue("FLAGS", (int)VehicleFlags);

                            /* linear */
                            writer.WriteNamedValue("LMDIR", VehicleParams[VehicleVectorParamId.LinearMotorDirection]);
                            writer.WriteNamedValue("LMFTIME", VehicleParams[VehicleVectorParamId.LinearFrictionTimescale]);
                            writer.WriteNamedValue("LMDTIME", VehicleParams[VehicleVectorParamId.LinearMotorDecayTimescale].Length);
                            writer.WriteNamedValue("LMTIME", VehicleParams[VehicleVectorParamId.LinearMotorTimescale].Length);
                            writer.WriteNamedValue("LMOFF", VehicleParams[VehicleVectorParamId.LinearMotorOffset]);

                            /* linear extension (must be written after float value due to loading concept) */
                            writer.WriteNamedValue("LinearMotorDecayTimescaleVector", VehicleParams[VehicleVectorParamId.LinearMotorDecayTimescale]);
                            writer.WriteNamedValue("LinearMotorTimescaleVector", VehicleParams[VehicleVectorParamId.LinearMotorTimescale]);

                            /* angular */
                            writer.WriteNamedValue("AMDIR", VehicleParams[VehicleVectorParamId.AngularMotorDirection]);
                            writer.WriteNamedValue("AMTIME", VehicleParams[VehicleVectorParamId.AngularMotorTimescale].Length);
                            writer.WriteNamedValue("AMDTIME", VehicleParams[VehicleVectorParamId.AngularMotorDecayTimescale].Length);
                            writer.WriteNamedValue("AMFTIME", VehicleParams[VehicleVectorParamId.AngularFrictionTimescale]);

                            /* angular extension (must be written after float value due to loading concept) */
                            writer.WriteNamedValue("AngularMotorTimescaleVector", VehicleParams[VehicleVectorParamId.AngularMotorTimescale]);
                            writer.WriteNamedValue("AngularMotorDecayTimescaleVector", VehicleParams[VehicleVectorParamId.AngularMotorDecayTimescale]);

                            /* deflection */
                            writer.WriteNamedValue("ADEFF", VehicleParams[VehicleFloatParamId.AngularDeflectionEfficiency]);
                            writer.WriteNamedValue("ADTIME", VehicleParams[VehicleFloatParamId.AngularDeflectionTimescale]);
                            writer.WriteNamedValue("LDEFF", VehicleParams[VehicleFloatParamId.LinearDeflectionEfficiency]);
                            writer.WriteNamedValue("LDTIME", VehicleParams[VehicleFloatParamId.LinearDeflectionTimescale]);

                            /* banking */
                            writer.WriteNamedValue("BEFF", VehicleParams[VehicleFloatParamId.BankingEfficiency]);
                            writer.WriteNamedValue("BMIX", VehicleParams[VehicleFloatParamId.BankingMix]);
                            writer.WriteNamedValue("BTIME", VehicleParams[VehicleFloatParamId.BankingTimescale]);
                            writer.WriteNamedValue("BankingAzimuth", VehicleParams[VehicleFloatParamId.BankingAzimuth]);
                            writer.WriteNamedValue("InvertedBankingModifier", VehicleParams[VehicleFloatParamId.InvertedBankingModifier]);

                            /* hover and buoyancy */
                            writer.WriteNamedValue("HHEI", VehicleParams[VehicleFloatParamId.HoverHeight]);
                            writer.WriteNamedValue("HEFF", VehicleParams[VehicleFloatParamId.HoverEfficiency]);
                            writer.WriteNamedValue("HTIME", VehicleParams[VehicleFloatParamId.HoverTimescale]);
                            writer.WriteNamedValue("VBUO", VehicleParams[VehicleFloatParamId.Buoyancy]);

                            /* attractor */
                            writer.WriteNamedValue("VAEFF", VehicleParams[VehicleFloatParamId.VerticalAttractionEfficiency]);
                            writer.WriteNamedValue("VATIME", VehicleParams[VehicleFloatParamId.VerticalAttractionTimescale]);

                            /* reference */
                            writer.WriteNamedValue("REF_FRAME", VehicleParams[VehicleRotationParamId.ReferenceFrame]);

                            /* wind */
                            writer.WriteNamedValue("LinearWindEfficiency", VehicleParams[VehicleVectorParamId.LinearWindEfficiency]);
                            writer.WriteNamedValue("AngularWindEfficiency", VehicleParams[VehicleVectorParamId.AngularWindEfficiency]);

                            /* mouselook */
                            writer.WriteNamedValue("MouselookAzimuth", VehicleParams[VehicleFloatParamId.MouselookAzimuth]);
                            writer.WriteNamedValue("MouselookAltitude", VehicleParams[VehicleFloatParamId.MouselookAltitude]);

                            /* disable motors */
                            writer.WriteNamedValue("DisableMotorsAbove", VehicleParams[VehicleFloatParamId.DisableMotorsAbove]);
                            writer.WriteNamedValue("DisableMotorsAfter", VehicleParams[VehicleFloatParamId.DisableMotorsAfter]);
                        }
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
            }
        }
        #endregion

        #region XML Deserialization
        private static void PayPriceFromXml(ObjectPart part, ObjectGroup rootGroup, XmlTextReader reader)
        {
            int paypriceidx = 0;
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.Name == "int")
                        {
                            int payprice = reader.ReadElementValueAsInt();
                            switch(paypriceidx++)
                            {
                                case 0:
                                    if(rootGroup != null)
                                    {
                                        rootGroup.PayPrice0 = payprice;
                                    }
                                    break;
                                case 1:
                                    if (rootGroup != null)
                                    {
                                        rootGroup.PayPrice1 = payprice;
                                    }
                                    break;
                                case 2:
                                    if (rootGroup != null)
                                    {
                                        rootGroup.PayPrice2 = payprice;
                                    }
                                    break;
                                case 3:
                                    if (rootGroup != null)
                                    {
                                        rootGroup.PayPrice3 = payprice;
                                    }
                                    break;
                                case 4:
                                    if (rootGroup != null)
                                    {
                                        rootGroup.PayPrice4 = payprice;
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            reader.ReadToEndElement();
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "PayPrice")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return;
                }
            }
            throw new InvalidObjectXmlException();
        }

        private static void ShapeFromXml(ObjectPart part, ObjectGroup rootGroup, XmlTextReader reader)
        {
            var shape = new PrimitiveShape();
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
                                shape.PathRadiusOffset = (sbyte)reader.ReadElementValueAsInt();
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
                                if(rootGroup != null)
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
                                    var p =  (byte)reader.ReadContentAsEnumValue<PrimitiveProfileShape>();
                                    shape.ProfileCurve = (byte)((shape.ProfileCurve & (byte)0xF0) | p);
                                }
                                break;

                            case "HollowShape":
                                {
                                    var p = (byte)reader.ReadContentAsEnumValue<PrimitiveProfileHollowShape>();
                                    shape.ProfileCurve = (byte)((shape.ProfileCurve & (byte)0x0F) | p);
                                }
                                break;

                            case "SculptTexture":
                                shape.SculptMap = reader.ReadContentAsUUID();
                                break;

                            case "FlexiSoftness":
                                {
                                    var flexparam = part.Flexible;
                                    flexparam.Softness = reader.ReadElementValueAsInt();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiTension":
                                {
                                    var flexparam = part.Flexible;
                                    flexparam.Tension = reader.ReadElementValueAsDouble();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiDrag":
                                {
                                    var flexparam = part.Flexible;
                                    flexparam.Friction = reader.ReadElementValueAsDouble();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiGravity":
                                {
                                    var flexparam = part.Flexible;
                                    flexparam.Gravity = reader.ReadElementValueAsDouble();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiWind":
                                {
                                    var flexparam = part.Flexible;
                                    flexparam.Wind = reader.ReadElementValueAsDouble();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiForceX":
                                {
                                    var flexparam = part.Flexible;
                                    var v = flexparam.Force;
                                    v.X = reader.ReadElementValueAsDouble();
                                    flexparam.Force = v;
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiForceY":
                                {
                                    var flexparam = part.Flexible;
                                    var v = flexparam.Force;
                                    v.Y = reader.ReadElementValueAsDouble();
                                    flexparam.Force = v;
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "FlexiForceZ":
                                {
                                    var flexparam = part.Flexible;
                                    var v = flexparam.Force;
                                    v.Z = reader.ReadElementValueAsDouble();
                                    flexparam.Force = v;
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "LightColorR":
                                {
                                    var lightparam = part.PointLight;
                                    var c = lightparam.LightColor;
                                    c.R = reader.ReadElementValueAsDouble().Clamp(0, 1);
                                    lightparam.LightColor = c;
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightColorG":
                                {
                                    var lightparam = part.PointLight;
                                    var c = lightparam.LightColor;
                                    c.G = reader.ReadElementValueAsDouble().Clamp(0, 1);
                                    lightparam.LightColor = c;
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightColorB":
                                {
                                    var lightparam = part.PointLight;
                                    var c = lightparam.LightColor;
                                    c.B = reader.ReadElementValueAsDouble().Clamp(0, 1);
                                    lightparam.LightColor = c;
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightRadius":
                                {
                                    var lightparam = part.PointLight;
                                    lightparam.Radius = reader.ReadElementValueAsDouble();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightCutoff":
                                {
                                    var lightparam = part.PointLight;
                                    lightparam.Cutoff = reader.ReadElementValueAsDouble();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightFalloff":
                                {
                                    var lightparam = part.PointLight;
                                    lightparam.Falloff = reader.ReadElementValueAsDouble();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "LightIntensity":
                                {
                                    var lightparam = part.PointLight;
                                    lightparam.Intensity = reader.ReadElementValueAsDouble();
                                    part.PointLight = lightparam;
                                }
                                break;

                            case "FlexiEntry":
                                {
                                    var flexparam = part.Flexible;
                                    flexparam.IsFlexible = reader.ReadElementValueAsBoolean();
                                    part.Flexible = flexparam;
                                }
                                break;

                            case "LightEntry":
                                {
                                    var lightparam = part.PointLight;
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

                            case "PhysicsShapeType":
                                switch(reader.ReadElementValueAsString())
                                {
                                    case "Prim":
                                        part.PhysicsShapeType = PrimitivePhysicsShapeType.Prim;
                                        break;

                                    case "None":
                                        part.PhysicsShapeType = PrimitivePhysicsShapeType.None;
                                        break;

                                    case "ConvexHull":
                                        part.PhysicsShapeType = PrimitivePhysicsShapeType.Convex;
                                        break;
                                }
                                break;

                            case "SculptType":
                                shape.SculptType = (PrimitiveSculptType)reader.ReadElementValueAsUInt();
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
                        if (!have_attachpoint && rootGroup != null)
                        {
                            rootGroup.AttachPoint = (AttachmentPoint)shape.State;
                        }
                        /* fixup wrong parameters */
                        if(shape.SculptMap == UUID.Zero && shape.SculptType != PrimitiveSculptType.None)
                        {
                            shape.SculptType = PrimitiveSculptType.None;
                        }
                        part.Shape = shape;
                        return;

                    default:
                        break;
                }
            }
        }

        private static Map DynAttrsFromXml(XmlTextReader reader)
        {
            if(reader.IsEmptyElement)
            {
                return new Map();
            }
            var damap = LlsdXml.Deserialize(reader) as Map;
            if (damap != null)
            {
                foreach (var key in damap.Keys)
                {
                    if (!(damap[key] is Map))
                    {
                        /* remove everything that is not a map */
                        damap.Remove(key);
                    }
                }
            }

            for(;;)
            {
                if(!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        reader.ReadToEndElement();
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "DynAttrs")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return damap;
                }
            }
        }

        private static PrimitiveFlags ReadPrimitiveFlagsEnum(XmlTextReader reader)
        {
            string value = reader.ReadElementValueAsString();
            if (value.Contains(" ") && !value.Contains(","))
            {
                value = value.Replace(" ", ", ");
            }
            string[] elems = value.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var flags = PrimitiveFlags.None;
            foreach(string elem in elems)
            {
                switch(elem)
                {
                    case "Physics":
                        flags |= PrimitiveFlags.Physics;
                        break;

                    case "Money":
                        flags |= PrimitiveFlags.TakesMoney;
                        break;

                    case "Phantom":
                        flags |= PrimitiveFlags.Phantom;
                        break;

                    case "InventoryEmpty":
                        flags |= PrimitiveFlags.InventoryEmpty;
                        break;

                    case "JointWheel": /* yes, its name is a messed up naming in OpenSim object xml */
                        flags |= PrimitiveFlags.IncludeInSearch;
                        break;

                    case "AllowInventoryDrop":
                        flags |= PrimitiveFlags.AllowInventoryDrop;
                        break;

                    case "CameraDecoupled":
                        flags |= PrimitiveFlags.CameraDecoupled;
                        break;

                    case "AnimSource":
                        flags |= PrimitiveFlags.AnimSource;
                        break;

                    case "CameraSource":
                        flags |= PrimitiveFlags.CameraSource;
                        break;

                    case "TemporaryOnRez":
                        flags |= PrimitiveFlags.TemporaryOnRez;
                        break;

                    case "Temporary":
                        flags |= PrimitiveFlags.Temporary;
                        break;
                }
            }

            return flags;
        }

        public static void LoadVehicleParams(XmlTextReader reader, ObjectPart part)
        {
            if (reader.IsEmptyElement)
            {
                return;
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
                        switch(reader.Name)
                        {
                            case "TYPE":
                                part.VehicleType = (VehicleType)reader.ReadElementValueAsInt();
                                break;

                            case "FLAGS":
                                part.VehicleFlags = (VehicleFlags)reader.ReadElementValueAsInt();
                                break;

                            case "LMDIR":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorDirection] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LMFTIME":
                                part.VehicleParams[VehicleVectorParamId.LinearFrictionTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LMDTIME":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorDecayTimescale] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "LMTIME":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorTimescale] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "LMOFF":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorOffset] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LinearMotorDecayTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorDecayTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "LinearMotorTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.LinearMotorTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AMDIR":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorDirection] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AMTIME":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorTimescale] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "AMDTIME":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorDecayTimescale] = new Vector3(reader.ReadElementValueAsDouble());
                                break;

                            case "AMFTIME":
                                part.VehicleParams[VehicleVectorParamId.AngularFrictionTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularMotorTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularMotorDecayTimescaleVector":
                                part.VehicleParams[VehicleVectorParamId.AngularMotorDecayTimescale] = reader.ReadElementChildsAsVector3();
                                break;

                            case "ADEFF":
                                part.VehicleParams[VehicleFloatParamId.AngularDeflectionEfficiency] = reader.ReadElementValueAsDouble();
                                break;

                            case "ADTIME":
                                part.VehicleParams[VehicleFloatParamId.AngularDeflectionTimescale] = reader.ReadElementValueAsDouble();
                                break;

                            case "LDEFF":
                                part.VehicleParams[VehicleFloatParamId.LinearDeflectionEfficiency] = reader.ReadElementValueAsDouble();
                                break;

                            case "LDTIME":
                                part.VehicleParams[VehicleFloatParamId.LinearDeflectionTimescale] = reader.ReadElementValueAsDouble();
                                break;

                            case "BEFF":
                                part.VehicleParams[VehicleFloatParamId.BankingEfficiency] = reader.ReadElementValueAsDouble();
                                break;

                            case "BMIX":
                                part.VehicleParams[VehicleFloatParamId.BankingMix] = reader.ReadElementValueAsDouble();
                                break;

                            case "BTIME":
                                part.VehicleParams[VehicleFloatParamId.BankingTimescale] = reader.ReadElementValueAsDouble();
                                break;

                            case "BankingAzimuth":
                                part.VehicleParams[VehicleFloatParamId.BankingAzimuth] = reader.ReadElementValueAsDouble();
                                break;

                            case "InvertedBankingModifier":
                                part.VehicleParams[VehicleFloatParamId.InvertedBankingModifier] = reader.ReadElementValueAsDouble();
                                break;

                            case "HHEI":
                                part.VehicleParams[VehicleFloatParamId.HoverHeight] = reader.ReadElementValueAsDouble();
                                break;

                            case "HEFF":
                                part.VehicleParams[VehicleFloatParamId.HoverEfficiency] = reader.ReadElementValueAsDouble();
                                break;

                            case "HTIME":
                                part.VehicleParams[VehicleFloatParamId.HoverTimescale] = reader.ReadElementValueAsDouble();
                                break;

                            case "VBUO":
                                part.VehicleParams[VehicleFloatParamId.Buoyancy] = reader.ReadElementValueAsDouble();
                                break;

                            case "VAEFF":
                                part.VehicleParams[VehicleFloatParamId.VerticalAttractionEfficiency] = reader.ReadElementValueAsDouble();
                                break;

                            case "VATIME":
                                part.VehicleParams[VehicleFloatParamId.VerticalAttractionTimescale] = reader.ReadElementValueAsDouble();
                                break;

                            case "REF_FRAME":
                                part.VehicleParams[VehicleRotationParamId.ReferenceFrame] = reader.ReadElementChildsAsQuaternion();
                                break;

                            case "LinearWindEfficiency":
                                part.VehicleParams[VehicleVectorParamId.LinearWindEfficiency] = reader.ReadElementChildsAsVector3();
                                break;

                            case "AngularWindEfficiency":
                                part.VehicleParams[VehicleVectorParamId.AngularWindEfficiency] = reader.ReadElementChildsAsVector3();
                                break;

                            case "MouselookAzimuth":
                                part.VehicleParams[VehicleFloatParamId.MouselookAzimuth] = reader.ReadElementValueAsDouble();
                                break;

                            case "MouselookAltitude":
                                part.VehicleParams[VehicleFloatParamId.MouselookAltitude] = reader.ReadElementValueAsDouble();
                                break;

                            case "DisableMotorsAbove":
                                part.VehicleParams[VehicleFloatParamId.DisableMotorsAbove] = reader.ReadElementValueAsDouble();
                                break;

                            case "DisableMotorsAfter":
                                part.VehicleParams[VehicleFloatParamId.DisableMotorsAfter] = reader.ReadElementValueAsDouble();
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "Vehicle")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return;
                }
            }
        }

        public static ObjectPart FromXml(XmlTextReader reader, ObjectGroup rootGroup, UUI currentOwner, XmlDeserializationOptions options)
        {
            var part = new ObjectPart
            {
                Owner = currentOwner
            };
            int InventorySerial = 1;
            bool IsPassCollisionsAlways = false;
            bool IsPassCollisions = true;
            bool IsPassTouches = false;
            bool IsPassTouchesAlways = true;
            bool IsVolumeDetect = false;
            bool isSitTargetActiveFound = false;

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
                            case "PayPrice":
                                PayPriceFromXml(part, rootGroup, reader);
                                break;

                            case "AllowedDrop":
                                part.IsAllowedDrop = reader.ReadElementValueAsBoolean();
                                break;

                            case "AllowUnsit":
                                part.AllowUnsit = reader.ReadElementValueAsBoolean();
                                break;

                            case "ScriptedSitOnly":
                                part.IsScriptedSitOnly = reader.ReadElementValueAsBoolean();
                                break;

                            case "ForceMouselook": /* boolean field */
                                part.ForceMouselook = reader.ReadElementValueAsBoolean();
                                break;

                            case "Vehicle":
                                LoadVehicleParams(reader, part);
                                break;

                            case "RezzerID":
                                if ((options & XmlDeserializationOptions.RestoreIDs) != 0)
                                {
                                    /* only makes sense to restore when keeping the other object ids same */
                                    part.ObjectGroup.RezzingObjectID = reader.ReadContentAsUUID();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "CreatorID":
                                {
                                    var creator = part.Creator;
                                    creator.ID = reader.ReadContentAsUUID();
                                    part.Creator = creator;
                                }
                                break;

                            case "CreatorData":
                                try
                                {
                                    var creator = part.Creator;
                                    creator.CreatorData = reader.ReadElementValueAsString();
                                    part.Creator = creator;
                                }
                                catch
                                {
                                    /* if it fails, we have to skip it and leave it partial */
                                }
                                break;

                            case "InventorySerial":
                                InventorySerial = reader.ReadElementValueAsInt();
                                break;

                            case "TaskInventory":
                                part.Inventory.FillFromXml(reader, currentOwner, options);
                                break;

                            case "UUID":
                                if ((options & XmlDeserializationOptions.RestoreIDs) != 0)
                                {
                                    part.ID = reader.ReadContentAsUUID();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "LocalId":
                                /* unnecessary to use the LocalId */
                                reader.ReadToEndElement();
                                break;

                            case "Name":
                                part.Name = reader.ReadElementValueAsString();
                                break;

                            case "Material":
                                part.Material = (PrimitiveMaterial)reader.ReadElementValueAsInt();
                                break;

                            case "VolumeDetectActive":
                                IsVolumeDetect = reader.ReadElementValueAsBoolean();
                                break;

                            case "Buoyancy":
                                part.Buoyancy = reader.ReadElementValueAsDouble();
                                break;

                            case "IsRotateXEnabled":
                                part.IsRotateXEnabled = reader.ReadElementValueAsBoolean();
                                break;

                            case "IsRotateYEnabled":
                                part.IsRotateYEnabled = reader.ReadElementValueAsBoolean();
                                break;

                            case "IsRotateZEnabled":
                                part.IsRotateZEnabled = reader.ReadElementValueAsBoolean();
                                break;

                            case "PassTouch":
                            case "PassTouches":
                                IsPassTouches = reader.ReadElementValueAsBoolean();
                                break;

                            case "PassTouchAlways":
                            case "PassTouchesAlways":
                                IsPassTouchesAlways = reader.ReadElementValueAsBoolean();
                                break;

                            case "PassCollisions":
                                IsPassCollisions = reader.ReadElementValueAsBoolean();
                                break;

                            case "PassCollisionsAlways":
                                IsPassCollisionsAlways = reader.ReadElementValueAsBoolean();
                                break;

                            case "ScriptAccessPin":
                                part.ScriptAccessPin = reader.ReadElementValueAsInt();
                                break;

                            case "GroupPosition":
                                /* needed in case of attachments */
                                if (rootGroup != null)
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
                                if (rootGroup == null)
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
                                    var tp = part.Text;
                                    tp.TextColor = reader.ReadElementChildsAsColorAlpha();
                                    part.Text = tp;
                                }
                                break;

                            case "Text":
                                {
                                    var tp = part.Text;
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

                            case "SitTargetActive":
                                part.IsSitTargetActive = reader.ReadElementValueAsBoolean();
                                isSitTargetActiveFound = true;
                                break;

                            case "SitTargetOrientation":
                                part.SitTargetOrientation = reader.ReadElementChildsAsQuaternion();
                                if(!part.SitTargetOrientation.ApproxEquals(Quaternion.Identity, double.Epsilon) && !isSitTargetActiveFound)
                                {
                                    part.IsSitTargetActive = true;
                                }
                                break;

                            case "SitTargetPosition":
                                part.SitTargetOffset = reader.ReadElementChildsAsVector3();
                                if(!part.SitTargetOffset.ApproxEquals(Vector3.Zero, double.Epsilon) && !isSitTargetActiveFound)
                                {
                                    part.IsSitTargetActive = true;
                                }
                                break;

                            case "ParentID":
                                reader.ReadToEndElement();
                                break;

                            case "CreationDate":
                                part.CreationDate = Date.UnixTimeToDateTime(reader.ReadElementValueAsULong());
                                break;

                            case "RezDate":
                                part.RezDate = Date.UnixTimeToDateTime(reader.ReadElementValueAsULong());
                                break;

                            case "Category":
                                if (rootGroup != null)
                                {
                                    rootGroup.Category = (UInt32)reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "SalePrice":
                                if(rootGroup != null)
                                {
                                    rootGroup.SalePrice = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "ObjectSaleType":
                                if (rootGroup != null)
                                {
                                    rootGroup.SaleType = (InventoryItem.SaleInfoData.SaleType)reader.ReadElementValueAsUInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "OwnershipCost":
                                if (rootGroup != null)
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

                            case "LastOwnerID":
                                if (rootGroup != null)
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
                                part.Flags = ReadPrimitiveFlagsEnum(reader);
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
                            case "SavedAttachmentPos":
                                if (rootGroup != null)
                                {
                                    rootGroup.AttachedPos = reader.ReadElementChildsAsVector3();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "SavedAttachmentPoint":
                                rootGroup.AttachPoint = (AttachmentPoint)reader.ReadElementValueAsInt();
                                break;

                            case "TextureAnimation":
                                part.TextureAnimationBytes = reader.ReadContentAsBase64();
                                break;

                            case "ParticleSystem":
                                part.ParticleSystemBytes = reader.ReadContentAsBase64();
                                break;

                            case "PayPrice0":
                                if (rootGroup != null)
                                {
                                    rootGroup.PayPrice0 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PayPrice1":
                                if (rootGroup != null)
                                {
                                    rootGroup.PayPrice1 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PayPrice2":
                                if (rootGroup != null)
                                {
                                    rootGroup.PayPrice2 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PayPrice3":
                                if (rootGroup != null)
                                {
                                    rootGroup.PayPrice3 = reader.ReadElementValueAsInt();
                                }
                                else
                                {
                                    reader.ReadToEndElement();
                                }
                                break;

                            case "PayPrice4":
                                if (rootGroup != null)
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
                                part.m_DynAttrMap = DynAttrsFromXml(reader);
                                break;

                            case "Components":
                                {
                                    string json = reader.ReadElementValueAsString();
                                    try
                                    {
                                        if (!string.IsNullOrEmpty(json))
                                        {
                                            using (var ms = new MemoryStream(json.ToUTF8Bytes()))
                                            {
                                                var m = Json.Deserialize(ms) as Map;
                                                if (m != null)
                                                {
                                                    if (m.ContainsKey("SavedAttachedPos") && m["SavedAttachedPos"] is AnArray && rootGroup != null)
                                                    {
                                                        var a = (AnArray)(m["SavedAttachedPos"]);
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
                                        /* do not pass exceptions to caller */
                                    }
                                }
                                break;

                            case "RegionHandle": /* <= why was this ever serialized, it breaks partly the deduplication attempt */
                            case "OwnerID": /* <= do not trust this thing ever! */
                            case "FolderID":
                            case "UpdateFlag":
                            case "SitTargetOrientationLL":
                            case "SitTargetPositionLL":
                                reader.ReadToEndElement();
                                break;

                            case "SoundID":
                            case "Sound":
                                {
                                    SoundParam sp = part.Sound;
                                    sp.SoundID = reader.ReadContentAsUUID();
                                    part.Sound = sp;
                                }
                                break;

                            case "SoundGain":
                                {
                                    SoundParam sp = part.Sound;
                                    sp.Gain = reader.ReadElementValueAsFloat();
                                    part.Sound = sp;
                                }
                                break;

                            case "SoundFlags":
                                {
                                    SoundParam sp = part.Sound;
                                    sp.Flags = (PrimitiveSoundFlags)reader.ReadElementValueAsInt();
                                    part.Sound = sp;
                                }
                                break;

                            case "SoundRadius":
                                {
                                    SoundParam sp = part.Sound;
                                    sp.Radius = reader.ReadElementValueAsFloat();
                                    part.Sound = sp;
                                }
                                break;

                            case "SoundQueueing":
                                part.IsSoundQueueing = reader.ReadElementValueAsBoolean();
                                break;

                            case "Force":
                                reader.ReadToEndElement();
                                break;

                            case "Torque":
                                reader.ReadToEndElement();
                                break;

                            case "WalkableCoefficientAvatar":
                                part.WalkableCoefficientAvatar = reader.ReadElementValueAsDouble();
                                break;

                            case "WalkableCoefficientA":
                                part.WalkableCoefficientA = reader.ReadElementValueAsDouble();
                                break;

                            case "WalkableCoefficientB":
                                part.WalkableCoefficientB = reader.ReadElementValueAsDouble();
                                break;

                            case "WalkableCoefficientC":
                                part.WalkableCoefficientC = reader.ReadElementValueAsDouble();
                                break;

                            case "WalkableCoefficientD":
                                part.WalkableCoefficientD = reader.ReadElementValueAsDouble();
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
                        if (rootGroup != null)
                        {
                            if((part.Flags & PrimitiveFlags.Temporary) != 0)
                            {
                                rootGroup.IsTemporary = true;
                            }
                            if ((part.Flags & PrimitiveFlags.TemporaryOnRez) != 0)
                            {
                                rootGroup.IsTempOnRez = true;
                            }
                        }

                        if ((part.Flags & PrimitiveFlags.Physics) != 0)
                        {
                            part.IsPhysics = true;
                        }

                        if ((part.Flags & PrimitiveFlags.VolumeDetect) != 0 || IsVolumeDetect)
                        {
                            part.IsVolumeDetect = true;
                        }

                        part.PassCollisionMode = !IsPassCollisions ?
                            PassEventMode.Never :
                            (IsPassCollisionsAlways ? PassEventMode.Always : PassEventMode.IfNotHandled);

                        part.PassTouchMode = !IsPassTouches ?
                            PassEventMode.Never :
                            (IsPassTouchesAlways ? PassEventMode.Always : PassEventMode.IfNotHandled);

                        if(part.Inventory.CountScripts == 0)
                        {
                            part.Flags &= ~(PrimitiveFlags.Touch | PrimitiveFlags.TakesMoney);
                        }

                        part.Flags &= ~(
                            PrimitiveFlags.InventoryEmpty | PrimitiveFlags.Physics | PrimitiveFlags.Temporary | PrimitiveFlags.TemporaryOnRez |
                            PrimitiveFlags.AllowInventoryDrop | PrimitiveFlags.Scripted | PrimitiveFlags.VolumeDetect |
                            PrimitiveFlags.ObjectGroupOwned | PrimitiveFlags.ObjectYouOwner | PrimitiveFlags.ObjectOwnerModify);
                        part.Inventory.InventorySerial = InventorySerial;
                        return part;

                    default:
                        break;
                }
            }
        }
        #endregion
    }
}
