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
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Object.Localization;
using SilverSim.Scene.Types.Object.Parameters;
using SilverSim.Scene.Types.Pathfinding;
using SilverSim.Scene.Types.Physics.Vehicle;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using SilverSim.Types.Parcel;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectPart : IPhysicalObject, ILocalIDAccessor, IKeyframedMotionObject
    {
        private static readonly ILog m_Log = LogManager.GetLogger("OBJECT PART");

        SceneInterface IKeyframedMotionObject.KeyframeScene => ObjectGroup?.Scene;

        private readonly object m_DataLock = new object();

        private int m_ObjectPartSerial;

        public int SerialNumber
        {
            get
            {
                int retVal = m_ObjectPartSerial;
                if (retVal == 0)
                {
                    /* ensure that we get a non-zero number */
                    retVal = Interlocked.CompareExchange(ref m_ObjectPartSerial, 1, 0);
                    if(retVal == 0)
                    {
                        retVal = 1;
                    }
                }
                return retVal;
            }
        }

        public void IncSerialNumber()
        {
            Interlocked.Increment(ref m_ObjectPartSerial);
        }

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
                    foreach(ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetLocalID(value);
                    }
                    UpdateInfo.LocalID = value;
                    m_LocalID = value;
                }
                UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags.All, incSerial);
            }
        }

        public ILocalIDAccessor LocalID => this;

        private ReferenceBoxed<UUID> m_ID = UUID.Zero;
        private ReferenceBoxed<Vector3> m_LocalPosition = Vector3.Zero;
        private ReferenceBoxed<Vector3> m_SandboxOrigin = Vector3.Zero;
        private ReferenceBoxed<Quaternion> m_LocalRotation = Quaternion.Identity;
        private PrimitivePhysicsShapeType m_PhysicsShapeType;
        private PrimitiveMaterial m_Material = PrimitiveMaterial.Wood;
        private ReferenceBoxed<Vector3> m_Size = new Vector3(0.5, 0.5, 0.5);
        private bool m_IsSitTargetActive;
        private ReferenceBoxed<Vector3> m_SitTargetOffset = Vector3.Zero;
        private string m_SitAnimation = string.Empty;
        private ReferenceBoxed<Quaternion> m_SitTargetOrientation = Quaternion.Identity;
        private bool m_IsAllowedDrop;
        private ClickActionType m_ClickAction;
        private PassEventMode m_PassCollisionMode;
        private PassEventMode m_PassTouchMode = PassEventMode.Always;
        private ReferenceBoxed<Vector3> m_AngularVelocity = Vector3.Zero;
        private ReferenceBoxed<Vector3> m_Velocity = Vector3.Zero;
        private UGUI m_Creator = UGUI.Unknown;
        private Date m_CreationDate = Date.Now;
        private Date m_RezDate = Date.Now;
        private PrimitiveFlags m_PrimitiveFlags;
        private Map m_DynAttrMap = new Map();
        public bool IsScripted { get; private set; }
        private bool m_AllowUnsit = true;
        private bool m_IsScriptedSitOnly;

        public Map DynAttrs => m_DynAttrMap;

        private readonly InventoryPermissionsData m_Permissions = new InventoryPermissionsData();

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

        private CharacterType m_PathfindingCharacterType;

        public CharacterType PathfindingCharacterType
        {
            get
            {
                return m_PathfindingCharacterType;
            }
            set
            {
                m_PathfindingCharacterType = value;
                TriggerOnUpdate(UpdateChangedFlags.Physics);
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
                return Interlocked.CompareExchange(ref m_WalkableCoefficientAvatar, 0, 0);
            }
            set
            {
                if (Atomic.TryChange(ref m_WalkableCoefficientAvatar, Math.Min(value, 0)))
                {
                    TriggerOnUpdate(UpdateChangedFlags.Physics);
                }
            }
        }

        public double WalkableCoefficientA
        {
            get
            {
                return Interlocked.CompareExchange(ref m_WalkableCoefficientA, 0, 0);
            }
            set
            {
                if (Atomic.TryChange(ref m_WalkableCoefficientA, Math.Min(value, 0)))
                {
                    TriggerOnUpdate(UpdateChangedFlags.Physics);
                }
            }
        }

        public double WalkableCoefficientB
        {
            get
            {
                return Interlocked.CompareExchange(ref m_WalkableCoefficientB, 0, 0);
            }
            set
            {
                if (Atomic.TryChange(ref m_WalkableCoefficientB, Math.Min(value, 0)))
                {
                    TriggerOnUpdate(UpdateChangedFlags.Physics);
                }
            }
        }

        public double WalkableCoefficientC
        {
            get
            {
                return Interlocked.CompareExchange(ref m_WalkableCoefficientC, 0, 0);
            }
            set
            {
                if (Atomic.TryChange(ref m_WalkableCoefficientC, Math.Min(value, 0)))
                {
                    TriggerOnUpdate(UpdateChangedFlags.Physics);
                }
            }
        }

        public double WalkableCoefficientD
        {
            get
            {
                return Interlocked.CompareExchange(ref m_WalkableCoefficientD, 0, 0);
            }
            set
            {
                if (Atomic.TryChange(ref m_WalkableCoefficientD, Math.Min(value, 0)))
                {
                    TriggerOnUpdate(UpdateChangedFlags.Physics);
                }
            }
        }

        private int m_PhysicsParameterUpdateSerial = 1;
        private int m_PhysicsShapeUpdateSerial = 1;

        public uint PhysicsParameterUpdateSerial
        {
            get
            {
                int retVal = m_PhysicsParameterUpdateSerial;
                if (retVal == 0)
                {
                    retVal = Interlocked.CompareExchange(ref m_PhysicsParameterUpdateSerial, 1, 0);
                    if (retVal == 0)
                    {
                        retVal = 1;
                    }
                }
                return (uint)retVal;
            }
        }

        public uint PhysicsShapeUpdateSerial
        {
            get
            {
                int retVal = m_PhysicsShapeUpdateSerial;
                if (retVal == 0)
                {
                    retVal = Interlocked.CompareExchange(ref m_PhysicsShapeUpdateSerial, 1, 0);
                    if (retVal == 0)
                    {
                        retVal = 1;
                    }
                }
                return (uint)retVal;
            }
        }

        private void IncrementPhysicsParameterUpdateSerial()
        {
            Interlocked.Increment(ref m_PhysicsParameterUpdateSerial);
        }

        private void IncrementPhysicsShapeUpdateSerial()
        {
            Interlocked.Increment(ref m_PhysicsShapeUpdateSerial);
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
            VehicleParams = new VehicleParams(this);
            m_DefaultLocalization = new ObjectPartLocalizedInfo(this);
            m_Permissions.Base = InventoryPermissionsMask.All;
            m_Permissions.Current = InventoryPermissionsMask.All;
            m_Permissions.Group = InventoryPermissionsMask.None;
            m_Permissions.EveryOne = InventoryPermissionsMask.None;
            m_Permissions.NextOwner = InventoryPermissionsMask.All;

            Inventory = new ObjectPartInventory();
            Inventory.OnChange += OnInventoryChange;
            Inventory.OnInventoryUpdate += OnInventoryUpdate;
            UpdateInfo = new ObjectUpdateInfo(this);
            AnimationController = new ObjectAnimationController(this);

            ID = UUID.Random;
        }

        public ObjectPart(UUID id)
        {
            VehicleParams = new VehicleParams(this);
            m_DefaultLocalization = new ObjectPartLocalizedInfo(this);
            m_Permissions.Base = InventoryPermissionsMask.All;
            m_Permissions.Current = InventoryPermissionsMask.All;
            m_Permissions.Group = InventoryPermissionsMask.None;
            m_Permissions.EveryOne = InventoryPermissionsMask.None;
            m_Permissions.NextOwner = InventoryPermissionsMask.All;

            Inventory = new ObjectPartInventory();
            Inventory.OnChange += OnInventoryChange;
            Inventory.OnInventoryUpdate += OnInventoryUpdate;
            UpdateInfo = new ObjectUpdateInfo(this);
            AnimationController = new ObjectAnimationController(this);

            ID = id;
        }

        public ObjectPart(UUID id, ObjectPart fromPart)
        {
            m_DefaultLocalization = new ObjectPartLocalizedInfo(this, fromPart.m_DefaultLocalization);
            foreach(ObjectPartLocalizedInfo l in fromPart.m_NamedLocalizations.Values)
            {
                m_NamedLocalizations.Add(l.LocalizationName, new ObjectPartLocalizedInfo(l.LocalizationName, this, l, m_DefaultLocalization));
            }

            Inventory = new ObjectPartInventory();
            Inventory.OnChange += OnInventoryChange;
            Inventory.OnInventoryUpdate += OnInventoryUpdate;
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
            Sound = fromPart.Sound;
            TextureAnimationBytes = fromPart.TextureAnimationBytes;
            TextureEntryBytes = fromPart.TextureEntryBytes;
            TouchText = fromPart.TouchText;
            VehicleType = fromPart.VehicleType;
            VehicleFlags = fromPart.VehicleFlags;
            VehicleParams = new VehicleParams(this, fromPart.VehicleParams);
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

        public bool CheckPermissions(UGUI accessor, UGI accessorgroup, InventoryPermissionsMask wanted) => (ObjectGroup.IsGroupOwned) ?
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
                lock (m_DataLock)
                {
                    int invSerial = Inventory.InventorySerial;
                    foreach (ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetInventorySerial(invSerial);
                    }
                }
                TriggerOnNextOwnerAssetIDChange();
            }
            else
            {
                lock (m_DataLock)
                {
                    int invSerial = Inventory.InventorySerial;
                    IsScripted = Inventory.CountScripts != 0;
                    foreach (ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetInventorySerial(invSerial);
                    }
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
            IsChanged = m_IsChangedEnabled;

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

            /* trigger cache rebuild */
            if((flags & (UpdateChangedFlags.Texture | UpdateChangedFlags.Shape)) != 0)
            {
                Interlocked.Increment(ref m_StaleCount);
            }

            UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags.All);
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

            UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags.All);
            ObjectGroup.Scene?.ScheduleUpdate(UpdateInfo);
        }

        private void TriggerOnPositionChange()
        {
            ObjectGroup grp = ObjectGroup;
            /* we have to check the ObjectGroup during setup process before using it here */
            if (grp == null)
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
            UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags.Full | ObjectPartLocalizedInfo.UpdateDataFlags.Terse);
            grp.Scene?.ScheduleUpdate(UpdateInfo);
            if(grp.IsAttached && grp.RootPart == this)
            {
                grp.AttachedPos = grp.Position;
                grp.AttachedRot = grp.Rotation;
            }
        }

        private void TriggerOnPhysicsPositionChange()
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
            UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags.Full | ObjectPartLocalizedInfo.UpdateDataFlags.Terse);
            ObjectGroup.Scene?.ScheduleUpdate(UpdateInfo, true);
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
                foreach (ObjectPartLocalizedInfo l in Localizations)
                {
                    l.SetBaseMask(value);
                }
            }

            TriggerOnUpdate(0);
        }

        public InventoryPermissionsMask BaseMask
        {
            get
            {
                return m_Permissions.Base;
            }
            set
            {
                lock (m_DataLock)
                {
                    m_Permissions.Base = value;
                    InventoryPermissionsMask ownerMask = value & InventoryPermissionsMask.ObjectPermissionsChangeable;

                    const InventoryPermissionsMask lockBits = InventoryPermissionsMask.Move | InventoryPermissionsMask.Modify;
                    m_Permissions.Current = (m_Permissions.Current & lockBits) | (ownerMask & ~lockBits);
                    foreach (ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetBaseMask(value);
                    }
                }

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
                foreach (ObjectPartLocalizedInfo l in Localizations)
                {
                    l.SetOwnerMask(value);
                }
            }
            TriggerOnUpdate(0);
        }

        public InventoryPermissionsMask OwnerMask
        {
            get
            {
                return m_Permissions.Current;
            }
            set
            {
                lock (m_DataLock)
                {
                    m_Permissions.Current = value;
                    foreach (ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetOwnerMask(value);
                    }
                }
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
                foreach (ObjectPartLocalizedInfo l in Localizations)
                {
                    l.SetGroupMask(value);
                }
            }
            TriggerOnUpdate(0);
        }

        private double m_Damage;
        public double Damage
        {
            get { return Interlocked.CompareExchange(ref m_Damage, 0, 0); }
            set
            {
                if(Atomic.TryChange(ref m_Damage, value.Clamp(0, 100)))
                {
                    TriggerOnUpdate(0);
                }
            }
        }

        public bool HasCausedDamage
        {
            get
            {
                return ObjectGroup.HasCausedDamage;
            }
            set
            {
                ObjectGroup.HasCausedDamage = value;
            }
        }

        public InventoryPermissionsMask GroupMask
        {
            get
            {
                return m_Permissions.Group;
            }
            set
            {
                lock (m_DataLock)
                {
                    m_Permissions.Group = value;
                    foreach(ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetGroupMask(value);
                    }
                }
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
                foreach(ObjectPartLocalizedInfo l in Localizations)
                {
                    l.SetEveryoneMask(value);
                }
            }
            TriggerOnUpdate(0);
        }

        public InventoryPermissionsMask EveryoneMask
        {
            get
            {
                return m_Permissions.EveryOne;
            }
            set
            {
                lock (m_DataLock)
                {
                    m_Permissions.EveryOne = value;
                    foreach (ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetEveryoneMask(value);
                    }
                }
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
                foreach(ObjectPartLocalizedInfo l in Localizations)
                {
                    l.SetNextOwnerMask(value);
                }
            }
            TriggerOnUpdate(0);
        }

        public bool IsScriptedSitOnly
        {
            get { return m_IsScriptedSitOnly; }

            set
            {
                m_IsScriptedSitOnly = value;
                TriggerOnUpdate(0);
            }
        }

        public bool AllowUnsit
        {
            get { return m_AllowUnsit; }

            set
            {
                m_AllowUnsit = value;
                TriggerOnUpdate(0);
            }
        }

        public InventoryPermissionsMask NextOwnerMask
        {
            get
            {
                return m_Permissions.NextOwner;
            }
            set
            {
                lock (m_DataLock)
                {
                    m_Permissions.NextOwner = value;
                    foreach (ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetNextOwnerMask(value);
                    }
                }
                TriggerOnUpdate(0);
            }
        }

        public Date RezDate
        {
            get
            {
                return new Date(m_RezDate);
            }
            set
            {
                m_RezDate = new Date(value);
                TriggerOnUpdate(0);
            }
        }

        public Date CreationDate
        {
            get
            {
                return new Date(m_CreationDate);
            }
            set
            {
                lock (m_DataLock)
                {
                    m_CreationDate = new Date(value);
                    foreach(ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetCreationDate(value);
                    }
                }
                TriggerOnUpdate(0);
            }
        }

        public PrimitiveFlags Flags
        {
            get
            {
                return m_PrimitiveFlags;
            }
            set
            {
                bool changed;
                lock(m_DataLock)
                {
                    changed = m_PrimitiveFlags != value;
                    m_PrimitiveFlags = value;
                }
                if (changed)
                {
                    TriggerOnUpdate(0);
                }
            }
        }

        public void SetClrFlagsMask(PrimitiveFlags setMask, PrimitiveFlags clrMask)
        {
            bool changed;
            lock (m_DataLock)
            {
                PrimitiveFlags old = m_PrimitiveFlags;
                m_PrimitiveFlags = (m_PrimitiveFlags  & ~clrMask) | setMask;
                changed = old != m_PrimitiveFlags;
            }
            if (changed)
            {
                TriggerOnUpdate(0);
            }
        }

        public UGUI Creator
        {
            get
            {
                    return new UGUI(m_Creator);
            }
            set
            {
                var uui = new UGUI(value);
                lock (m_DataLock)
                {
                    m_Creator = uui;
                    foreach (ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetCreator(uui);
                    }
                }
                TriggerOnUpdate(0);
            }
        }

        private ObjectGroup m_ObjectGroupStore;
        public ObjectGroup ObjectGroup
        {
            get
            {
                return m_ObjectGroupStore;
            }
            set
            {
                m_ObjectGroupStore = value;
            }
        }
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
                lock (m_DataLock)
                {
                    m_ClickAction = value;
                    foreach(ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetClickAction(value);
                    }
                }
                TriggerOnUpdate(0);
            }
        }

        public PassEventMode PassCollisionMode
        {
            get { return m_PassCollisionMode; }

            set
            {
                bool changed;
                lock (m_DataLock)
                {
                    changed = m_PassCollisionMode != value;
                    m_PassCollisionMode = value;
                }
                if (changed)
                {
                    IncrementPhysicsParameterUpdateSerial();
                    TriggerOnUpdate(0);
                }
            }
        }

        public PassEventMode PassTouchMode
        {
            get { return m_PassTouchMode; }
            set
            {
                bool changed;
                lock (m_DataLock)
                {
                    changed = m_PassTouchMode != value;
                    m_PassTouchMode = value;
                }
                if (changed)
                {
                    TriggerOnUpdate(0);
                }
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
                    foreach(ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetVelocity(value);
                    }
                }
                UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags.Full);
            }
        }

        public Vector3 AngularVelocity
        {
            get
            {
                return m_AngularVelocity;
            }
            set
            {
                bool changed = Atomic.TryChange(ref m_AngularVelocity, value);
                lock (m_DataLock)
                {
                    foreach(ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetAngularVelocity(value);
                    }
                }
                if (changed)
                {
                    TriggerOnUpdate(0);
                }
            }
        }

        public Vector3 Acceleration
        {
            get
            {
                return ObjectGroup?.Acceleration ?? Vector3.Zero;
            }
            set
            {
                ObjectGroup grp = ObjectGroup;
                if(grp != null)
                {
                    grp.Acceleration = value;
                }
            }
        }

        public Vector3 AngularAcceleration
        {
            get
            {
                return ObjectGroup?.AngularAcceleration ?? Vector3.Zero;
            }
            set
            {
                ObjectGroup grp = ObjectGroup;
                if (grp != null)
                {
                    grp.AngularAcceleration = value;
                }
            }
        }


        private bool m_IsSoundQueueing;

        public bool IsSoundQueueing
        {
            get { return m_IsSoundQueueing; }

            set
            {
                bool changed;
                lock (m_DataLock)
                {
                    changed = m_IsSoundQueueing != value;
                    m_IsSoundQueueing = value;
                }
                if (changed)
                {
                    TriggerOnUpdate(0);
                }
            }
        }

        public bool IsRootPart => ObjectGroup?.RootPart == this;

        public ObjectPart RootPart => ObjectGroup?.RootPart;

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
                bool changed;
                lock (m_DataLock)
                {
                    changed = m_IsAllowedDrop != value;
                    m_IsAllowedDrop = value;
                }
                if (changed)
                {
                    TriggerOnUpdate(UpdateChangedFlags.AllowedDrop);
                }
            }
        }

        public bool IsSitTargetActive
        {
            get
            {
                return m_IsSitTargetActive;
            }
            set
            {
                bool changed;
                lock (m_DataLock)
                {
                    changed = m_IsSitTargetActive != value;
                    m_IsSitTargetActive = value;
                }
                if (changed)
                {
                    TriggerOnUpdate(0);
                }
            }
        }

        public string SitAnimation
        {
            get
            {
                return m_SitAnimation;
            }
            set
            {
                string v = value ?? value;
                bool changed;
                lock (m_DataLock)
                {
                    changed = v != m_SitAnimation;
                    m_SitAnimation = v;
                }
                if (changed)
                {
                    TriggerOnUpdate(0);
                }
            }
        }

        private ReferenceBoxed<Vector3> m_UnSitTargetOffset = Vector3.Zero;
        private ReferenceBoxed<Quaternion> m_UnSitTargetOrientation = Quaternion.Identity;
        private bool m_IsUnSitTargetActive;

        public Vector3 UnSitTargetOffset
        {
            get
            {
                return m_UnSitTargetOffset;
            }
            set
            {
                if (Atomic.TryChange(ref m_UnSitTargetOffset, value))
                {
                    TriggerOnUpdate(0);
                }
            }
        }

        public Quaternion UnSitTargetOrientation
        {
            get
            {
                return m_UnSitTargetOrientation;
            }
            set
            {
                if (Atomic.TryChange(ref m_UnSitTargetOrientation, value))
                {
                    TriggerOnUpdate(0);
                }
            }
        }

        public bool IsUnSitTargetActive
        {
            get
            {
                return m_IsUnSitTargetActive;
            }
            set
            {
                bool changed;
                lock (m_DataLock)
                {
                    changed = m_IsUnSitTargetActive != value;
                    m_IsUnSitTargetActive = value;
                }
                if (changed)
                {
                    TriggerOnUpdate(0);
                }
            }
        }

        public Vector3 SitTargetOffset
        {
            get
            {
                return m_SitTargetOffset;
            }
            set
            {
                if (Atomic.TryChange(ref m_SitTargetOffset, value))
                {
                    TriggerOnUpdate(0);
                }
            }
        }

        public Quaternion SitTargetOrientation
        {
            get
            {
                return m_SitTargetOrientation;
            }
            set
            {
                if (Atomic.TryChange(ref m_SitTargetOrientation, value))
                {
                    TriggerOnUpdate(0);
                }
            }
        }

        public string SitText
        {
            get
            {
                return m_DefaultLocalization.SitText;
            }
            set
            {
                m_DefaultLocalization.SitText = value;
            }
        }

        public UGUI Owner
        {
            get
            {
                if (ObjectGroup != null)
                {
                    return ObjectGroup.Owner;
                }
                return UGUI.Unknown;
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
                return m_DefaultLocalization.TouchText;
            }
            set
            {
                m_DefaultLocalization.TouchText = value;
            }
        }

        public PrimitivePhysicsShapeType PhysicsShapeType
        {
            get { return m_PhysicsShapeType; }

            set
            {
                bool valueChanged;
                lock (m_DataLock)
                {
                    valueChanged = value != m_PhysicsShapeType;
                    m_PhysicsShapeType = value;
                }
                if (valueChanged)
                {
                    IncrementPhysicsShapeUpdateSerial();
                    IncrementPhysicsParameterUpdateSerial();
                    TriggerOnUpdate(UpdateChangedFlags.Shape);
                }
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
                lock (m_DataLock)
                {
                    foreach (ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetMaterial(value);
                    }
                }
                IncrementPhysicsParameterUpdateSerial();
                TriggerOnUpdate(0);
            }
        }

        public Vector3 Size
        {
            get
            {
                return m_Size;
            }
            set
            {
                bool valueChanged;
                lock(m_DataLock)
                {
                    valueChanged = m_Size != value;
                    m_Size = value;
                    foreach(ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetScale(value);
                    }
                }
                if (valueChanged)
                {
                    IncrementPhysicsParameterUpdateSerial();
                    TriggerOnUpdate(UpdateChangedFlags.Scale);
                }
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
            }
        }

        public UUID ID
        {
            get
            {
                return m_ID;
            }
            private set
            {
                lock(m_DataLock)
                {
                    m_ID = value;
                    UpdateInfo.ID = value;
                    Inventory.PartID = value;
                    foreach(ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetID(value);
                    }
                }
                UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags.All);
            }
        }

        public string Name
        {
            get { return m_DefaultLocalization.Name; }

            set
            {
                m_DefaultLocalization.Name = value;
            }
        }

        public string Description
        {
            get { return m_DefaultLocalization.Description; }

            set
            {
                m_DefaultLocalization.Description = value;
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
                return m_LocalPosition;
            }
            set
            {
                if (m_IsSandbox &&
                    ObjectGroup?.RootPart == this &&
                    HasHitSandboxLimit(value))
                {
                    goto hitsandboxlimit;
                }
                bool changed = IsPhysics;
                lock (m_DataLock)
                {
                    changed = changed || Atomic.TryChange(ref m_LocalPosition, value);
                    foreach(ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetPosition(value);
                    }
                }
                if (changed)
                {
                    TriggerOnUpdate(0);
                    TriggerOnPositionChange();
                }
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
                ObjectGroup grp = ObjectGroup;
                if(grp != null && grp.RootPart != this)
                {
                    return m_LocalPosition + grp.RootPart.GlobalPosition;
                }
                return m_LocalPosition;
            }
            set
            {
                ObjectGroup grp = ObjectGroup;
                if (grp != null)
                {
                    if (grp.RootPart != this)
                    {
                        value -= grp.RootPart.GlobalPosition;
                    }
                    else if (m_IsSandbox && (value - m_SandboxOrigin).Length > 10)
                    {
                        goto hitsandboxlimit;
                    }
                }
                bool changed = IsPhysics;
                lock (m_DataLock)
                {
                    changed = changed || Atomic.TryChange(ref m_LocalPosition, value);
                    foreach (ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetPosition(value);
                    }
                }

                if (changed)
                {
                    TriggerOnUpdate(0);
                    TriggerOnPositionChange();
                }
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
                return m_LocalPosition;
            }
            set
            {
                ObjectGroup grp = ObjectGroup;
                if (m_IsSandbox &&
                    grp?.RootPart != null &&
                    HasHitSandboxLimit(value))
                {
                    goto hitsandboxlimit;
                }
                bool changed = IsPhysics;
                lock (m_DataLock)
                {
                    changed = changed || Atomic.TryChange(ref m_LocalPosition, value);
                    foreach (ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetPosition(value);
                    }
                }

                if (changed)
                {
                    TriggerOnUpdate(0);
                    TriggerOnPositionChange();
                }
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
                return m_LocalRotation;
            }
            set
            {
                bool changed = IsPhysics;
                lock (m_DataLock)
                {
                    changed = changed || Atomic.TryChange(ref m_LocalRotation, value);
                    foreach(ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetRotation(value);
                    }
                }

                if (changed)
                {
                    TriggerOnUpdate(0);
                    TriggerOnPositionChange();
                }
            }
        }

        public Quaternion GlobalRotation
        {
            get
            {
                ObjectGroup grp = ObjectGroup;
                return (grp != null && this != grp.RootPart) ?
                    m_LocalRotation * ObjectGroup.RootPart.GlobalRotation :
                    (Quaternion)m_LocalRotation;
            }
            set
            {
                bool changed = IsPhysics;
                lock (m_DataLock)
                {
                    ObjectGroup grp = ObjectGroup;
                    if (grp != null && this != grp.RootPart)
                    {
                        value /= grp.RootPart.GlobalRotation;
                    }
                    changed = changed || Atomic.TryChange(ref m_LocalRotation, value);
                    foreach (ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetRotation(value);
                    }
                }

                if (changed)
                {
                    TriggerOnUpdate(0);
                    TriggerOnPositionChange();
                }
            }
        }

        public Quaternion LocalRotation
        {
            get
            {
                return m_LocalRotation;
            }
            set
            {
                bool changed = IsPhysics;
                lock (m_DataLock)
                {
                    changed = changed || Atomic.TryChange(ref m_LocalRotation, value);
                    foreach (ObjectPartLocalizedInfo l in Localizations)
                    {
                        l.SetRotation(value);
                    }
                }

                if (changed)
                {
                    TriggerOnUpdate(0);
                    TriggerOnPositionChange();
                }
            }
        }
        #endregion

        #region Link / Unlink
        protected internal void Link(ObjectGroup group)
        {
            if(null != Interlocked.CompareExchange(ref m_ObjectGroupStore, group, null))
            {
                throw new ArgumentException("ObjectGroup is already set");
            }
            UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags.All);
        }

        protected internal void Unlink()
        {
            ObjectGroup = null;
            UpdateData(ObjectPartLocalizedInfo.UpdateDataFlags.All);
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
                        paramList.Add(ObjectGroup.IsTemporary);
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
        private void PostCollisionEvent(CollisionEvent ev, bool filterExperience, UUID experienceID)
        {
            ObjectGroup grp = ObjectGroup;
            SceneInterface scene = grp?.Scene;

            if(grp.Damage > 0)
            {
                scene?.Remove(grp);
                return;
            }

            /* check if prim collides with vehicle having seated avatars on damage enabled areas */
            if(grp?.AgentSitting.Count != 0 && scene != null)
            {
                CollisionEvent nev = new CollisionEvent { Type = ev.Type };
                foreach(DetectInfo di in ev.Detected)
                {
                    bool causedDamage = false;
                    if (di.CausingDamage > 0)
                    {
                        foreach (IAgent agent in grp.m_SittingAgents.Keys1)
                        {
                            ParcelInfo pInfo;
                            if(scene.Parcels.TryGetValue(agent.GlobalPosition, out pInfo) && (pInfo.Flags & ParcelFlags.AllowDamage) != 0)
                            {
                                agent.DecreaseHealth(di.CausingDamage);
                                causedDamage = true;
                            }
                        }
                    }
                    if (!causedDamage)
                    {
                        nev.Detected.Add(di);
                    }
                }
                ev = nev;
                if(ev.Detected.Count == 0)
                {
                    return;
                }
            }

            bool isHandledEvent = false;
            foreach(ObjectPartInventoryItem item in Inventory.Values)
            {
                if (!(item.ScriptInstance?.HasCollisionEvent ?? false))
                {
                    continue;
                }
                isHandledEvent = true;
                ObjectPartInventoryItem.CollisionFilterParam filter = item.CollisionFilter;
                if(string.IsNullOrEmpty(filter.Name) && filter.ID == UUID.Zero && filter.Type == ObjectPartInventoryItem.CollisionFilterEnum.Accept)
                {
                    /* unfiltered so leave it unmodified */
                    if (!filterExperience || experienceID == item.ExperienceID)
                    {
                        item.ScriptInstance?.PostEvent(ev);
                    }
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

                    if(evnew.Detected.Count != 0 && (!filterExperience || experienceID == item.ExperienceID))
                    {
                        /* only post event if at least one passed the filter */
                        item.ScriptInstance?.PostEvent(evnew);
                    }
                }
            }

            ObjectPart rootPart = ObjectGroup?.RootPart;
            if(rootPart != this)
            {
                switch(PassCollisionMode)
                {
                    case PassEventMode.Never:
                        break;

                    case PassEventMode.IfNotHandled:
                        if(!isHandledEvent)
                        {
                            goto case PassEventMode.Always;
                        }
                        break;

                    case PassEventMode.Always:
                        rootPart?.PostCollisionEvent(ev, filterExperience, experienceID);
                        break;
                }
            }
        }

        public void PostEvent(IScriptEvent ev)
        {
            if (ev.GetType() == typeof(CollisionEvent))
            {
                PostCollisionEvent((CollisionEvent)ev, false, UUID.Zero);
            }
            else
            {
                foreach (ObjectPartInventoryItem item in Inventory.Values)
                {
                    item.ScriptInstance?.PostEvent(ev);
                }
            }
        }

        public void PostEvent(IScriptEvent ev, UUID experienceIDfilter)
        {
            if (ev.GetType() == typeof(CollisionEvent))
            {
                PostCollisionEvent((CollisionEvent)ev, true, experienceIDfilter);
            }
            else
            {
                foreach (ObjectPartInventoryItem item in Inventory.Values)
                {
                    if (item.ExperienceID == experienceIDfilter)
                    {
                        item.ScriptInstance?.PostEvent(ev);
                    }
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

        public TextReader OpenScriptInclude(string name)
        {
            ObjectPartInventoryItem item = Inventory[name];
            AssetData data;
            switch(item.AssetType)
            {
                case AssetType.Notecard:
                    if(ObjectGroup.Scene.AssetService.TryGetValue(item.AssetID, out data) && data.Type == item.AssetType)
                    {
                        var nc = new Notecard(data);
                        return new StreamReader(new MemoryStream(nc.Text.ToUTF8Bytes()));
                    }
                    break;

                case AssetType.LSLText:
                    if(ObjectGroup.Scene.AssetService.TryGetValue(item.AssetID, out data) && data.Type == item.AssetType)
                    {
                        return new StreamReader(data.InputStream);
                    }
                    break;
            }
            throw new KeyNotFoundException("Unsupported asset type for include");
        }

        #region Cache for Mesh and Texture asset ids
        private UUID[] m_TextureAssetIds = new UUID[0];
        private int m_StaleCount;
        private int m_StaleCountAck = -1;
        private readonly object m_CacheUpdateLock = new object();

        public UUID[] UsedMeshesAndTextures
        {
            get
            {
                int limitCount = 3;
                UUID[] returnArray = m_TextureAssetIds;
                int processingCount;
                /* occasionally this will do more work than actually needed but keeping the mutex time to a minimum keeps code from holding off each other */
                while ((processingCount = m_StaleCount) != m_StaleCountAck && limitCount-- > 0)
                {
                    var list = new Dictionary<UUID, bool>();
                    PrimitiveShape shape = Shape;
                    if (shape.Type == PrimitiveShapeType.Sculpt)
                    {
                        list[shape.SculptMap] = true;
                    }

                    foreach (ObjectPartLocalizedInfo localized in Localizations)
                    {
                        foreach (TextureEntryFace face in localized.GetFaces(ObjectPartLocalizedInfo.ALL_SIDES))
                        {
                            list[face.TextureID] = true;
                            UUID materialID = face.MaterialID;
                            if(materialID != UUID.Zero)
                            {
                                try
                                {
                                    Material mat = ObjectGroup?.Scene?.GetMaterial(materialID);
                                    if (mat != null)
                                    {
                                        UUID id = mat.SpecMap;
                                        if (id != UUID.Zero)
                                        {
                                            list[id] = true;
                                        }
                                        id = mat.NormMap;
                                        if (id != UUID.Zero)
                                        {
                                            list[id] = true;
                                        }
                                    }
                                }
                                catch
                                {
                                    /* intentionally ignored */
                                }
                            }
                        }
                    }

                    returnArray = list.Keys.ToArray();
                    lock(m_CacheUpdateLock)
                    {
                        if(processingCount == m_StaleCount)
                        {
                            m_TextureAssetIds = returnArray;
                            m_StaleCountAck = m_StaleCount;
                        }
                    }
                }
                return returnArray;
            }
        }
        #endregion
    }
}
