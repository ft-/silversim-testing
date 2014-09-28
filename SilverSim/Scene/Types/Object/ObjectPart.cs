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

using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using ThreadedClasses;
using log4net;
using System.Reflection;
using System.Threading;

namespace SilverSim.Scene.Types.Object
{
    public class ObjectPart : IObject, IDisposable
    {
        private static readonly ILog m_Log = LogManager.GetLogger("OBJECT PART");

        #region Events
        public delegate void OnUpdateDelegate(ObjectPart part, int changed);
        public event OnUpdateDelegate OnUpdate;
        public event Action<IObject> OnPositionChange;
        #endregion

        public UInt32 LocalID { get; set; }

        #region Fields
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
        private bool m_IsSoundQueueing = false;
        private byte[] m_ParticleSystem = new byte[0];
        private TextureEntry m_TextureEntry = new TextureEntry();
        private byte[] m_TextureEntryBytes = new byte[0];
        private ReaderWriterLock m_TextureEntryLock = new ReaderWriterLock();
        private ReaderWriterLock m_ParticleSystemLock = new ReaderWriterLock();

        public int ScriptAccessPin = 0;

        public TextureEntry TextureEntry
        {
            get
            {
                m_TextureEntryLock.AcquireReaderLock(-1);
                try
                {
                    return new TextureEntry(m_TextureEntryBytes);
                }
                finally
                {
                    m_TextureEntryLock.ReleaseReaderLock();
                }
            }
            set
            {
                TextureEntry copy = new TextureEntry(value.GetBytes());
                m_TextureEntryLock.AcquireWriterLock(-1);
                try
                {
                    m_TextureEntry = value;
                    m_TextureEntryBytes = value.GetBytes();
                }
                finally
                {
                    m_TextureEntryLock.ReleaseWriterLock();
                }
            }
        }

        public byte[] TextureEntryBytes
        {
            get
            {
                m_TextureEntryLock.AcquireReaderLock(-1);
                try
                {
                    byte[] b = new byte[m_TextureEntryBytes.Length];
                    Buffer.BlockCopy(m_TextureEntryBytes, 0, b, 0, m_TextureEntryBytes.Length);
                    return b;
                }
                finally
                {
                    m_TextureEntryLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_TextureEntryLock.AcquireWriterLock(-1);
                try
                {
                    m_TextureEntryBytes = value;
                    m_TextureEntry = new TextureEntry(value);
                }
                finally
                {
                    m_TextureEntryLock.ReleaseWriterLock();
                }
            }
        }

        public ParticleSystem ParticleSystem
        {
            get
            {
                m_ParticleSystemLock.AcquireReaderLock(-1);
                try
                {
                    if(m_ParticleSystem.Length == 0)
                    {
                        return null;
                    }
                    return new ParticleSystem(m_ParticleSystem, 0);
                }
                finally
                {
                    m_ParticleSystemLock.ReleaseReaderLock();
                }
            }

            set
            {
                m_ParticleSystemLock.AcquireWriterLock(-1);
                try
                {
                    if (value == null)
                    {
                        m_ParticleSystem = new byte[0];
                    }
                    else
                    {
                        m_ParticleSystem = value.GetBytes();
                    }
                }
                finally
                {
                    m_ParticleSystemLock.ReleaseWriterLock();
                }
            }
        }

        public byte[] ParticleSystemBytes
        {
            get
            {
                m_ParticleSystemLock.AcquireReaderLock(-1);
                try
                {
                    byte[] o = new byte[m_ParticleSystem.Length];
                    Buffer.BlockCopy(m_ParticleSystem, 0, o, 0, m_ParticleSystem.Length);
                    return o;
                }
                finally
                {
                    m_ParticleSystemLock.ReleaseReaderLock();
                }
            }

            set
            {
                m_ParticleSystemLock.AcquireWriterLock(-1);
                try
                {
                    if (value == null)
                    {
                        m_ParticleSystem = new byte[0];
                    }
                    else
                    {
                        m_ParticleSystem = new byte[value.Length];
                        Buffer.BlockCopy(value, 0, m_ParticleSystem, 0, value.Length);
                    }
                }
                finally
                {
                    m_ParticleSystemLock.ReleaseWriterLock();
                }
            }
        }

        public class TextParam
        {
            #region Constructor
            public TextParam()
            {

            }
            #endregion

            #region Fields
            public string Text = string.Empty;
            public ColorAlpha TextColor = new ColorAlpha(0, 0, 0, 0);
            #endregion
        }
        private readonly TextParam m_Text = new TextParam();
        public class FlexibleParam
        {
            #region Constructor
            public FlexibleParam()
            {

            }
            #endregion

            #region Fields
            public bool IsFlexible = false;
            public int Softness = 0;
            public double Gravity = 0;
            public double Friction = 0;
            public double Wind = 0;
            public double Tension = 0;
            public Vector3 Force = Vector3.Zero;
            #endregion
        }
        private readonly FlexibleParam m_Flexible = new FlexibleParam();
        public class PointLightParam
        {
            #region Constructor
            public PointLightParam()
            {

            }
            #endregion

            #region Fields
            public bool IsLight = false;
            public Color LightColor = new Color();
            public double Intensity = 0;
            public double Radius = 0;
            public double Falloff = 0;
            #endregion
        }
        private readonly PointLightParam m_PointLight = new PointLightParam();
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
        private readonly OmegaParam m_Omega = new OmegaParam();
        public class PrimitiveShape
        {
            #region Constructor
            public PrimitiveShape()
            {
            }
            #endregion

            #region Fields
            public PrimitiveShapeType Type = PrimitiveShapeType.Box;
            public PrimitiveHoleShape HoleShape = PrimitiveHoleShape.Default;
            public Vector3 Cut = new Vector3(0, 1, 0);
            public double Hollow = 0f;
            public Vector3 Twist = Vector3.Zero;
            public Vector3 TopSize = Vector3.Zero;
            public Vector3 TopShear = Vector3.Zero;
            public Vector3 Dimple = Vector3.Zero;
            public Vector3 AdvancedCut = Vector3.Zero;
            public Vector3 Taper = Vector3.Zero;
            public Vector3 HoleSize = Vector3.Zero;
            public double Revolutions = 1f;
            public double RadiusOffset = 0f;
            public double Skew = 0f;
            public UUID SculptMap = UUID.Zero;
            public PrimitiveSculptType SculptType = PrimitiveSculptType.Sphere;
            public bool IsSculptInverted = false;
            public bool IsSculptMirrored = false;
            #endregion
        }
        private readonly PrimitiveShape m_Shape = new PrimitiveShape();
        public class CollisionSoundParam
        {
            #region Constructor
            public CollisionSoundParam()
            {

            }
            #endregion

            #region Fields
            public UUID ImpactSound = UUID.Zero;
            public double ImpactVolume = 0f;
            #endregion
        }
        private readonly CollisionSoundParam m_CollisionSound = new CollisionSoundParam();
        #endregion

        #region Constructor
        public ObjectPart()
        {
            Group = null;
            IsChanged = false;
            Inventory = new ObjectPartInventory();
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
        }
        #endregion

        private void TriggerOnUpdate(int flags)
        {
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
        }

        #region Properties
        public ObjectGroup Group { get; private set; }
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
                if(Group != null)
                {
                    return Group.Velocity;
                }
                else
                {
                    return Vector3.Zero;
                }
            }
            set
            {
                if(Group != null)
                {
                    Group.Velocity = value;
                }
            }
        }

        public Vector3 AngularVelocity
        {
            get
            {
                if (Group != null)
                {
                    return Group.AngularVelocity;
                }
                else
                {
                    return Vector3.Zero;
                }
            }
            set
            {
                if (Group != null)
                {
                    Group.AngularVelocity = value;
                }
            }
        }

        public Vector3 Acceleration
        {
            get
            {
                if(Group != null)
                {
                    return Group.Acceleration;
                }
                else
                {
                    return Vector3.Zero;
                }
            }
            set
            {
                if(Group != null)
                {
                    Group.Acceleration = value;
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
                ObjectGroup grp = Group;
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
                TriggerOnUpdate((int)ChangedEvent.ChangedFlags.AllowedDrop);
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
                if (Group != null)
                {
                    return Group.Owner;
                }
                return UUI.Unknown;
            }
            set
            {
                if(Group != null)
                {
                    Group.Owner = value;
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
                TriggerOnUpdate((int)ChangedEvent.ChangedFlags.Shape);
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

        public CollisionSoundParam CollisionSound
        {
            get
            {
                CollisionSoundParam res = new CollisionSoundParam();
                lock(m_CollisionSound)
                {
                    res.ImpactSound = m_CollisionSound.ImpactSound;
                    res.ImpactVolume = m_CollisionSound.ImpactVolume;
                }
                return res;
            }
            set
            {
                lock(m_CollisionSound)
                {
                    m_CollisionSound.ImpactSound = value.ImpactSound;
                    m_CollisionSound.ImpactVolume = value.ImpactVolume;
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
                TriggerOnUpdate((int)ChangedEvent.ChangedFlags.Scale);
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
                TriggerOnUpdate((int)ChangedEvent.ChangedFlags.Shape);
            }
        }

        public TextParam Text
        {
            get
            {
                TextParam res = new TextParam();
                lock (m_Text)
                {
                    res.Text = m_Text.Text;
                    res.TextColor = new ColorAlpha(m_Text.TextColor);
                }
                return res;
            }
            set
            {
                lock(m_Text)
                {
                    m_Text.Text = value.Text;
                    m_Text.TextColor = new ColorAlpha(value.TextColor);
                }
                IsChanged = true;
                TriggerOnUpdate(0);
            }
        }

        public FlexibleParam Flexible
        {
            get
            {
                FlexibleParam res = new FlexibleParam();
                lock(m_Flexible)
                {
                    res.Force = m_Flexible.Force;
                    res.Friction = m_Flexible.Friction;
                    res.Gravity = m_Flexible.Gravity;
                    res.IsFlexible = m_Flexible.IsFlexible;
                    res.Softness = m_Flexible.Softness;
                    res.Tension = m_Flexible.Tension;
                    res.Wind = m_Flexible.Wind;
                }
                return res;
            }
            set
            {
                lock (m_Flexible)
                {
                    m_Flexible.Force = value.Force;
                    m_Flexible.Friction = value.Friction;
                    m_Flexible.Gravity = value.Gravity;
                    m_Flexible.IsFlexible = value.IsFlexible;
                    m_Flexible.Softness = value.Softness;
                    m_Flexible.Tension = value.Tension;
                    m_Flexible.Wind = value.Wind;
                }
                IsChanged = true;
                TriggerOnUpdate((int)ChangedEvent.ChangedFlags.Shape);
            }
        }

        public PointLightParam PointLight
        {
            get
            {
                PointLightParam res = new PointLightParam();
                lock (m_PointLight)
                {
                    res.Falloff = m_PointLight.Falloff;
                    res.Intensity = m_PointLight.Intensity;
                    res.IsLight = m_PointLight.IsLight;
                    res.LightColor = new Color(m_PointLight.LightColor);
                    res.Radius = m_PointLight.Radius;
                }
                return res;
            }
            set
            {
                lock (m_PointLight)
                {
                    m_PointLight.Falloff = value.Falloff;
                    m_PointLight.Intensity = value.Intensity;
                    m_PointLight.IsLight = value.IsLight;
                    m_PointLight.LightColor = new Color(value.LightColor);
                    m_PointLight.Radius = value.Radius;
                }
                IsChanged = true;
                TriggerOnUpdate(0);
            }
        }

        public OmegaParam Omega
        {
            get
            {
                OmegaParam res = new OmegaParam();
                lock(m_Omega)
                {
                    res.Axis = m_Omega.Axis;
                    res.Gain = m_Omega.Gain;
                    res.Spinrate = m_Omega.Spinrate;
                }
                return res;
            }
            set
            {
                lock(m_Omega)
                {
                    m_Omega.Axis = value.Axis;
                    m_Omega.Gain = value.Gain;
                    m_Omega.Spinrate = value.Spinrate;
                }
                IsChanged = true;
                TriggerOnUpdate(0);
            }
        }

        public PrimitiveShape Shape
        {
            get
            {
                PrimitiveShape res = new PrimitiveShape();
                lock(m_Shape)
                {
                    res.Type = m_Shape.Type;
                    res.HoleShape = m_Shape.HoleShape;
                    res.Cut = m_Shape.Cut;
                    res.Hollow = m_Shape.Hollow;
                    res.Twist = m_Shape.Twist;
                    res.TopSize = m_Shape.TopSize;
                    res.TopShear = m_Shape.TopShear;
                    res.Dimple = m_Shape.Dimple;
                    res.AdvancedCut = m_Shape.AdvancedCut;
                    res.Taper = m_Shape.Taper;
                    res.Revolutions = m_Shape.Revolutions;
                    res.RadiusOffset = m_Shape.RadiusOffset;
                    res.Skew = m_Shape.Skew;
                    res.SculptMap = new UUID(m_Shape.SculptMap);
                    res.SculptType = m_Shape.SculptType;
                    res.IsSculptInverted = m_Shape.IsSculptInverted;
                    res.IsSculptMirrored = m_Shape.IsSculptMirrored;
                    res.HoleSize = m_Shape.HoleSize;
                }
                return res;
            }
            set
            {
                lock(m_Shape)
                {
                    m_Shape.Type = value.Type;
                    m_Shape.HoleShape = value.HoleShape;
                    m_Shape.Cut = value.Cut;
                    m_Shape.Hollow = value.Hollow;
                    m_Shape.Twist = value.Twist;
                    m_Shape.TopSize = value.TopSize;
                    m_Shape.TopShear = value.TopShear;
                    m_Shape.Dimple = value.Dimple;
                    m_Shape.AdvancedCut = value.AdvancedCut;
                    m_Shape.Taper = value.Taper;
                    m_Shape.Revolutions = value.Revolutions;
                    m_Shape.RadiusOffset = value.RadiusOffset;
                    m_Shape.Skew = value.Skew;
                    m_Shape.SculptMap = new UUID(value.SculptMap);
                    m_Shape.SculptType = value.SculptType;
                    m_Shape.IsSculptInverted = value.IsSculptInverted;
                    m_Shape.IsSculptMirrored = value.IsSculptMirrored;
                    m_Shape.HoleSize = value.HoleSize;
                }
                IsChanged = true;
                TriggerOnUpdate((int)ChangedEvent.ChangedFlags.Shape);
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
                    if(Group != null)
                    {
                        if(this != Group.RootPart)
                        {
                            return m_GlobalPosition - Group.RootPart.GlobalPosition;
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
                    if (Group != null)
                    {
                        if (this != Group.RootPart)
                        {
                            m_GlobalPosition = value + Group.RootPart.GlobalPosition;
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
                    if (Group != null)
                    {
                        if (this != Group.RootPart)
                        {
                            return m_GlobalPosition - Group.RootPart.GlobalPosition;
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
                    if (Group != null)
                    {
                        if (this != Group.RootPart)
                        {
                            m_GlobalPosition = value + Group.RootPart.GlobalPosition;
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
                    if (Group != null)
                    {
                        if (this != Group.RootPart)
                        {
                            return m_GlobalRotation / Group.RootPart.GlobalRotation;
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
                    if (Group != null)
                    {
                        if (this != Group.RootPart)
                        {
                            m_GlobalRotation = value * Group.RootPart.GlobalRotation;
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
                    if (Group != null)
                    {
                        if (this != Group.RootPart)
                        {
                            return m_GlobalRotation / Group.RootPart.GlobalRotation;
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
                    if (Group != null)
                    {
                        if (this != Group.RootPart)
                        {
                            m_GlobalRotation = value * Group.RootPart.GlobalRotation;
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
                if(Group != null)
                {
                    throw new ArgumentException();
                }
                Group = group;
            }
        }

        protected internal void UnlinkFromObjectGroup()
        {
            lock (this)
            {
                Group = null;
            }
        }
        #endregion

        #region Primitive Methods
        public void GetPrimitiveParams(AnArray.Enumerator enumerator, ref AnArray paramList)
        {
            switch(ParamsHelper.GetPrimParamType(enumerator))
            {
                case PrimitiveParamsType.Name:
                    paramList.Add(Name);
                    break;

                case PrimitiveParamsType.Desc:
                    paramList.Add(Description);
                    break;

                case PrimitiveParamsType.Type:
                    {
                        PrimitiveShape shape = Shape;
                        paramList.Add((int)shape.Type);
                        if(shape.Type == PrimitiveShapeType.Sculpt)
                        {
                            paramList.Add(shape.SculptMap);
                            int sculptFlags = (int)shape.SculptType;
                            if(shape.IsSculptInverted)
                            {
                                sculptFlags |= 0x40;
                            }
                            if(shape.IsSculptMirrored)
                            {
                                sculptFlags |= 0x80;
                            }
                            paramList.Add(sculptFlags);
                        }
                        else
                        {
                            paramList.Add((int)shape.HoleShape);
                            paramList.Add(shape.Cut);
                            paramList.Add(shape.Hollow);
                            paramList.Add(shape.Twist);
                            switch (shape.Type)
                            {
                                case PrimitiveShapeType.Box:
                                case PrimitiveShapeType.Cylinder:
                                case PrimitiveShapeType.Prism:
                                    paramList.Add(shape.TopSize);
                                    paramList.Add(shape.TopShear);
                                    break;

                                case PrimitiveShapeType.Sphere:
                                    paramList.Add(shape.Dimple);
                                    break;

                                case PrimitiveShapeType.Torus:
                                case PrimitiveShapeType.Tube:
                                case PrimitiveShapeType.Ring:
                                    paramList.Add(shape.HoleSize);
                                    paramList.Add(shape.TopShear);
                                    paramList.Add(shape.AdvancedCut);
                                    paramList.Add(shape.Taper);
                                    paramList.Add(shape.Revolutions);
                                    paramList.Add(shape.RadiusOffset);
                                    paramList.Add(shape.Skew);
                                    break;
                            }
                        }
                    }
                    break;

                case PrimitiveParamsType.Slice:
                    paramList.Add(Slice);
                    break;

                case PrimitiveParamsType.PhysicsShapeType:
                    paramList.Add((int)PhysicsShapeType);
                    break;

                case PrimitiveParamsType.Material:
                    paramList.Add((int)Material);
                    break;

                case PrimitiveParamsType.Position:
                    paramList.Add(Position);
                    break;

                case PrimitiveParamsType.PosLocal:
                    paramList.Add(LocalPosition);
                    break;

                case PrimitiveParamsType.Rotation:
                    paramList.Add(Rotation);
                    break;

                case PrimitiveParamsType.RotLocal:
                    paramList.Add(LocalRotation);
                    break;

                case PrimitiveParamsType.Size:
                    paramList.Add(Size);
                    break;

                case PrimitiveParamsType.Texture:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXTURE"));
                        foreach(TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, ref paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.Text:
                    {
                        TextParam text = Text;
                        paramList.Add(text.TextColor.AsVector3);
                        paramList.Add(text.TextColor.A);
                    }
                    break;

                case PrimitiveParamsType.Color:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_COLOR"));
                        foreach(TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Color, ref paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.BumpShiny:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY"));
                        foreach(TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.BumpShiny, ref paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.PointLight:
                    {
                        PointLightParam p = PointLight;
                        paramList.Add(p.IsLight);
                        paramList.Add(p.LightColor.AsVector3);
                        paramList.Add(p.Intensity);
                        paramList.Add(p.Radius);
                        paramList.Add(p.Falloff);
                    }
                    break;

                case PrimitiveParamsType.FullBright:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_FULLBRIGHT"));
                        foreach (TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, ref paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.Flexible:
                    {
                        FlexibleParam p = Flexible;
                        paramList.Add(p.IsFlexible);
                        paramList.Add(p.Softness);
                        paramList.Add(p.Gravity);
                        paramList.Add(p.Friction);
                        paramList.Add(p.Wind);
                        paramList.Add(p.Tension);
                        paramList.Add(p.Force);
                    }
                    break;

                case PrimitiveParamsType.TexGen:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXGEN"));
                        foreach(TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, ref paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.Glow:
                    m_TextureEntryLock.AcquireReaderLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_GLOW"));
                        foreach(TextureEntryFace face in faces)
                        {
                            GetTexPrimitiveParams(face, PrimitiveParamsType.Texture, ref paramList);
                        }
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseReaderLock();
                    }
                    break;

                case PrimitiveParamsType.Omega:
                    {
                        OmegaParam p = Omega;
                        paramList.Add(p.Axis);
                        paramList.Add(p.Spinrate);
                        paramList.Add(p.Gain);
                    }
                    break;

                default:
                    throw new ArgumentException(String.Format("Invalid primitive parameter type {0}", enumerator.Current.AsUInt));
            }
        }

        public const int ALL_SIDES = -1;


        public int NumberOfSides
        {
            get
            {
                int ret = 0;
                bool hasCut;
                bool hasHollow;
                bool hasDimple;
                bool hasProfileCut;

                PrimitiveShapeType primType = Shape.Type;
                if (primType == PrimitiveShapeType.Box
                    ||
                    primType == PrimitiveShapeType.Cylinder
                    ||
                    primType == PrimitiveShapeType.Prism)
                {

                    hasCut = (Shape.Cut.X > 0) || (Shape.Cut.Y < 1);
                }
                else
                {
                    hasCut = (Shape.Cut.X > 0) || (Shape.Cut.Y < 1);
                }

                hasHollow = Shape.Hollow > 0;
                hasDimple = (Shape.Cut.X > 0) || (Shape.Cut.Y < 1); // taken from llSetPrimitiveParms
                hasProfileCut = hasDimple; // is it the same thing?

                switch (primType)
                {
                    case PrimitiveShapeType.Box:
                        ret = 6;
                        if (hasCut) ret += 2;
                        if (hasHollow) ret += 1;
                        break;
                    case PrimitiveShapeType.Cylinder:
                        ret = 3;
                        if (hasCut) ret += 2;
                        if (hasHollow) ret += 1;
                        break;
                    case PrimitiveShapeType.Prism:
                        ret = 5;
                        if (hasCut) ret += 2;
                        if (hasHollow) ret += 1;
                        break;
                    case PrimitiveShapeType.Sphere:
                        ret = 1;
                        if (hasCut) ret += 2;
                        if (hasDimple) ret += 2;
                        if (hasHollow) ret += 1;
                        break;
                    case PrimitiveShapeType.Torus:
                        ret = 1;
                        if (hasCut) ret += 2;
                        if (hasProfileCut) ret += 2;
                        if (hasHollow) ret += 1;
                        break;
                    case PrimitiveShapeType.Tube:
                        ret = 4;
                        if (hasCut) ret += 2;
                        if (hasProfileCut) ret += 2;
                        if (hasHollow) ret += 1;
                        break;
                    case PrimitiveShapeType.Ring:
                        ret = 3;
                        if (hasCut) ret += 2;
                        if (hasProfileCut) ret += 2;
                        if (hasHollow) ret += 1;
                        break;
                    case PrimitiveShapeType.Sculpt:
                        // Special mesh handling
                        if (Shape.SculptType == PrimitiveSculptType.Mesh)
                            ret = 32; // if it's a mesh then max 32 faces
                        else
                            ret = 1; // if it's a sculpt then max 1 face
                        break;
                }

                return ret;
            }
        }

        public ICollection<TextureEntryFace> GetFaces(int face)
        {
            if(face  == ALL_SIDES)
            {
                List<TextureEntryFace> list = new List<TextureEntryFace>();
                for (int i = 0; i < NumberOfSides; ++i)
                {
                    list.Add(m_TextureEntry[(uint)face]);
                }
                return list;
            }
            else
            {
                List<TextureEntryFace> list = new List<TextureEntryFace>();
                list.Add(m_TextureEntry[(uint)face]);
                return list;
            }
        }

        public void SetPrimitiveParams(AnArray.MarkEnumerator enumerator)
        {
            switch (ParamsHelper.GetPrimParamType(enumerator))
            {
                case PrimitiveParamsType.Name:
                    Name = ParamsHelper.GetString(enumerator, "PRIM_NAME");
                    break;

                case PrimitiveParamsType.Desc:
                    Description = ParamsHelper.GetString(enumerator, "PRIM_DESC");
                    break;

                case PrimitiveParamsType.Type:
                    {
                        PrimitiveShape shape = Shape;
                        shape.Type = (PrimitiveShapeType)ParamsHelper.GetInteger(enumerator, "PRIM_TYPE");
                        if (shape.Type == PrimitiveShapeType.Sculpt)
                        {
                            shape.SculptMap = ParamsHelper.GetKey(enumerator, "PRIM_TYPE");
                            int sculptFlags = ParamsHelper.GetInteger(enumerator, "PRIM_TYPE");
                            shape.SculptType = (PrimitiveSculptType)(sculptFlags & 0x0F);
                            shape.IsSculptInverted = (sculptFlags & 0x40) != 0;
                            shape.IsSculptMirrored = (sculptFlags & 0x80) != 0;
                        }
                        else
                        {
                            shape.HoleShape = (PrimitiveHoleShape)ParamsHelper.GetInteger(enumerator, "PRIM_TYPE");
                            shape.Cut = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                            shape.Hollow = ParamsHelper.GetDouble(enumerator, "PRIM_TYPE");
                            shape.Twist = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                            switch (shape.Type)
                            {
                                case PrimitiveShapeType.Box:
                                case PrimitiveShapeType.Cylinder:
                                case PrimitiveShapeType.Prism:
                                    shape.TopSize = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                                    shape.TopShear = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                                    break;

                                case PrimitiveShapeType.Sphere:
                                    shape.Dimple = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                                    break;

                                case PrimitiveShapeType.Torus:
                                case PrimitiveShapeType.Tube:
                                case PrimitiveShapeType.Ring:
                                    shape.HoleSize = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                                    shape.TopShear = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                                    shape.AdvancedCut = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                                    shape.Taper = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                                    shape.Revolutions = ParamsHelper.GetDouble(enumerator, "PRIM_TYPE");
                                    shape.RadiusOffset = ParamsHelper.GetDouble(enumerator, "PRIM_TYPE");
                                    shape.Skew = ParamsHelper.GetDouble(enumerator, "PRIM_TYPE");
                                    break;

                                default:
                                    throw new ArgumentException(String.Format("Invalid primitive type {0}", shape.Type));
                            }
                            Shape = shape;
                        }
                    }
                    break;

                case PrimitiveParamsType.Slice:
                    Slice = ParamsHelper.GetVector(enumerator, "PRIM_SIZE");
                    break;

                case PrimitiveParamsType.PhysicsShapeType:
                    PhysicsShapeType = (PrimitivePhysicsShapeType) ParamsHelper.GetInteger(enumerator, "PRIM_PHYSICS_SHAPE_TYPE");
                    break;

                case PrimitiveParamsType.Material:
                    Material = (PrimitiveMaterial) ParamsHelper.GetInteger(enumerator, "PRIM_MATERIAL");
                    break;

                case PrimitiveParamsType.Position:
                    Position = ParamsHelper.GetVector(enumerator, "PRIM_POSITION");
                    break;

                case PrimitiveParamsType.PosLocal:
                    LocalPosition = ParamsHelper.GetVector(enumerator, "PRIM_POS_LOCAL");
                    break;

                case PrimitiveParamsType.Rotation:
                    Rotation = ParamsHelper.GetRotation(enumerator, "PRIM_ROTATION");
                    break;

                case PrimitiveParamsType.RotLocal:
                    LocalRotation = ParamsHelper.GetRotation(enumerator, "PRIM_ROT_LOCAL");
                    break;

                case PrimitiveParamsType.Size:
                    Size = ParamsHelper.GetVector(enumerator, "PRIM_SIZE");
                    break;

                case PrimitiveParamsType.Texture:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXTURE"));
                        enumerator.MarkPosition();
                        foreach(TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Texture, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.Text:
                    {
                        TextParam p = new TextParam();
                        p.Text = ParamsHelper.GetString(enumerator, "PRIM_TEXT");
                        Vector3 v = ParamsHelper.GetVector(enumerator, "PRIM_TEXT");
                        double alpha = ParamsHelper.GetDouble(enumerator, "PRIM_TEXT");
                        p.TextColor = new ColorAlpha(v, alpha);
                        Text = p;
                    }
                    break;

                case PrimitiveParamsType.Color:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXTURE"));
                        enumerator.MarkPosition();
                        foreach(TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Color, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.BumpShiny:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY"));
                        enumerator.MarkPosition();
                        foreach (TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.BumpShiny, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.PointLight:
                    {
                        PointLightParam p = new PointLightParam();
                        p.IsLight = ParamsHelper.GetBoolean(enumerator, "PRIM_POINT_LIGHT");
                        p.LightColor = new Color(ParamsHelper.GetVector(enumerator, "PRIM_POINT_LIGHT"));
                        p.Intensity = ParamsHelper.GetDouble(enumerator, "PRIM_POINT_LIGHT");
                        p.Radius = ParamsHelper.GetDouble(enumerator, "PRIM_POINT_LIGHT");
                        p.Falloff = ParamsHelper.GetDouble(enumerator, "PRIM_POINT_LIGHT");
                        PointLight = p;
                    }
                    break;

                case PrimitiveParamsType.FullBright:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_FULLBRIGHT"));
                        enumerator.MarkPosition();
                        foreach (TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.FullBright, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.Flexible:
                    {
                        FlexibleParam p = new FlexibleParam();
                        p.IsFlexible = ParamsHelper.GetBoolean(enumerator, "PRIM_FLEXIBLE");
                        p.Softness = ParamsHelper.GetInteger(enumerator, "PRIM_FLEXIBLE");
                        p.Gravity = ParamsHelper.GetDouble(enumerator, "PRIM_FLEXIBLE");
                        p.Friction = ParamsHelper.GetDouble(enumerator, "PRIM_FLEXIBLE");
                        p.Wind = ParamsHelper.GetDouble(enumerator, "PRIM_FLEXIBLE");
                        p.Force = ParamsHelper.GetVector(enumerator, "PRIM_FLEXIBLE");
                        Flexible = p;
                    }
                    break;

                case PrimitiveParamsType.TexGen:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXGEN"));
                        enumerator.MarkPosition();
                        foreach (TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.TexGen, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.Glow:
                    m_TextureEntryLock.AcquireWriterLock(-1);
                    try
                    {
                        ICollection<TextureEntryFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_GLOW"));
                        enumerator.MarkPosition();
                        foreach(TextureEntryFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            SetTexPrimitiveParams(face, PrimitiveParamsType.Texture, enumerator);
                        }
                        m_TextureEntryBytes = m_TextureEntry.GetBytes();
                    }
                    finally
                    {
                        m_TextureEntryLock.ReleaseWriterLock();
                    }
                    break;

                case PrimitiveParamsType.Omega:
                    {
                        OmegaParam p = new OmegaParam();
                        p.Axis = ParamsHelper.GetVector(enumerator, "PRIM_OMEGA");
                        p.Spinrate = ParamsHelper.GetDouble(enumerator, "PRIM_OMEGA");
                        p.Gain = ParamsHelper.GetDouble(enumerator, "PRIM_OMEGA");
                        Omega = p;
                    }
                    break;

                default:
                    throw new ArgumentException(String.Format("Invalid primitive parameter type {0}", enumerator.Current.AsInt));
            }
        }
        #endregion

        #region TextureEntryFace functions
        public void GetTexPrimitiveParams(TextureEntryFace face, PrimitiveParamsType type, ref AnArray paramList)
        {
            switch (type)
            {
                case PrimitiveParamsType.Texture:
                    paramList.Add(face.TextureID);
                    paramList.Add(new Vector3(face.RepeatU, face.RepeatV, 0));
                    paramList.Add(new Vector3(face.OffsetU, face.OffsetV, 0));
                    paramList.Add(face.Rotation);
                    break;

                case PrimitiveParamsType.Color:
                    paramList.Add(face.TextureColor.AsVector3);
                    paramList.Add(face.TextureColor.A);
                    break;

                case PrimitiveParamsType.BumpShiny:
                    paramList.Add((int)face.Shiny);
                    paramList.Add((int)face.Bump);
                    break;

                case PrimitiveParamsType.FullBright:
                    paramList.Add(face.FullBright);
                    break;

                case PrimitiveParamsType.TexGen:
                    paramList.Add((int)face.TexMapType);
                    break;

                case PrimitiveParamsType.Glow:
                    paramList.Add(face.Glow);
                    break;

                default:
                    throw new ArgumentException(String.Format("Internal error! Primitive parameter type {0} should not be passed to PrimitiveFace", type));
            }
        }

        public void SetTexPrimitiveParams(TextureEntryFace face, PrimitiveParamsType type, AnArray.MarkEnumerator enumerator)
        {
            switch (type)
            {
                case PrimitiveParamsType.Texture:
                    {
                        face.TextureID = ParamsHelper.GetString(enumerator, "PRIM_TEXTURE");
                        Vector3 v;
                        v = ParamsHelper.GetVector(enumerator, "PRIM_TEXTURE");
                        face.RepeatU = (float)v.X;
                        face.RepeatV = (float)v.Y;
                        v = ParamsHelper.GetVector(enumerator, "PRIM_TEXTURE");
                        face.OffsetU = (float)v.X;
                        face.OffsetV = (float)v.Y;
                        face.Rotation = (float)ParamsHelper.GetDouble(enumerator, "PRIM_TEXTURE");
                    }
                    break;

                case PrimitiveParamsType.Color:
                    {
                        Vector3 color = ParamsHelper.GetVector(enumerator, "PRIM_COLOR");
                        double alpha = ParamsHelper.GetDouble(enumerator, "PRIM_COLOR");
                        face.TextureColor = new ColorAlpha(color, alpha);
                    }
                    break;

                case PrimitiveParamsType.BumpShiny:
                    face.Shiny = (Shininess)ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY");
                    face.Bump = (Bumpiness)ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY");
                    break;

                case PrimitiveParamsType.FullBright:
                    face.FullBright = ParamsHelper.GetBoolean(enumerator, "PRIM_FULLBRIGHT");
                    break;

                case PrimitiveParamsType.TexGen:
                    face.TexMapType = (MappingType)ParamsHelper.GetInteger(enumerator, "PRIM_TEXGEN");
                    break;

                case PrimitiveParamsType.Glow:
                    face.Glow = (float)ParamsHelper.GetDouble(enumerator, "PRIM_GLOW");
                    break;

                default:
                    throw new ArgumentException(String.Format("Internal error! Primitive parameter type {0} should not be passed to PrimitiveFace", type));
            }
        }
        #endregion

        #region Object Details Methods
        public void GetObjectDetails(AnArray.Enumerator enumerator, ref AnArray paramList)
        {
            Group.GetObjectDetails(enumerator, ref paramList);
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
    }
}
