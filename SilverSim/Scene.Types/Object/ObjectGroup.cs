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
using SilverSim.Scene.Types.KeyframedMotion;
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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectGroup : RwLockedSortedDoubleDictionary<int, UUID, ObjectPart>, IObject, IKeyframedMotionObject
    {
        private static IScriptCompilerRegistry m_CompilerRegistry;
        public static IScriptCompilerRegistry CompilerRegistry
        {
            get { return m_CompilerRegistry; }

            set
            {
                if (m_CompilerRegistry == null)
                {
                    m_CompilerRegistry = value;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        SceneInterface IKeyframedMotionObject.KeyframeScene => Scene;

        private static readonly ILog m_Log = LogManager.GetLogger("OBJECT GROUP");

        #region Events
        public event Action<ObjectGroup, UpdateChangedFlags> OnUpdate;
        public event Action<IObject> OnPositionChange;
        #endregion

        public ILocalIDAccessor LocalID => RootPart.LocalID;

        public const int LINK_SET = -1;
        public const int LINK_ALL_OTHERS = -2;
        public const int LINK_ALL_CHILDREN = -3;
        public const int LINK_THIS = -4;
        public const int LINK_ROOT = 1;

        private bool m_IsTemporary;
        private bool m_IsGroupOwned;
        private bool m_IsIncludedInSearch;
        private AttachmentPoint m_AttachPoint;
        private ReferenceBoxed<Vector3> m_AttachedPos = Vector3.Zero;
        private ReferenceBoxed<Vector3> m_Velocity = Vector3.Zero;
        private UGI m_Group = UGI.Unknown;
        private UGUI m_Owner = UGUI.Unknown;
        private UGUI m_LastOwner = UGUI.Unknown;
        private ReferenceBoxed<UUID> m_OriginalAssetID = UUID.Zero; /* necessary for reducing asset re-generation */
        private ReferenceBoxed<UUID> m_NextOwnerAssetID = UUID.Zero; /* necessary for reducing asset re-generation */
        protected internal RwLockedBiDiMappingDictionary<IAgent, ObjectPart> m_SittingAgents = new RwLockedBiDiMappingDictionary<IAgent, ObjectPart>();
        public AgentSittingInterface AgentSitting { get; }
        public SceneInterface Scene { get; set; }

        private ReferenceBoxed<UUID> m_FromItemID = UUID.Zero;
        public UUID FromItemID /* used for attachments */
        {
            get
            {
                return m_FromItemID;
            }
            set
            {
                m_FromItemID = value;
            }
        }

        private ReferenceBoxed<UUID> m_RezzingObjectID = UUID.Zero;
        public UUID RezzingObjectID /* used alongside llRezObject and llRezAtRoot */
        {
            get
            {
                return m_RezzingObjectID;
            }
            set
            {
                m_RezzingObjectID = value;
            }
        }
        private BoundingBox? m_BoundingBox;

        private readonly object m_Lock = new object();

        private AssetServiceInterface m_AssetService;
        public AssetServiceInterface AssetService /* specific for attachments usage */
        {
            get { return m_AssetService ?? Scene.AssetService; }

            set
            {
                /* set is specific for attachments to reduce immediate rezzing load */
                m_AssetService = value;
            }
        }

        private Vector3 m_Acceleration = Vector3.Zero;
        private Vector3 m_AngularAcceleration = Vector3.Zero;
        public Vector3 CoalescedRestoreOffset = Vector3.Zero;

        private bool m_IsChangedEnabled;
        public bool IsChangedEnabled
        {
            get { return m_IsChangedEnabled; }

            set
            {
                m_IsChangedEnabled = m_IsChangedEnabled || value;
                foreach (ObjectPart p in Values)
                {
                    p.IsChangedEnabled = value;
                }
            }
        }

        #region Constructor
        public ObjectGroup()
        {
            AgentSitting = new AgentSittingInterface(this);
            IsChanged = false;
        }

        public ObjectGroup(ObjectGroup former)
        {
            AgentSitting = new AgentSittingInterface(this);

            m_IsGroupOwned = former.m_IsGroupOwned;
            m_IsIncludedInSearch = former.m_IsIncludedInSearch;
            m_Group = former.m_Group;
            m_Owner = former.m_Owner;
            m_LastOwner = former.m_LastOwner;
            IsChanged = false;
        }
        #endregion

        public void Add(int link, ObjectPart part) =>
            Add(link, part.ID, part);

        public bool IsMoving
        {
            get
            {
                return RootPart.IsMoving;
            }
            set
            {
                RootPart.IsMoving = value;
            }
        }

        public double Damage
        {
            get
            {
                return RootPart.Damage;
            }
            set
            {
                foreach (ObjectPart part in Values)
                {
                    if (part.Damage != value)
                    {
                        part.Damage = value;
                    }
                }
            }
        }

        public bool IsSandbox
        {
            get
            {
                return RootPart.IsSandbox;
            }
            set
            {
                lock (m_PhysicsLinksetUpdateLock)
                {
                    foreach (ObjectPart part in Values)
                    {
                        if (part.IsSandbox != value)
                        {
                            part.IsSandbox = value;
                        }
                    }
                }
            }
        }

        public bool IsBlockGrab
        {
            get
            {
                return RootPart.IsBlockGrab;
            }
            set
            {
                lock (m_PhysicsLinksetUpdateLock)
                {
                    foreach (ObjectPart part in Values)
                    {
                        if (part.IsBlockGrab != value)
                        {
                            part.IsBlockGrab = value;
                        }
                    }
                }
            }
        }

        public bool IsDieAtEdge
        {
            get
            {
                return RootPart.IsDieAtEdge;
            }
            set
            {
                lock (m_PhysicsLinksetUpdateLock)
                {
                    foreach (ObjectPart part in Values)
                    {
                        if (part.IsDieAtEdge != value)
                        {
                            part.IsDieAtEdge = value;
                        }
                    }
                }
            }
        }

        public bool IsReturnAtEdge
        {
            get
            {
                return RootPart.IsReturnAtEdge;
            }
            set
            {
                lock (m_PhysicsLinksetUpdateLock)
                {
                    foreach (ObjectPart part in Values)
                    {
                        if (part.IsReturnAtEdge != value)
                        {
                            part.IsReturnAtEdge = value;
                        }
                    }
                }
            }
        }

        public bool IsBlockGrabObject
        {
            get
            {
                return RootPart.IsBlockGrabObject;
            }
            set
            {
                lock (m_PhysicsLinksetUpdateLock)
                {
                    foreach (ObjectPart part in Values)
                    {
                        if (part.IsBlockGrabObject != value)
                        {
                            part.IsBlockGrabObject = value;
                        }
                    }
                }
            }
        }

        /* UUID references to PartID and Vector3 is the attaching force */
        public RwLockedDictionary<UUID, Vector3> AttachedForces = new RwLockedDictionary<UUID, Vector3>();

        public UUID OriginalAssetID /* will be set to UUID.Zero when anything has been changed */
        {
            get
            {
                return m_OriginalAssetID;
            }
            set
            {
                m_OriginalAssetID = value;
                if (value != UUID.Zero)
                {
                    TriggerOnAssetIDChange();
                }
            }
        }

        public UUID NextOwnerAssetID /* will be set to UUID.Zero when anything has been changed */
        {
            get
            {
                return m_NextOwnerAssetID;
            }
            set
            {
                m_NextOwnerAssetID = value;
                if (value != UUID.Zero)
                {
                    TriggerOnAssetIDChange();
                }
            }
        }

        public DetectedTypeFlags DetectedType => RootPart.DetectedType;

        public double PhysicsGravityMultiplier
        {
            get { return RootPart.PhysicsGravityMultiplier; }

            set { RootPart.PhysicsGravityMultiplier = value; }
        }

        internal void TriggerOnAssetIDChange()
        {
            foreach (Action<ObjectGroup, UpdateChangedFlags> del in OnUpdate?.GetInvocationList().OfType<Action<ObjectGroup, UpdateChangedFlags>>() ?? new Action<ObjectGroup, UpdateChangedFlags>[0])
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

            RootPart.TriggerOnNextOwnerAssetIDChange();
        }

        private void TriggerOnUpdate(UpdateChangedFlags flags)
        {
            if (Count == 0)
            {
                return;
            }
            lock (m_Lock)
            {
                OriginalAssetID = UUID.Zero;
                NextOwnerAssetID = UUID.Zero;
                m_BoundingBox = null;
            }

            foreach (Action<ObjectGroup, UpdateChangedFlags> del in OnUpdate?.GetInvocationList().OfType<Action<ObjectGroup, UpdateChangedFlags>>() ?? new Action<ObjectGroup, UpdateChangedFlags>[0])
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

            RootPart.TriggerOnUpdate(flags);
        }

        #region KeyframedMotion
        private KeyframedMotionController m_KeyframedMotion;
        private readonly object m_KeyframeMotionUpdateLock = new object();

        public KeyframedMotion.KeyframedMotion KeyframedMotion
        {
            get
            {
                KeyframedMotionController controller = m_KeyframedMotion;
                if(controller != null)
                {
                    return controller.Program;
                }
                return null;
            }

            set
            {
                lock(m_KeyframeMotionUpdateLock)
                {
                    if (m_KeyframedMotion != null)
                    {
                        m_KeyframedMotion.Stop();
                        m_KeyframedMotion.Dispose();
                    }
                    m_KeyframedMotion = null;
                }
                if (value != null)
                {
                    lock(m_KeyframeMotionUpdateLock)
                    {
                        if(m_KeyframedMotion == null)
                        {
                            m_KeyframedMotion = new KeyframedMotionController(this)
                            {
                                Program = value
                            };
                        }
                        else
                        {
                            m_KeyframedMotion.Program = value;
                        }
                    }
                }
            }
        }

        public void PlayKeyframedMotion()
        {
            KeyframedMotionController controller = m_KeyframedMotion;
            controller?.Play();
        }

        public void PauseKeyframedMotion()
        {
            KeyframedMotionController controller = m_KeyframedMotion;
            controller?.Pause();
        }

        public void StopKeyframedMotion()
        {
            KeyframedMotionController controller = m_KeyframedMotion;
            controller?.Stop();
        }

        #endregion

        #region Properties
        public bool IsChanged { get; private set; }

        public InventoryItem.SaleInfoData.SaleType m_SaleType;
        public int m_SalePrice;
        public int m_OwnershipCost;
        public UInt32 m_Category;
        public int m_PayPrice0 = -1;
        public int m_PayPrice1 = -1;
        public int m_PayPrice2 = -1;
        public int m_PayPrice3 = -1;
        public int m_PayPrice4 = -1;

        public bool IsIncludedInSearch
        {
            get { return m_IsIncludedInSearch; }

            set
            {
                m_IsIncludedInSearch = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int PayPrice0
        {
            get { return m_PayPrice0; }

            set
            {
                m_PayPrice0 = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int PayPrice1
        {
            get { return m_PayPrice1; }

            set
            {
                m_PayPrice1 = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int PayPrice2
        {
            get { return m_PayPrice2; }

            set
            {
                m_PayPrice2 = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int PayPrice3
        {
            get { return m_PayPrice3; }

            set
            {
                m_PayPrice3 = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int PayPrice4
        {
            get { return m_PayPrice4; }

            set
            {
                m_PayPrice4 = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public InventoryItem.SaleInfoData.SaleType SaleType
        {
            get { return m_SaleType; }

            set
            {
                m_SaleType = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int SalePrice
        {
            get { return m_SalePrice; }

            set
            {
                m_SalePrice = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int OwnershipCost
        {
            get { return m_OwnershipCost; }
            set
            {
                m_OwnershipCost = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public UInt32 Category
        {
            get { return m_Category; }

            set
            {
                m_Category = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public AttachmentPoint AttachPoint
        {
            get { return m_AttachPoint; }

            set
            {
                m_AttachPoint = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        private bool m_IsAttached;

        public bool IsAttached
        {
            get { return m_IsAttached; }

            set
            {
                m_IsAttached = value;
                TriggerOnUpdate(0);
            }
        }

        public bool IsAttachedToPrivate
        {
            get
            {
                if(!IsAttached)
                {
                    return false;
                }
                switch(AttachPoint)
                {
                    case AttachmentPoint.HudBottom:
                    case AttachmentPoint.HudBottomLeft:
                    case AttachmentPoint.HudBottomRight:
                    case AttachmentPoint.HudCenter1:
                    case AttachmentPoint.HudCenter2:
                    case AttachmentPoint.HudTopCenter:
                    case AttachmentPoint.HudTopLeft:
                    case AttachmentPoint.HudTopRight:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public bool IsTempAttached
        {
            get { return IsAttached && FromItemID == UUID.Zero; }
        }

        public Vector3 Acceleration
        {
            get { return m_Acceleration; }

            set
            {
                m_Acceleration = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Vector3 AttachedPos
        {
            get { return m_AttachedPos; }

            set
            {
                m_AttachedPos = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Vector3 AngularAcceleration
        {
            get { return m_AngularAcceleration; }

            set
            {
                m_AngularAcceleration = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public bool IsTemporary
        {
            get { return m_IsTemporary; }

            set
            {
                lock (m_Lock)
                {
                    if(m_IsTemporary == value)
                    {
                        return;
                    }
                    m_IsTemporary = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Vector3 Size
        {
            get { return RootPart.Size; }

            set { RootPart.Size = value; }
        }

        public bool IsGroupOwned
        {
            get
            {
                return m_IsGroupOwned;
            }
            set
            {
                bool changed = false;
                lock (m_Lock)
                {
                    changed = m_IsGroupOwned != value;
                    m_IsGroupOwned = value;
                }
                if (changed)
                {
                    IsChanged = m_IsChangedEnabled;
                    TriggerOnUpdate(UpdateChangedFlags.Owner);
                }
            }
        }

        public UGI Group
        {
            get
            {
                return new UGI(m_Group);
            }
            set
            {
                m_Group = new UGI(value);
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public UGUI LastOwner
        {
            get
            {
                return new UGUI(m_LastOwner);
            }
            set
            {
                m_LastOwner = new UGUI(value);
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public UGUI Owner
        {
            get
            {
                return new UGUI(m_Owner);
            }
            set
            {
                m_Owner = new UGUI(value);
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Owner);
            }
        }

        public ObjectPart RootPart => this[LINK_ROOT];

        public Vector3 Velocity
        {
            get
            {
                return m_Velocity;
            }
            set
            {
                m_Velocity = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Vector3 AngularVelocity
        {
            get { return RootPart.AngularVelocity; }

            set { RootPart.AngularVelocity = value; }
        }

        public Quaternion Rotation
        {
            get { return RootPart.Rotation; }
            set { RootPart.Rotation = value; }
        }

        public Quaternion GlobalRotation
        {
            get { return RootPart.GlobalRotation; }
            set { RootPart.GlobalRotation = value; }
        }

        public Quaternion LocalRotation
        {
            get { return RootPart.LocalRotation; }
            set { RootPart.LocalRotation = value; }
        }

        public UUID ID
        {
            get { return RootPart.ID; }
        }

        public string Name
        {
            get { return RootPart.Name; }
            set { RootPart.Name = value; }
        }

        public string Description
        {
            get { return RootPart.Description; }
            set { RootPart.Description = value; }
        }

        public Vector3 Position
        {
            get { return RootPart.Position; }
            set { RootPart.Position = value; }
        }

        public Vector3 GlobalPosition
        {
            get { return RootPart.GlobalPosition; }
            set { RootPart.GlobalPosition = value; }
        }

        public Vector3 LocalPosition
        {
            get { return RootPart.LocalPosition; }
            set { RootPart.LocalPosition = value; }
        }

        public IObject GetObjectLink(int linkTarget)
        {
            int PrimCount = Count;
            if(PrimCount < linkTarget)
            {
                linkTarget -= PrimCount + 1;
                List<IAgent> agents = m_SittingAgents.Keys1;
                if(linkTarget >= agents.Count)
                {
                    throw new KeyNotFoundException();
                }
                return agents[linkTarget];
            }
            else
            {
                return this[linkTarget];
            }
        }
        #endregion

        public bool IsInScene(SceneInterface scene) => true;

        public void InvokeOnPositionUpdate(IObject obj)
        {
            if(obj != RootPart)
            {
                return;
            }

            OnPositionChange?.Invoke(this);
        }

        #region Primitive Params Methods
        public void GetPrimitiveParams(int linkThis, int linkTarget, AnArray.Enumerator enumerator, AnArray paramList) =>
            GetPrimitiveParams(linkThis, linkTarget, enumerator, paramList, null);

        public void GetPrimitiveParams(int linkThis, int linkTarget, AnArray.Enumerator enumerator, AnArray paramList, CultureInfo initialCulture)
        {
            CultureInfo currentCulture = initialCulture;
            if(0 == linkTarget)
            {
                linkTarget = LINK_ROOT;
            }
            else if(LINK_THIS == linkTarget)
            {
                linkTarget = linkThis;
            }
            else if (linkTarget < LINK_ROOT)
            {
                throw new LocalizedScriptErrorException(this, "InvalidLinkTargetParameterForFunction0Msg1", "Invalid link target parameter for {0}: {1}", "GetPrimitiveParams", linkTarget);
            }

            while (enumerator.MoveNext())
            {
                switch(ParamsHelper.GetPrimParamType(enumerator))
                {
                    case PrimitiveParamsType.Language:
                        {
                            string cultureName = ParamsHelper.GetString(enumerator, "PRIM_LANGUAGE");
                            if (string.IsNullOrEmpty(cultureName))
                            {
                                currentCulture = null;
                            }
                            else
                            {
                                try
                                {
                                    currentCulture = new CultureInfo(cultureName);
                                }
                                catch
                                {
                                    throw new LocalizedScriptErrorException(this, "InvalidLanguageParameter0", "Invalid language parameter '{0}'", cultureName);
                                }
                            }
                        }
                        break;

                    case PrimitiveParamsType.LinkTarget:
                        linkTarget = ParamsHelper.GetInteger(enumerator, "PRIM_LINK_TARGET");
                        if(0 == linkTarget)
                        {
                            linkTarget = LINK_ROOT;
                        }
                        else if(LINK_THIS == linkTarget)
                        {
                            linkTarget = linkThis;
                        }
                        break;

                    case PrimitiveParamsType.Physics:
                        paramList.Add(IsPhysics);
                        break;

                    case PrimitiveParamsType.TempOnRez:
                        paramList.Add(IsTemporary);
                        break;

                    case PrimitiveParamsType.Phantom:
                        paramList.Add(IsPhantom);
                        break;

                    default:
                        switch (linkTarget)
                        {
                            case LINK_SET:
                                throw new LocalizedScriptErrorException(this, "LinkTarget0NotAllowedFor1", "Link Target {0} not allowed for {1}", "LINK_SET", "GetPrimitiveParams");

                            case LINK_ALL_OTHERS:
                                throw new LocalizedScriptErrorException(this, "LinkTarget0NotAllowedFor1", "Link Target {0} not allowed for {1}", "LINK_ALL_OTHERS", "GetPrimitiveParams");

                            case LINK_ALL_CHILDREN:
                                throw new LocalizedScriptErrorException(this, "LinkTarget0NotAllowedFor1", "Link Target {0} not allowed for {1}", "LINK_ALL_CHILDREN", "GetPrimitiveParams");

                            default:
                                if(linkTarget < 1)
                                {
                                    throw new LocalizedScriptErrorException(this, "InvalidLinkTargetParameterForFunction0Msg1", "Invalid link target parameter for {0}: {1}", linkTarget, "GetPrimitiveParams");
                                }
                                IObject obj;
                                try
                                {
                                    obj = GetObjectLink(linkTarget);
                                }
                                catch(KeyNotFoundException)
                                {
                                    throw new LocalizedScriptErrorException(this, "LinkTarget0DoesNotExist", "Link target {0} does not exist", linkTarget);
                                }
                                obj.GetPrimitiveParams(enumerator, paramList, currentCulture);
                                break;
                        }
                        break;
                }
            }
        }

        public void GetPrimitiveParams(AnArray.Enumerator enumerator, AnArray paramList)
        {
            GetPrimitiveParams(LINK_ROOT, LINK_ROOT, enumerator, paramList, null);
        }

        public void GetPrimitiveParams(AnArray.Enumerator enumerator, AnArray paramList, CultureInfo currentCulture)
        {
            GetPrimitiveParams(LINK_ROOT, LINK_ROOT, enumerator, paramList, currentCulture);
        }

        public void SetPrimitiveParams(int linkThis, int linkTarget, AnArray.MarkEnumerator enumerator)
        {
            if (0 == linkTarget)
            {
                linkTarget = LINK_ROOT;
            }
            else if(linkTarget == LINK_THIS)
            {
                linkTarget = linkThis;
            }
            string cultureName = null;
            bool haveAtLeastOne = false;

            while (enumerator.MoveNext())
            {
                switch (ParamsHelper.GetPrimParamType(enumerator))
                {
                    case PrimitiveParamsType.Language:
                        {
                            cultureName = ParamsHelper.GetString(enumerator, "PRIM_LANGUAGE");
                            if (string.IsNullOrEmpty(cultureName))
                            {
                                cultureName = null;
                            }
                            else
                            {
                                try
                                {
                                    CultureInfo.CreateSpecificCulture(cultureName);
                                }
                                catch
                                {
                                    throw new LocalizedScriptErrorException(this, "InvalidLanguageParameter0", "Invalid language parameter '{0}'", cultureName);
                                }
                            }
                        }
                        break;

                    case PrimitiveParamsType.LinkTarget:
                        linkTarget = ParamsHelper.GetInteger(enumerator, "PRIM_LINK_TARGET");
                        if (0 == linkTarget)
                        {
                            linkTarget = LINK_ROOT;
                        }
                        else if(LINK_THIS == linkTarget)
                        {
                            linkTarget = linkThis;
                        }
                        break;

                    case PrimitiveParamsType.Physics:
                        IsPhysics = ParamsHelper.GetBoolean(enumerator, "PRIM_PHYSICS");
                        break;

                    case PrimitiveParamsType.TempOnRez:
                        IsTemporary = ParamsHelper.GetBoolean(enumerator, "PRIM_TEMP_ON_REZ");
                        break;

                    case PrimitiveParamsType.Phantom:
                        IsPhantom = ParamsHelper.GetBoolean(enumerator, "PRIM_PHANTOM");
                        break;

                    default:
                        switch (linkTarget)
                        {
                            case LINK_SET:
                                enumerator.MarkPosition();
                                foreach(ObjectPart obj in this.ValuesByKey1)
                                {
                                    enumerator.GoToMarkPosition();
                                    obj.SetPrimitiveParams(enumerator, cultureName);
                                }
                                foreach(IAgent agent in m_SittingAgents.Keys1)
                                {
                                    enumerator.GoToMarkPosition();
                                    agent.SetPrimitiveParams(enumerator);
                                }
                                break;

                            case LINK_ALL_CHILDREN:
                                enumerator.MarkPosition();
                                foreach(KeyValuePair<int, ObjectPart> kvp in Key1ValuePairs)
                                {
                                    if (kvp.Key != LINK_ROOT)
                                    {
                                        enumerator.GoToMarkPosition();
                                        kvp.Value.SetPrimitiveParams(enumerator, cultureName);
                                        haveAtLeastOne = true;
                                    }
                                }
                                foreach(IAgent agent in m_SittingAgents.Keys1)
                                {
                                    enumerator.GoToMarkPosition();
                                    agent.SetPrimitiveParams(enumerator);
                                    haveAtLeastOne = true;
                                }
                                if (!haveAtLeastOne)
                                {
                                    enumerator.GoToMarkPosition();
                                    SkipParameterBlock(enumerator);
                                }
                                break;

                            case LINK_ALL_OTHERS:
                                enumerator.MarkPosition();
                                foreach(KeyValuePair<int, ObjectPart> kvp in Key1ValuePairs)
                                {
                                    if (kvp.Key != linkThis)
                                    {
                                        enumerator.GoToMarkPosition();
                                        kvp.Value.SetPrimitiveParams(enumerator, cultureName);
                                        haveAtLeastOne = true;
                                    }
                                }
                                foreach(IAgent agent in m_SittingAgents.Keys1)
                                {
                                    enumerator.GoToMarkPosition();
                                    agent.SetPrimitiveParams(enumerator);
                                    haveAtLeastOne = true;
                                }
                                if(!haveAtLeastOne)
                                {
                                    enumerator.GoToMarkPosition();
                                    SkipParameterBlock(enumerator);
                                }
                                break;

                            default:
                                if (linkTarget < 1)
                                {
                                    throw new LocalizedScriptErrorException(this, "InvalidLinkTargetParameterForFunction0Msg1", "Invalid link target parameter for {0}: {1}", linkTarget, "SetPrimitiveParams");
                                }
                                IObject linkobj;
                                try
                                {
                                    linkobj = GetObjectLink(linkTarget);
                                }
                                catch(KeyNotFoundException)
                                {
                                    throw new LocalizedScriptErrorException(this, "LinkTarget0DoesNotExist", "Link target {0} does not exist", linkTarget);
                                }
                                linkobj.SetPrimitiveParams(enumerator);
                                break;
                        }
                        break;
                }
            }
        }

        private void SkipParameterBlock(AnArray.MarkEnumerator enumerator)
        {
            int paramcount = 0;
            switch(ParamsHelper.GetPrimParamType(enumerator))
            {
                case PrimitiveParamsType.Type:
                    /* skip prim type block */
                    ObjectPart.PrimitiveShape.FromPrimitiveParams(enumerator);
                    return;

                case PrimitiveParamsType.FullBright:
                case PrimitiveParamsType.TexGen:
                case PrimitiveParamsType.Glow:
                    paramcount = 2;
                    break;


                case PrimitiveParamsType.Text:
                case PrimitiveParamsType.Color:
                case PrimitiveParamsType.BumpShiny:
                case PrimitiveParamsType.Omega:
                case PrimitiveParamsType.AlphaMode:
                case PrimitiveParamsType.SitTarget:
                case PrimitiveParamsType.UnSitTarget:
                    paramcount = 3;
                    break;


                case PrimitiveParamsType.Texture:
                case PrimitiveParamsType.PointLight:
                case PrimitiveParamsType.Normal:
                case PrimitiveParamsType.Projector:
                    paramcount = 5;
                    break;

                case PrimitiveParamsType.Flexible:
                    paramcount = 7;
                    break;

                case PrimitiveParamsType.Specular:
                    paramcount = 8;
                    break;

                default:
                    paramcount = 1;
                    break;
            }

            while(paramcount-->0)
            {
                if(!enumerator.MoveNext())
                {
                    break;
                }
            }
        }

        public void SetPrimitiveParams(AnArray.MarkEnumerator enumerator)
        {
            SetPrimitiveParams(LINK_ROOT, LINK_ROOT, enumerator);
        }
        #endregion

        #region Object Details Methods
        public void GetObjectDetails(AnArray.Enumerator enumerator, AnArray paramList)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region Agent Sitting
        public class AgentSittingInterface
        {
            private readonly ObjectGroup m_Group;
            private readonly object m_SitLock = new object();

            public AgentSittingInterface(ObjectGroup group)
            {
                m_Group = group;
            }

            public IAgent this[ObjectPart p] => m_Group.m_SittingAgents[p];

            public bool TryGetValue(ObjectPart p, out IAgent agent) => m_Group.m_SittingAgents.TryGetValue(p, out agent);

            public bool TryGetValue(IAgent agent, out ObjectPart part) => m_Group.m_SittingAgents.TryGetValue(agent, out part);

            public bool TryGetValue(UUID agentid, out IAgent agent)
            {
                foreach(var ag in m_Group.m_SittingAgents.Keys1)
                {
                    if(ag.ID == agentid)
                    {
                        agent = ag;
                        return true;
                    }
                }
                agent = null;
                return false;
            }

            public int Count => m_Group.m_SittingAgents.Count;

            private static readonly Vector3 SIT_TARGET_OFFSET = new Vector3(0, 0, 0.4);

            public bool Sit(IAgent agent, int preferedLinkNumber = -1, bool forceScriptedSitOnly = false) =>
                Sit(agent, Vector3.Zero, preferedLinkNumber, forceScriptedSitOnly);

            private bool TryGetAnimation(ObjectPart sitOnTarget, string sitanim, out UUID animid)
            {
                animid = UUID.Zero;
                AssetMetadata metadata = null;
                ObjectPartInventoryItem item = null;
                if (UUID.TryParse(sitanim, out animid))
                {
                }
                else if (sitOnTarget.Inventory.TryGetValue(sitanim, out item) && item.AssetType == AssetType.Animation)
                {
                    animid = item.AssetID;
                }
                else
                {
                    return false;
                }

                if (sitOnTarget.ObjectGroup?.Scene?.AssetService.Metadata.TryGetValue(animid, out metadata) ?? false)
                {
                    return metadata.Type == AssetType.Animation;
                }
                return false;
            }

            public bool Sit(IAgent agent, Vector3 preferedOffset, int preferedLinkNumber = -1, bool forceScriptedSitOnly = false)
            {
                ObjectGroup sitOn = agent.SittingOnObject;
                Vector3 sitPosition;
                Quaternion sitTarget;
                ObjectPart sitOnTarget;
                lock (m_SitLock)
                {
                    if (!CheckSittable(agent, out sitPosition, out sitTarget, out sitOnTarget, preferedOffset, preferedLinkNumber, forceScriptedSitOnly))
                    {
                        return false;
                    }

                    if (sitOn != null)
                    {
                        sitOn.m_SittingAgents.Remove(agent);
                    }

                    m_Group.m_SittingAgents.Add(agent, sitOnTarget);
                }
                Vector3 localPosition;
                Quaternion localRotation;

                agent.AllowUnsit = sitOnTarget.AllowUnsit;

                if(sitOnTarget.IsSitTargetActive)
                {
                    localPosition = sitOnTarget.SitTargetOffset - SIT_TARGET_OFFSET;
                    localRotation = sitOnTarget.SitTargetOrientation;
                }
                else
                {
                    localPosition = preferedOffset;
                    localRotation = Quaternion.Identity;
#warning Implement Unscripted sit here
                }
                agent.SetSittingOn(sitOnTarget.ObjectGroup, localPosition, localRotation);

                /* we have to set those to zero */
                agent.Velocity = Vector3.Zero;
                agent.AngularVelocity = Vector3.Zero;
                agent.AngularAcceleration = Vector3.Zero;
                agent.Acceleration = Vector3.Zero;
                agent.BeginSitAnimation();
                string sitanim = sitOnTarget.SitAnimation;
                if(sitanim?.Length != 0)
                {
                    UUID animid;
                    if(TryGetAnimation(sitOnTarget, sitanim, out animid))
                    {
                        agent.StopAnimation(agent.GetDefaultAnimationID(), sitOnTarget.ID);
                        agent.PlayAnimation(animid, sitOnTarget.ID);
                    }
                }

                var scene = m_Group.Scene;
                if (scene != null)
                {
                    scene.SendAgentObjectToAllAgents(agent);
                    m_Group.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Link));
                }
                return true;
            }

            public bool CheckSittable(IAgent agent, out Vector3 sitPosition, out Quaternion sitRotation, out ObjectPart sitOnTarget, Vector3 preferedOffset, int preferedLinkNumber = -1, bool forceScriptedSitOnly = false)
            {
                sitOnTarget = null;
                lock (m_SitLock)
                {
                    if (preferedLinkNumber > 0)
                    {
                        ObjectPart part;
                        if (m_Group.TryGetValue(preferedLinkNumber, out part) &&
                            !m_Group.m_SittingAgents.ContainsKey(part))
                        {
                            /* select prim */
                            sitOnTarget = part;
                        }
                    }

                    if (sitOnTarget == null)
                    {
                        foreach (ObjectPart part in m_Group.ValuesByKey1)
                        {
                            if (part.IsSitTargetActive &&
                                !m_Group.m_SittingAgents.ContainsKey(part) && (!part.IsScriptedSitOnly))
                            {
                                /* select prim */
                                sitOnTarget = part;
                            }
                        }
                    }

                    if (sitOnTarget == null)
                    {
                        if(m_Group.RootPart.IsScriptedSitOnly || forceScriptedSitOnly)
                        {
                            sitPosition = Vector3.Zero;
                            sitRotation = Quaternion.Identity;
                            return false;
                        }
                        sitOnTarget = m_Group.RootPart;
                    }
                }
                if (sitOnTarget.IsSitTargetActive)
                {
                    sitPosition = sitOnTarget.SitTargetOffset - SIT_TARGET_OFFSET;
                    sitRotation = sitOnTarget.SitTargetOrientation;
                }
                else
                {
                    sitPosition = preferedOffset;
                    sitRotation = Quaternion.Identity;
#warning Implement Unscripted sit here
                }

                return sitOnTarget != null;
            }

            public bool UnSit(IAgent agent) => UnSit(agent, Vector3.Zero, Quaternion.Identity, false);
            public bool UnSit(IAgent agent, Vector3 targetPos, Quaternion targetRot) => UnSit(agent, targetPos, targetRot, true);

            private bool UnSit(IAgent agent, Vector3 targetPos, Quaternion targetRot, bool paramTarget)
            {
                bool res;
                IObject satOn = null;
                ObjectPart satOnTarget;
                lock (m_SitLock)
                {
                    res = m_Group.m_SittingAgents.Remove(agent, out satOnTarget);
                    if (res)
                    {
                        satOn = agent.SittingOnObject;
                        Vector3 formerPos = agent.GlobalPosition;
                        Quaternion formerRot = agent.GlobalRotation;
                        Vector3 realTargetPosition;
                        Quaternion realTargetRotation;
                        if(paramTarget)
                        {
                            realTargetPosition = targetPos * satOnTarget.ObjectGroup.RootPart.GlobalRotation + satOnTarget.ObjectGroup.RootPart.GlobalPosition + agent.Size / 2;
                            Quaternion q = targetRot * satOnTarget.ObjectGroup.RootPart.GlobalRotation;
                            realTargetRotation = Quaternion.CreateFromEulers(0, 0, q.GetEulerAngles().Z);
                        }
                        else if (satOnTarget.IsUnSitTargetActive)
                        {
                            realTargetPosition = satOnTarget.UnSitTargetOffset * satOnTarget.GlobalRotation + satOnTarget.GlobalPosition + agent.Size / 2;
                            Quaternion q = satOnTarget.UnSitTargetOrientation * satOnTarget.GlobalRotation;
                            realTargetRotation = Quaternion.CreateFromEulers(0, 0, q.GetEulerAngles().Z);
                        }
                        else
                        {
                            realTargetPosition = formerPos + agent.Size / 2;
                            realTargetRotation = Quaternion.CreateFromEulers(0, 0, formerRot.GetEulerAngles().Z);
                        }
                        agent.ClearSittingOn(realTargetPosition, realTargetRotation);
                    }
                }

                satOn?.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Link));

                if (res)
                {
                    agent.StopAllAnimations(satOnTarget.ID);

                    SceneInterface scene = m_Group.Scene;
                    scene?.SendAgentObjectToAllAgents(agent);
                    agent.EndSitAnimation();
                }
                return res;
            }

            public void UnSitAll()
            {
                var agents = new List<IAgent>();
                lock (m_SitLock)
                {
                    agents = new List<IAgent>(m_Group.m_SittingAgents.Keys1);
                }

                foreach(IAgent agent in agents)
                {
                    UnSit(agent);
                }
            }
        }
        #endregion

        #region Script Events
        public void PostEvent(IScriptEvent ev)
        {
            foreach (ObjectPart item in Values)
            {
                item.PostEvent(ev);
            }
        }
        #endregion

        public void GetBoundingBox(out BoundingBox box)
        {
            bool rebuildBoundingBox;
            lock(m_Lock)
            {
                rebuildBoundingBox = m_BoundingBox == null;
                if (!rebuildBoundingBox)
                {
                    box = m_BoundingBox.Value;
                    return;
                }
            }

            box = new BoundingBox();
            var min = new Vector3(double.MaxValue, double.MaxValue, double.MaxValue);
            var max = new Vector3(double.MinValue, double.MinValue, double.MinValue);
            foreach(var p in ValuesByKey1)
            {
                BoundingBox inner;
                p.GetBoundingBox(out inner);
                inner.CenterOffset = p.LocalPosition;
                inner.Size *= p.LocalRotation;
                inner.Size = inner.Size.ComponentMax(-inner.Size);
                max = max.ComponentMax(p.LocalPosition + inner.CenterOffset + inner.Size / 2);
                min = min.ComponentMin(p.LocalPosition + inner.CenterOffset - inner.Size / 2);
            }

            box.CenterOffset = max + min;
            box.Size = max - min;

            lock (m_Lock)
            {
                if (m_BoundingBox == null)
                {
                    m_BoundingBox = box;
                }
            }
        }

        public byte[] TerseData
        {
            get { throw new NotImplementedException(); }
        }

        #region Resource costs
        public double PhysicsCost
        {
            get
            {
                double cost = 0;
                foreach(ObjectPart part in ValuesByKey1)
                {
                    cost += part.PhysicsCost;
                }
                return cost;
            }
        }

        public double LinkCost
        {
            get
            {
                double cost = 0;
                foreach (ObjectPart part in ValuesByKey1)
                {
                    cost += part.LinkCost;
                }
                return cost;
            }
        }

        public double StreamingCost
        {
            get
            {
                double cost = 0;
                foreach(ObjectPart part in ValuesByKey1)
                {
                    cost += part.StreamingCost;
                }
                return cost;
            }
        }

        public double SimulationCost
        {
            get
            {
                double cost = 0;
                foreach(ObjectPart part in ValuesByKey1)
                {
                    cost += part.SimulationCost;
                }
                return cost;
            }
        }
        #endregion
    }
}
