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
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Xml;

namespace SilverSim.Scene.Types.Object
{
    public partial class ObjectGroup : RwLockedSortedDoubleDictionary<int, UUID, ObjectPart>, IObject
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

        private static readonly ILog m_Log = LogManager.GetLogger("OBJECT GROUP");

        #region Events
        public event Action<ObjectGroup, UpdateChangedFlags> OnUpdate;
        public event Action<IObject> OnPositionChange;
        #endregion

        public UInt32 LocalID
        {
            get { return RootPart.LocalID; }
            set { RootPart.LocalID = value; }
        }

        public const int LINK_SET = -1;
        public const int LINK_ALL_OTHERS = -2;
        public const int LINK_ALL_CHILDREN = -3;
        public const int LINK_THIS = -4;
        public const int LINK_ROOT = 1;

        private bool m_IsTempOnRez;
        private bool m_IsTemporary;
        private bool m_IsGroupOwned;
        private bool m_IsIncludedInSearch;
        private AttachmentPoint m_AttachPoint;
        private Vector3 m_AttachedPos = Vector3.Zero;
        private Vector3 m_Velocity = Vector3.Zero;
        private UGI m_Group = UGI.Unknown;
        private UUI m_Owner = UUI.Unknown;
        private UUI m_LastOwner = UUI.Unknown;
        private UUID m_OriginalAssetID = UUID.Zero; /* necessary for reducing asset re-generation */
        private UUID m_NextOwnerAssetID = UUID.Zero; /* necessary for reducing asset re-generation */
        protected internal RwLockedBiDiMappingDictionary<IAgent, ObjectPart> m_SittingAgents = new RwLockedBiDiMappingDictionary<IAgent, ObjectPart>();
        public AgentSittingInterface AgentSitting { get; }
        public SceneInterface Scene { get; set; }
        public UUID FromItemID = UUID.Zero; /* used for attachments */
        public UUID RezzingObjectID = UUID.Zero; /* used alongside llRezObject and llRezAtRoot */
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
        #endregion

        public PathfindingType PathfindingType
        {
            get { return RootPart.PathfindingType; }

            set { RootPart.PathfindingType = value; }
        }

        /* UUID references to PartID and Vector3 is the attaching force */
        public RwLockedDictionary<UUID, Vector3> AttachedForces = new RwLockedDictionary<UUID, Vector3>();

        public UUID OriginalAssetID /* will be set to UUID.Zero when anything has been changed */
        {
            get
            {
                lock(m_Lock)
                {
                    return m_OriginalAssetID;
                }
            }
            set
            {
                lock(m_Lock)
                {
                    m_OriginalAssetID = value;
                }
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
                lock (m_Lock)
                {
                    return m_NextOwnerAssetID;
                }
            }
            set
            {
                lock (m_Lock)
                {
                    m_NextOwnerAssetID = value;
                }
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
            foreach (Action<ObjectGroup, UpdateChangedFlags> del in OnUpdate?.GetInvocationList() ?? new Delegate[0])
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

            foreach (Action<ObjectGroup, UpdateChangedFlags> del in OnUpdate?.GetInvocationList() ?? new Delegate[0])
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
                    m_KeyframedMotion.Stop();
                    m_KeyframedMotion.Dispose();
                    m_KeyframedMotion = null;
                }
                if (value != null)
                {
                    lock(m_KeyframeMotionUpdateLock)
                    {
                        if(m_KeyframedMotion == null)
                        {
                            var controller = new KeyframedMotionController(this)
                            {
                                Program = value
                            };
                            m_KeyframedMotion = controller;
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
                    m_IsTemporary = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public bool IsTempOnRez
        {
            get { return m_IsTempOnRez; }

            set
            {
                m_IsTempOnRez = value;
                lock (m_Lock)
                {
                    m_IsTemporary = m_IsTemporary && m_IsTempOnRez;
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
                lock (m_Lock)
                {
                    return m_IsGroupOwned;
                }
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
                lock (m_Lock)
                {
                    return new UGI(m_Group);
                }
            }
            set
            {
                lock (m_Lock)
                {
                    m_Group = new UGI(value);
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public UUI LastOwner
        {
            get
            {
                lock(m_Lock)
                {
                    return new UUI(m_LastOwner);
                }
            }
            set
            {
                lock(m_Lock)
                {
                    m_LastOwner = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public UUI Owner
        {
            get
            {
                lock (m_Lock)
                {
                    return new UUI(m_Owner);
                }
            }
            set
            {
                lock (m_Lock)
                {
                    m_Owner = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Owner);
            }
        }

        public ObjectPart RootPart => this[LINK_ROOT];

        public Vector3 Velocity
        {
            get
            {
                lock (m_Lock)
                {
                    return m_Velocity;
                }
            }
            set
            {
                lock (m_Lock)
                {
                    m_Velocity = value;
                }
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
                return m_SittingAgents.Keys1[linkTarget];
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
        public void GetPrimitiveParams(int linkThis, int linkTarget, AnArray.Enumerator enumerator, AnArray paramList)
        {
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
                        paramList.Add(IsTempOnRez);
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
                                obj.GetPrimitiveParams(enumerator, paramList);
                                break;
                        }
                        break;
                }
            }
        }

        public void GetPrimitiveParams(AnArray.Enumerator enumerator, AnArray paramList)
        {
            GetPrimitiveParams(LINK_ROOT, LINK_ROOT, enumerator, paramList);
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
            else if(linkTarget < LINK_ROOT)
            {
                throw new LocalizedScriptErrorException(this, "InvalidLinkTargetParameterForFunction0Msg1", "Invalid link target parameter for {0}: {1}", "SetPrimitiveParams", linkTarget);
            }

            while (enumerator.MoveNext())
            {
                switch (ParamsHelper.GetPrimParamType(enumerator))
                {
                    case PrimitiveParamsType.LinkTarget:
                        if (!enumerator.MoveNext())
                        {
                            return;
                        }
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
                        IsTempOnRez = ParamsHelper.GetBoolean(enumerator, "PRIM_TEMP_ON_REZ");
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
                                    obj.SetPrimitiveParams(enumerator);
                                }
                                m_SittingAgents.ForEach((IAgent agent) =>
                                {
                                    enumerator.GoToMarkPosition();
                                    agent.SetPrimitiveParams(enumerator);
                                });
                                break;

                            case LINK_ALL_CHILDREN:
                                enumerator.MarkPosition();
                                ForEach((KeyValuePair<int, ObjectPart> kvp) =>
                                {
                                    if (kvp.Key != LINK_ROOT)
                                    {
                                        enumerator.GoToMarkPosition();
                                        kvp.Value.SetPrimitiveParams(enumerator);
                                    }
                                });
                                m_SittingAgents.ForEach((IAgent agent) =>
                                {
                                    enumerator.GoToMarkPosition();
                                    agent.SetPrimitiveParams(enumerator);
                                });
                                break;

                            case LINK_ALL_OTHERS:
                                enumerator.MarkPosition();
                                ForEach((KeyValuePair<int, ObjectPart> kvp) =>
                                {
                                    if (kvp.Key != linkThis)
                                    {
                                        enumerator.GoToMarkPosition();
                                        kvp.Value.SetPrimitiveParams(enumerator);
                                    }
                                });
                                m_SittingAgents.ForEach((IAgent agent) =>
                                {
                                    enumerator.GoToMarkPosition();
                                    agent.SetPrimitiveParams(enumerator);
                                });
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

            private static readonly Vector3 SIT_TARGET_OFFSET = new Vector3(0, 0, 0.4);

            public void Sit(IAgent agent, int preferedLinkNumber = -1)
            {
                Sit(agent, Vector3.Zero, preferedLinkNumber);
            }

            public void Sit(IAgent agent, Vector3 preferedOffset, int preferedLinkNumber = -1)
            {
                var sitOn = (ObjectGroup)agent.SittingOnObject;
                if(sitOn != null)
                {
                    sitOn.m_SittingAgents.Remove(agent);
                }

                Vector3 sitPosition;
                Quaternion sitTarget;
                ObjectPart sitOnTarget;
                lock (m_SitLock)
                {
                    CheckSittable(agent, out sitPosition, out sitTarget, out sitOnTarget, preferedOffset, preferedLinkNumber);
                    m_Group.m_SittingAgents.Add(agent, sitOnTarget);
                    agent.SittingOnObject = sitOn;
                }
                agent.AllowUnsit = sitOnTarget.AllowUnsit;

                if(!sitOnTarget.SitTargetOffset.ApproxEquals(Vector3.Zero, double.Epsilon) ||
                    !sitOnTarget.SitTargetOrientation.ApproxEquals(Quaternion.Identity, double.Epsilon))
                {
                    agent.LocalPosition = sitOnTarget.SitTargetOffset - SIT_TARGET_OFFSET;
                    agent.LocalRotation = sitOnTarget.SitTargetOrientation;
                }
                else
                {
                    agent.LocalPosition = preferedOffset;
                    agent.GlobalRotation = Quaternion.Identity;
#warning Implement Unscripted sit here
                }

                agent.SetDefaultAnimation("sitting");

                var scene = m_Group.Scene;
                if (scene != null)
                {
                    scene.SendAgentObjectToAllAgents(agent);
                    m_Group.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Link));
                }
            }

            public bool CheckSittable(IAgent agent, out Vector3 sitPosition, out Quaternion sitRotation, out ObjectPart sitOnTarget, Vector3 preferedOffset, int preferedLinkNumber = -1)
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
                            if ((!part.SitTargetOffset.ApproxEquals(Vector3.Zero, double.Epsilon) ||
                                !part.SitTargetOrientation.ApproxEquals(Quaternion.Identity, double.Epsilon)) &&
                                !m_Group.m_SittingAgents.ContainsKey(part) && (!part.IsScriptedSitOnly))
                            {
                                /* select prim */
                                sitOnTarget = part;
                            }
                        }
                    }

                    if (sitOnTarget == null)
                    {
                        if(m_Group.RootPart.IsScriptedSitOnly)
                        {
                            sitPosition = Vector3.Zero;
                            sitRotation = Quaternion.Identity;
                            return false;
                        }
                        sitOnTarget = m_Group.RootPart;
                    }
                }
                if (!sitOnTarget.SitTargetOffset.ApproxEquals(Vector3.Zero, double.Epsilon) ||
                    !sitOnTarget.SitTargetOrientation.ApproxEquals(Quaternion.Identity, double.Epsilon))
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

            public bool UnSit(IAgent agent)
            {
                bool res;
                IObject satOn = null;
                lock (m_SitLock)
                {
                    res = m_Group.m_SittingAgents.Remove(agent);
                    if (res)
                    {
                        satOn = agent.SittingOnObject;
                        agent.SittingOnObject = null;
                    }
                }

                satOn?.PostEvent(new ChangedEvent(ChangedEvent.ChangedFlags.Link));

                if (res)
                {
                    SceneInterface scene = m_Group.Scene;
                    scene?.SendAgentObjectToAllAgents(agent);
                    agent.SetDefaultAnimation("standing");
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
            ForEach((ObjectPart item) => item.PostEvent(ev));
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

        #region XML Serialization
        public void ToXml(XmlTextWriter writer, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            ToXml(writer, UUI.Unknown, Vector3.Zero, options, false);
        }

        public void ToXml(XmlTextWriter writer, UUI nextOwner, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            ToXml(writer, nextOwner, Vector3.Zero, options, false);
        }

        public void ToXml(XmlTextWriter writer, UUI nextOwner, Vector3 offsetpos, XmlSerializationOptions options = XmlSerializationOptions.None, bool writeOffsetPos = true)
        {
            List<ObjectPart> parts = Values;
            writer.WriteStartElement("SceneObjectGroup");
            if (writeOffsetPos)
            {
                Vector3 opos = Position - offsetpos;
                writer.WriteAttributeString("x", opos.X.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteAttributeString("y", opos.Y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteAttributeString("z", opos.Z.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            if ((options & XmlSerializationOptions.WriteXml2) == 0)
            {
                writer.WriteStartElement("RootPart");
            }
            RootPart.ToXml(writer, options);
            if ((options & XmlSerializationOptions.WriteXml2) == 0)
            {
                writer.WriteEndElement();
            }
            writer.WriteStartElement("OtherParts");
            foreach (ObjectPart p in parts)
            {
                if(p.ID != RootPart.ID)
                {
                    if ((options & XmlSerializationOptions.WriteXml2) == 0)
                    {
                        writer.WriteStartElement("Part");
                    }
                    p.ToXml(writer, nextOwner, options);
                    if ((options & XmlSerializationOptions.WriteXml2) == 0)
                    {
                        writer.WriteEndElement();
                    }
                }
            }
            writer.WriteEndElement();

#warning KeyframeMotion Base64

            bool haveScriptState = false;
            foreach(ObjectPart p in parts)
            {
                foreach(ObjectPartInventoryItem i in p.Inventory.Values)
                {
                    IScriptState scriptState = i.ScriptState;
                    if(scriptState != null)
                    {
                        if(!haveScriptState)
                        {
                            writer.WriteStartElement("GroupScriptStates");
                            haveScriptState = true;
                        }

                        writer.WriteStartElement("SavedScriptState");
                        writer.WriteAttributeString("UUID", i.ID.ToString());

                        scriptState.ToXml(writer);

                        writer.WriteEndElement();
                    }
                }
            }
            if(haveScriptState)
            {
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        #endregion

        #region XML Deserialization
        private static ObjectPart ParseOtherPart(XmlTextReader reader, ObjectGroup group, UUI currentOwner)
        {
            ObjectPart otherPart = null;
            if (reader.IsEmptyElement)
            {
                throw new InvalidObjectXmlException();
            }
            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                bool isEmptyElement = reader.IsEmptyElement;
                string nodeName = reader.Name;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (nodeName)
                        {
                            case "SceneObjectPart":
                                if (isEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                if (otherPart != null)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                otherPart = ObjectPart.FromXml(reader, null, currentOwner);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != "Part")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return otherPart;

                    default:
                        break;
                }
            }
        }

        private static void FromXmlOtherParts(XmlTextReader reader, ObjectGroup group, UUI currentOwner)
        {
            ObjectPart part;
            var links = new SortedDictionary<int, ObjectPart>();
            if (reader.IsEmptyElement)
            {
                throw new InvalidObjectXmlException();
            }

            for (; ; )
            {
                if (!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                bool isEmptyElement = reader.IsEmptyElement;
                string nodeName = reader.Name;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (nodeName)
                        {
                            case "Part":
                                if (isEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                part = ParseOtherPart(reader, group, currentOwner);
                                links.Add(part.LoadedLinkNumber, part);
                                break;

                            case "SceneObjectPart":
                                if (isEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                part = ObjectPart.FromXml(reader, null, currentOwner);
                                try
                                {
                                    part.LoadedLinkNumber = links.Count + 2;
                                    links.Add(part.LoadedLinkNumber, part);
                                }
                                catch
                                {
                                    throw new ObjectDeserializationFailedDueKeyException();
                                }
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != "OtherParts")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        foreach(KeyValuePair<int, ObjectPart> kvp in links)
                        {
                            group.Add(kvp.Key, kvp.Value.ID, kvp.Value);
                        }
                        return;

                    default:
                        break;
                }
            }
        }

        private static ObjectPart ParseRootPart(XmlTextReader reader, ObjectGroup group, UUI currentOwner)
        {
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

                string nodeName = reader.Name;
                bool isEmptyElement = reader.IsEmptyElement;

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (nodeName)
                        {
                            case "SceneObjectPart":
                                if(rootPart != null)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                if (isEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                if (rootPart != null)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                rootPart = ObjectPart.FromXml(reader, group, currentOwner);
                                group.Add(LINK_ROOT, rootPart.ID, rootPart);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(nodeName != "RootPart")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return rootPart;

                    default:
                        break;
                }
            }
        }

        public static ObjectGroup FromXml(XmlTextReader reader, UUI currentOwner, bool inRootPart = false)
        {
            var group = new ObjectGroup();
            ObjectPart rootPart = null;
            if(reader.IsEmptyElement)
            {
                throw new InvalidObjectXmlException();
            }

            for(;;)
            {
                if(inRootPart)
                {
                    inRootPart = false;
                }
                else if(!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                bool isEmptyElement = reader.IsEmptyElement;
                string nodeName = reader.Name;
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (nodeName)
                        {
                            case "RootPart":
                                if (isEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                if (rootPart != null)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                rootPart = ParseRootPart(reader, group, currentOwner);
                                break;

                            case "SceneObjectPart":
                                if(rootPart != null)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                if (isEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                if (rootPart != null)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                rootPart = ObjectPart.FromXml(reader, group, currentOwner);
                                group.Add(LINK_ROOT, rootPart.ID, rootPart);
                                break;

                            case "OtherParts":
                                if (isEmptyElement)
                                {
                                    break;
                                }
                                FromXmlOtherParts(reader, group, currentOwner);
                                break;

                            case "GroupScriptStates":
                                if (isEmptyElement)
                                {
                                    break;
                                }
                                FromXmlGroupScriptStates(reader, group);
                                break;

                            case "KeyframeMotion":
                                /* this should only be serialized when doing OAR and sim state save */
                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != "SceneObjectGroup")
                        {
                            throw new InvalidObjectXmlException();
                        }

                        foreach (ObjectPart part in group.Values)
                        {
                            part.Owner = currentOwner;
                        }
                        group.FinalizeObject();
                        return group;

                    default:
                        break;
                }
            }
        }

        public void FinalizeObject()
        {
            ObjectPart rootPart = RootPart;

            foreach (ObjectPart part in Values)
            {
                /* make those parameters align well */
                part.IsPhantom = rootPart.IsPhantom;
                part.IsPhysics = rootPart.IsPhysics;
                part.IsVolumeDetect = rootPart.IsVolumeDetect;

                part.ObjectGroup = this;
                part.UpdateData(ObjectPart.UpdateDataFlags.All);
            }
        }

        private static void FromXmlGroupScriptStates(XmlTextReader reader, ObjectGroup group)
        {
            var itemID = UUID.Zero;

            for(;;)
            {
                if(!reader.Read())
                {
                    throw new InvalidObjectXmlException();
                }

                bool isEmptyElement = reader.IsEmptyElement;
                string nodeName = reader.Name;

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (isEmptyElement)
                        {
                            break;
                        }
                        switch (nodeName)
                        {
                            case "SavedScriptState":
                                itemID = UUID.Zero;
                                if (reader.MoveToFirstAttribute())
                                {
                                    do
                                    {
                                        switch (reader.Name)
                                        {
                                            case "UUID":
                                                if(!UUID.TryParse(reader.Value, out itemID))
                                                {
                                                    throw new InvalidObjectXmlException();
                                                }
                                                break;

                                            default:
                                                break;
                                        }
                                    }
                                    while (reader.MoveToNextAttribute());
                                }

                                FromXmlSavedScriptState(reader, group, itemID);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (nodeName != "GroupScriptStates")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }

        private static void FromXmlSavedScriptStateInner(XmlTextReader reader, ObjectGroup group, UUID itemID)
        {
            string tagname = reader.Name;
            var attrs = new Dictionary<string, string>();
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    attrs[reader.Name] = reader.Value;
                } while (reader.MoveToNextAttribute());
            }
            ObjectPartInventoryItem item = null;

            if (!attrs.ContainsKey("Asset") || !attrs.ContainsKey("Engine"))
            {
                reader.ReadToEndElement(tagname);
                return;
            }

            foreach (ObjectPart part in group.Values)
            {
                if (part.Inventory.ContainsKey(itemID))
                {
                    item = part.Inventory[itemID];
                    UUID assetid;

                    /* validate inventory item */
                    if (!UUID.TryParse(attrs["Asset"], out assetid) ||
                        item.AssetType != SilverSim.Types.Asset.AssetType.LSLText ||
                        item.InventoryType != SilverSim.Types.Inventory.InventoryType.LSLText ||
                        assetid != item.AssetID)
                    {
                        item = null;
                    }
                    break;
                }
            }

            if (item == null)
            {
                reader.ReadToEndElement(tagname);
            }
            else
            {
                IScriptCompiler compiler;
                try
                {
                    compiler = CompilerRegistry[attrs["Engine"]];
                }
                catch
                {
                    reader.ReadToEndElement(tagname);
                    return;
                }

                try
                {
                    item.ScriptState = compiler.StateFromXml(reader, attrs, item);
                }
                catch (ScriptStateLoaderNotImplementedException)
                {
                    reader.ReadToEndElement(tagname);
                    return;
                }
            }
        }

        private static void FromXmlSavedScriptState(XmlTextReader reader, ObjectGroup group, UUID itemID)
        {
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
                            case "State":
                                FromXmlSavedScriptStateInner(reader, group, itemID);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "SavedScriptState")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }
        #endregion
    }
}
