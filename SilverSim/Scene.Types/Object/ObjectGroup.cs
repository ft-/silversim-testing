// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Inventory;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Object
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "DoNotLockOnThisOrTypesRule")]
    public partial class ObjectGroup : RwLockedSortedDoubleDictionary<int, UUID, ObjectPart>, IObject
    {
        static IScriptCompilerRegistry m_CompilerRegistry;
        public static IScriptCompilerRegistry CompilerRegistry
        {
            get
            {
                return m_CompilerRegistry;
            }
            set
            {
                if(m_CompilerRegistry == null)
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
            get
            {
                return RootPart.LocalID;
            }
            set
            {
                RootPart.LocalID = value;
            }
        }

        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const int LINK_SET = -1;
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const int LINK_ALL_OTHERS = -2;
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const int LINK_ALL_CHILDREN = -3;
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const int LINK_THIS = -4;
        [SuppressMessage("Gendarme.Rules.BadPractice", "AvoidVisibleConstantFieldRule")]
        public const int LINK_ROOT = 1;

        private bool m_IsTempOnRez;
        private bool m_IsTemporary;
        private bool m_IsGroupOwned;
        private AttachmentPoint m_AttachPoint;
        private Vector3 m_AttachedPos = Vector3.Zero;
        private Vector3 m_Velocity = Vector3.Zero;
        private UGI m_Group = UGI.Unknown;
        private UUI m_Owner = UUI.Unknown;
        private UUI m_LastOwner = UUI.Unknown;
        private UUID m_OriginalAssetID = UUID.Zero; /* necessary for reducing asset re-generation */
        private UUID m_NextOwnerAssetID = UUID.Zero; /* necessary for reducing asset re-generation */
        protected internal RwLockedBiDiMappingDictionary<IAgent, ObjectPart> m_SittingAgents = new RwLockedBiDiMappingDictionary<IAgent, ObjectPart>();
        public AgentSittingInterface AgentSitting { get; private set; }
        public SceneInterface Scene { get; set; }
        public UUID FromItemID = UUID.Zero; /* used for attachments */

        AssetServiceInterface m_AssetService;
        public AssetServiceInterface AssetService /* specific for attachments usage */
        { 
            get
            {
                if (null == m_AssetService)
                {
                    return Scene.AssetService;
                }
                else
                {
                    return m_AssetService;
                }
            }
            set
            {
                /* set is specific for attachments to reduce immediate rezzing load */
                m_AssetService = value;
            }
        }
        private Vector3 m_Acceleration = Vector3.Zero;
        private Vector3 m_AngularAcceleration = Vector3.Zero;

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
                foreach(ObjectPart p in Values)
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

        public UUID OriginalAssetID /* will be set to UUID.Zero when anything has been changed */
        {
            get
            {
                lock(this)
                {
                    return m_OriginalAssetID;
                }
            }
            set
            {
                lock(this)
                {
                    m_OriginalAssetID = value;
                }
            }
        }

        public UUID NextOwnerAssetID /* will be set to UUID.Zero when anything has been changed */
        {
            get
            {
                lock (this)
                {
                    return m_NextOwnerAssetID;
                }
            }
            set
            {
                lock (this)
                {
                    m_NextOwnerAssetID = value;
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        private void TriggerOnUpdate(UpdateChangedFlags flags)
        {
            if (Count == 0)
            {
                return;
            }
            lock (this)
            {
                OriginalAssetID = UUID.Zero;
                NextOwnerAssetID = UUID.Zero;
            }

            var ev = OnUpdate; /* events are not exactly thread-safe, so copy the reference first */
            if (ev != null)
            {
                Action<ObjectGroup, UpdateChangedFlags>[] invocationList = (Action<ObjectGroup, UpdateChangedFlags>[])ev.GetInvocationList();
                foreach (Action<ObjectGroup, UpdateChangedFlags> del in invocationList)
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

            RootPart.TriggerOnUpdate(flags);
        }

        #region Properties
        public bool IsChanged { get; private set; }

        public SilverSim.Types.Inventory.InventoryItem.SaleInfoData.SaleType m_SaleType;
        public int m_SalePrice;
        public int m_OwnershipCost;
        public UInt32 m_Category;
        public int m_PayPrice0 = -1;
        public int m_PayPrice1 = -1;
        public int m_PayPrice2 = -1;
        public int m_PayPrice3 = -1;
        public int m_PayPrice4 = -1;

        public int PayPrice0
        {
            get
            {
                return m_PayPrice0;
            }
            set
            {
                m_PayPrice0 = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int PayPrice1
        {
            get
            {
                return m_PayPrice1;
            }
            set
            {
                m_PayPrice1 = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int PayPrice2
        {
            get
            {
                return m_PayPrice2;
            }
            set
            {
                m_PayPrice2 = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int PayPrice3
        {
            get
            {
                return m_PayPrice3;
            }
            set
            {
                m_PayPrice3 = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int PayPrice4
        {
            get
            {
                return m_PayPrice4;
            }
            set
            {
                m_PayPrice4 = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public InventoryItem.SaleInfoData.SaleType SaleType
        {
            get
            {
                return m_SaleType;
            }
            set
            {
                m_SaleType = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int SalePrice
        {
            get
            {
                return m_SalePrice;
            }
            set
            {
                m_SalePrice = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public int OwnershipCost
        {
            get
            {
                return m_OwnershipCost;
            }
            set
            {
                m_OwnershipCost = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public UInt32 Category
        {
            get
            {
                return m_Category;
            }
            set
            {
                m_Category = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public AttachmentPoint AttachPoint
        {
            get
            {
                return m_AttachPoint;
            }
            set
            {
                m_AttachPoint = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        bool m_IsAttached;

        public bool IsAttached
        {
            get
            {
                return m_IsAttached;
            }
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

        public Vector3 Acceleration
        {
            get
            {
                return m_Acceleration;
            }
            set
            {
                m_Acceleration = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Vector3 AttachedPos
        {
            get
            {
                return m_AttachedPos;
            }
            set
            {
                m_AttachedPos = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Vector3 AngularAcceleration
        {
            get
            {
                return m_AngularAcceleration;
            }
            set
            {
                m_AngularAcceleration = value;
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public bool IsTemporary
        {
            get
            {
                return m_IsTemporary;
            }
            set
            {
                lock(this) 
                {
                    m_IsTemporary = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public bool IsTempOnRez
        {
            get
            {
                return m_IsTempOnRez;
            }
            set
            {
                m_IsTempOnRez = value;
                lock(this) 
                {
                    m_IsTemporary = m_IsTemporary && m_IsTempOnRez;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Vector3 Size
        {
            get
            {
                return RootPart.Size;
            }
            set
            {
                RootPart.Size = value;
            }
        }

        public bool IsGroupOwned
        {
            get
            {
                lock (this)
                {
                    return m_IsGroupOwned;
                }
            }
            set
            {
                bool changed = false;
                lock (this)
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
                lock (this)
                {
                    return new UGI(m_Group);
                }
            }
            set
            {
                lock (this)
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
                lock(this)
                {
                    return new UUI(m_LastOwner);
                }
            }
            set
            {
                lock(this)
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
                lock (this)
                {
                    return new UUI(m_Owner);
                }
            }
            set
            {
                lock (this)
                {
                    m_Owner = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(UpdateChangedFlags.Owner);
            }
        }

        public ObjectPart RootPart
        {
            get
            {
                return this[LINK_ROOT]; /* we always count from one here */
            }
        }

        public Vector3 Velocity
        {
            get
            {
                lock (this)
                {
                    return m_Velocity;
                }
            }
            set
            {
                lock (this)
                {
                    m_Velocity = value;
                }
                IsChanged = m_IsChangedEnabled;
                TriggerOnUpdate(0);
            }
        }

        public Vector3 AngularVelocity
        {
            get
            {
                return RootPart.AngularVelocity;
            }
            set
            {
                RootPart.AngularVelocity = value;
            }
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
                linkTarget -= (PrimCount + 1);
                return m_SittingAgents.Keys1[linkTarget];
            }
            else
            {
                return this[linkTarget];
            }
        }
        #endregion

        public bool IsInScene(SceneInterface scene)
        {
            return true;
        }

        public void InvokeOnPositionUpdate(IObject obj)
        {
            if(obj != RootPart)
            {
                return;
            }
            var e = OnPositionChange; /* events are not exactly thread-safe, so copy the reference first */
            if (e != null)
            {
                Action<IObject>[] invocationList = (Action<IObject>[])e.GetInvocationList();
                foreach (Action<IObject> del in invocationList)
                {
                    del(this);
                }
            }
        }

        #region Primitive Params Methods
        public void GetPrimitiveParams(int linkThis, int linkTarget, AnArray.Enumerator enumerator, ref AnArray paramList)
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
                throw new ArgumentException(String.Format("Invalid link target parameter for GetPrimitiveParams: {0}", linkTarget));
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
                                throw new ArgumentException("Link Target LINK_SET not allowed for GetPrimitiveParams");

                            case LINK_ALL_OTHERS:
                                throw new ArgumentException("Link Target LINK_ALL_OTHERS not allowed for GetPrimitiveParams");

                            case LINK_ALL_CHILDREN:
                                throw new ArgumentException("Link Target LINK_ALL_CHILDREN not allowed for GetPrimitiveParams");

                            default:
                                if(linkTarget < 1)
                                {
                                    throw new ArgumentException(String.Format("Invalid link target {0}", linkTarget));
                                }
                                try
                                {
                                    IObject obj = GetObjectLink(linkTarget);
                                    obj.GetPrimitiveParams(enumerator, ref paramList);
                                }
                                catch(KeyNotFoundException)
                                {
                                    throw new ArgumentException(String.Format("Link target {0} does not exist", linkTarget));
                                }
                                break;
                        }
                        break;
                }
            }
        }

        public void GetPrimitiveParams(AnArray.Enumerator enumerator, ref AnArray paramList)
        {
            GetPrimitiveParams(LINK_ROOT, LINK_ROOT, enumerator, ref paramList);
        }

        public void SetPrimitiveParams(int linkThis, int linkTarget, AnArray.MarkEnumerator enumerator)
        {
            if (0 == linkTarget)
            {
                linkTarget = LINK_ROOT;
            }
            else if(linkTarget < LINK_ROOT)
            {
                throw new ArgumentException(String.Format("Invalid link target parameter for SetPrimitiveParams: {0}", linkTarget));
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
                                m_SittingAgents.ForEach(delegate(IAgent agent)
                                {
                                    enumerator.GoToMarkPosition();
                                    agent.SetPrimitiveParams(enumerator);
                                });
                                break;

                            case LINK_ALL_CHILDREN:
                                enumerator.MarkPosition();
                                ForEach(delegate(KeyValuePair<int, ObjectPart> kvp)
                                {
                                    if (kvp.Key != LINK_ROOT)
                                    {
                                        enumerator.GoToMarkPosition();
                                        kvp.Value.SetPrimitiveParams(enumerator);
                                    }
                                });
                                m_SittingAgents.ForEach(delegate(IAgent agent)
                                {
                                    enumerator.GoToMarkPosition();
                                    agent.SetPrimitiveParams(enumerator);
                                });
                                break;

                            case LINK_ALL_OTHERS:
                                enumerator.MarkPosition();
                                ForEach(delegate(KeyValuePair<int, ObjectPart> kvp)
                                {
                                    if (kvp.Key != linkThis)
                                    {
                                        enumerator.GoToMarkPosition();
                                        kvp.Value.SetPrimitiveParams(enumerator);
                                    }
                                });
                                m_SittingAgents.ForEach(delegate(IAgent agent)
                                {
                                    enumerator.GoToMarkPosition();
                                    agent.SetPrimitiveParams(enumerator);
                                });
                                break;

                            default:
                                if (linkTarget < 1)
                                {
                                    throw new ArgumentException(String.Format("Invalid link target {0}", linkTarget));
                                }
                                try
                                {
                                    IObject obj = GetObjectLink(linkTarget);
                                    obj.SetPrimitiveParams(enumerator);
                                }
                                catch(KeyNotFoundException)
                                {
                                    throw new ArgumentException(String.Format("Link target {0} does not exist", linkTarget));
                                }
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
        public void GetObjectDetails(AnArray.Enumerator enumerator, ref AnArray paramList)
        {
            while(enumerator.MoveNext())
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
                        paramList.Add(RootPart.Rotation);
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
                        paramList.Add(RootPart.Creator.ID);
                        break;
                    
                    case ObjectDetailsType.RunningScriptCount:
                        {
                            int n = 0;
                            foreach(ObjectPart obj in this.Values)
                            {
                                n += obj.Inventory.CountRunningScripts;
                            }
                            paramList.Add(n);
                        }
                        break;

                    case ObjectDetailsType.TotalScriptCount:
                        {
                            int n = 0;
                            foreach(ObjectPart obj in this.Values)
                            {
                                n += obj.Inventory.CountScripts;
                            }
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
                        paramList.Add(Count);
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
                        paramList.Add((int)AttachPoint);
                        break;

                    case ObjectDetailsType.PathfindingType:
                        paramList.Add(0);
                        break;

                    case ObjectDetailsType.Physics:
                        paramList.Add(IsPhysics);
                        break;

                    case ObjectDetailsType.Phantom:
                        paramList.Add(IsPhantom);
                        break;

                    case ObjectDetailsType.TempOnRez:
                        paramList.Add(IsTempOnRez);
                        break;

                    case ObjectDetailsType.RenderWeight:
                        paramList.Add(0);
                        break;

                    default:
                        throw new ArgumentException("Unknown Object Details Type");
                }
            }
        }
        #endregion

        #region Agent Sitting
        public class AgentSittingInterface
        {
            readonly ObjectGroup m_Group;

            public AgentSittingInterface(ObjectGroup group)
            {
                m_Group = group;
            }

            public IAgent this[ObjectPart p]
            {
                get
                {
                    return m_Group.m_SittingAgents[p];
                }
            }
        }
        #endregion

        #region Script Events
        public void PostEvent(IScriptEvent ev)
        {
            ForEach(delegate(ObjectPart item)
            {
                item.PostEvent(ev);
            });
        }
        #endregion

        public byte[] TerseData
        {
            get
            {
                throw new NotImplementedException();
            }
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
                writer.WriteStartAttribute("x");
                writer.WriteValue(opos.X);
                writer.WriteEndAttribute();
                writer.WriteStartAttribute("y");
                writer.WriteValue(opos.Y);
                writer.WriteEndAttribute();
                writer.WriteStartAttribute("z");
                writer.WriteValue(opos.Z);
                writer.WriteEndAttribute();
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
                        writer.WriteStartAttribute("UUID");
                        writer.WriteValue(i.ID);
                        writer.WriteEndAttribute();

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
        static ObjectPart ParseOtherPart(XmlTextReader reader, ObjectGroup group, UUI currentOwner)
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

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        static void FromXmlOtherParts(XmlTextReader reader, ObjectGroup group, UUI currentOwner)
        {
            ObjectPart part;
            SortedDictionary<int, ObjectPart> links = new SortedDictionary<int, ObjectPart>();
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

        static ObjectPart ParseRootPart(XmlTextReader reader, ObjectGroup group, UUI currentOwner)
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
            ObjectGroup group = new ObjectGroup();
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

                            case "KeyframeMotion":
                                reader.ReadToEndElement();
                                break;

                            case "GroupScriptStates":
                                if (isEmptyElement)
                                {
                                    break;
                                }
                                FromXmlGroupScriptStates(reader, group);
                                break;

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

                        foreach(ObjectPart part in group.Values)
                        {
                            part.ObjectGroup = group;
                            part.Owner = currentOwner;
                            part.UpdateData(ObjectPart.UpdateDataFlags.All);
                        }
                        return group;

                    default:
                        break;
                }
            }
        }

        public void FinalizeObject()
        {
            foreach (ObjectPart part in this.Values)
            {
                part.ObjectGroup = this;
                part.UpdateData(ObjectPart.UpdateDataFlags.All);
            }
        }

        static void FromXmlGroupScriptStates(XmlTextReader reader, ObjectGroup group)
        {
            UUID itemID = UUID.Zero;

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

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        static void FromXmlSavedScriptStateInner(XmlTextReader reader, ObjectGroup group, UUID itemID)
        {
            string tagname = reader.Name;
            Dictionary<string, string> attrs = new Dictionary<string, string>();
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

            if (null == item)
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

        static void FromXmlSavedScriptState(XmlTextReader reader, ObjectGroup group, UUID itemID)
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
