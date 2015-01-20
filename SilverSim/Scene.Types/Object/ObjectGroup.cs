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
using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Physics;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Xml;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Object
{
    public class ObjectGroup : RwLockedSortedDoubleDictionary<int, UUID, ObjectPart>, IObject, IDisposable
    {
        private static readonly ILog m_Log = LogManager.GetLogger("OBJECT GROUP");

        #region Events
        public delegate void OnUpdateDelegate(ObjectGroup objgroup, ChangedEvent.ChangedFlags flags);
        public event OnUpdateDelegate OnUpdate;
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

        public const int LINK_SET = -1;
        public const int LINK_ALL_OTHERS = -2;
        public const int LINK_ALL_CHILDREN = -3;
        public const int LINK_THIS = -4;
        public const int LINK_ROOT = 1;

        private bool m_IsTempOnRez = false;
        private bool m_IsTemporary = false;
        private bool m_IsGroupOwned = false;
        private AttachmentPoint m_AttachPoint = AttachmentPoint.NotAttached;
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

        AssetServiceInterface m_AssetService = null;
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

        #region Constructor
        public ObjectGroup()
        {
            AgentSitting = new AgentSittingInterface(this);
            IsChanged = false;
            PhysicsActor = DummyPhysicsObject.SharedInstance;
        }

        public void Dispose()
        {
            Scene = null;
        }
        #endregion

        #region Physics Linkage
        IPhysicsObject m_PhysicsActor = DummyPhysicsObject.SharedInstance;

        public IPhysicsObject PhysicsActor
        {
            get
            {
                lock (this)
                {
                    return RootPart.PhysicsActor;
                }
            }
            set
            {
                throw new InvalidOperationException();
            }
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

        private void TriggerOnUpdate(ChangedEvent.ChangedFlags flags)
        {
            lock (this)
            {
                OriginalAssetID = UUID.Zero;
                NextOwnerAssetID = UUID.Zero;
            }

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

            RootPart.TriggerOnUpdate(flags);
        }

        #region Properties
        public bool IsChanged { get; private set; }

        public AttachmentPoint AttachPoint
        {
            get
            {
                return m_AttachPoint;
            }
            set
            {
                m_AttachPoint = value;
                IsChanged = true;
                TriggerOnUpdate(0);
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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
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

        public bool IsPhantom
        {
            get
            {
                return PhysicsActor.IsPhantom;
            }
            set
            {
                PhysicsActor.IsPhantom = value;
                IsChanged = true;
                TriggerOnUpdate(0);
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
                    IsChanged = true;
                    TriggerOnUpdate(ChangedEvent.ChangedFlags.Owner);
                }
            }
        }

        public bool IsPhysics
        {
            get
            {
                return PhysicsActor.IsPhysicsActive;
            }
            set
            {
                PhysicsActor.IsPhysicsActive = value;
                IsChanged = true;
                TriggerOnUpdate(0);
            }
        }

        public bool IsVolumeDetect
        {
            get
            {
                return PhysicsActor.IsVolumeDetect;
            }
            set
            {
                PhysicsActor.IsVolumeDetect = value;
                IsChanged = true;
                TriggerOnUpdate(0);
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
                IsChanged = true;
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
                IsChanged = true;
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
                IsChanged = true;
                TriggerOnUpdate(ChangedEvent.ChangedFlags.Owner);
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
                IsChanged = true;
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
                foreach (Action<IObject> del in e.GetInvocationList())
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
            ObjectGroup m_Group;

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
            ToXml(writer, UUID.Zero, null, options);
        }
        public void ToXml(XmlTextWriter writer, UUID nextOwner, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            ToXml(writer, nextOwner, null, options);
        }
        public void ToXml(XmlTextWriter writer, UUID nextOwner, Vector3 offsetpos, XmlSerializationOptions options = XmlSerializationOptions.None)
        {
            List<ObjectPart> parts = Values;
            writer.WriteStartElement("SceneObjectGroup");
            if(offsetpos != null)
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
            RootPart.ToXml(writer, options);
            writer.WriteEndElement();
            writer.WriteStartElement("OtherParts");
            foreach (ObjectPart p in parts)
            {
                if(p.ID != RootPart.ID)
                {
                    p.ToXml(writer, nextOwner, options);
                }
            }
            writer.WriteEndElement();
            
#warning KeyframeMotion Base64
        }
        #endregion

        #region XML Deserialization
        static void fromXmlOtherParts(XmlTextReader reader, ObjectGroup group)
        {
            ObjectPart part = null;
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

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case "SceneObjectPart":
                                if (reader.IsEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                part = ObjectPart.FromXml(reader, null);
                                group.Add(part.LinkNumber, part.ID, part);
                                break;

                            default:
                                if (!reader.IsEmptyElement)
                                {
                                    reader.Skip();
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "OtherParts")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }

        public static ObjectGroup FromXml(XmlTextReader reader)
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
                        switch (reader.Name)
                        {
                            case "SceneObjectPart":
                                if (reader.IsEmptyElement)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                if (rootPart != null)
                                {
                                    throw new InvalidObjectXmlException();
                                }
                                rootPart = ObjectPart.FromXml(reader, group);
                                group.Add(rootPart.LinkNumber, rootPart.ID, rootPart);
                                break;

                            case "OtherParts":
                                if (reader.IsEmptyElement)
                                {
                                    break;
                                }
                                fromXmlOtherParts(reader, group);
                                break;

                            case "KeyframeMotion":
                                if (reader.IsEmptyElement)
                                {
                                    reader.Skip();
                                }
                                break;

                            default:
                                if (!reader.IsEmptyElement)
                                {
                                    reader.Skip();
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "SceneObjectGroup")
                        {
                            throw new InvalidObjectXmlException();
                        }
                        return group;

                    default:
                        break;
                }
            }
        }
        #endregion
    }
}
