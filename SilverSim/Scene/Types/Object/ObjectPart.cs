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
using SilverSim.Types.Primitive;
using System;
using System.Collections.Generic;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Scene.Types.Object
{
    public class ObjectPart : IObject, IDisposable
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

        #region Fields
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
        private bool m_IsSoundQueueing = false;
        private byte[] m_ParticleSystem = new byte[0];
        private TextureEntry m_TextureEntry = new TextureEntry();
        private byte[] m_TextureEntryBytes = new byte[0];
        private ReaderWriterLock m_TextureEntryLock = new ReaderWriterLock();
        private ReaderWriterLock m_ParticleSystemLock = new ReaderWriterLock();
        private byte[] m_ExtraParamsBytes = new byte[0];
        private ReaderWriterLock m_ExtraParamsLock = new ReaderWriterLock();

        public int ScriptAccessPin = 0;

        private bool m_IsFacelightDisabled = false;
        private bool m_IsAttachmentLightsDisabled = false;

        private bool IsFacelightDisabled
        {
            get
            {
                return m_IsFacelightDisabled;
            }
            set
            {
                lock(this)
                {
                    m_IsFacelightDisabled = value;
                }
                UpdateExtraParams();
                TriggerOnUpdate(0);
            }
        }

        private bool IsAttachmentLightsDisabled
        {
            get
            {
                return m_IsAttachmentLightsDisabled;
            }
            set
            {
                lock (this)
                {
                    m_IsAttachmentLightsDisabled = value;
                }
                UpdateExtraParams();
                TriggerOnUpdate(0);
            }
        }

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
            public double Cutoff = 0;
            public double Falloff = 0;
            #endregion
        }
        private readonly PointLightParam m_PointLight = new PointLightParam();

        public class ProjectionParam
        {
            #region Constructor
            public ProjectionParam()
            {

            }
            #endregion

            #region Fields
            public bool IsProjecting = false;
            public UUID ProjectionTextureID = UUID.Zero;
            public double ProjectionFOV;
            public double ProjectionFocus;
            public double ProjectionAmbience;
            #endregion
        }

        private readonly ProjectionParam m_Projection = new ProjectionParam();
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

            public UUID SculptMap = UUID.Zero;
            public PrimitiveSculptType SculptType = PrimitiveSculptType.Sphere;
            public bool IsSculptInverted = false;
            public bool IsSculptMirrored = false;

            public void ToPrimitiveParams(AnArray paramList)
            {
                paramList.Add((int)Type);
                if (Type == PrimitiveShapeType.Sculpt)
                {
                    paramList.Add(SculptMap);
                    int sculptFlags = (int)SculptType;
                    if (IsSculptInverted)
                    {
                        sculptFlags |= 0x40;
                    }
                    if (IsSculptMirrored)
                    {
                        sculptFlags |= 0x80;
                    }
                    paramList.Add(sculptFlags);
                }
                else
                {
#if OLD
                    paramList.Add((int)HoleShape);
                    paramList.Add(Cut);
                    paramList.Add(Hollow);
                    paramList.Add(Twist);
                    switch (Type)
                    {
                        case PrimitiveShapeType.Box:
                        case PrimitiveShapeType.Cylinder:
                        case PrimitiveShapeType.Prism:
                            paramList.Add(TopSize);
                            paramList.Add(TopShear);
                            break;

                        case PrimitiveShapeType.Sphere:
                            paramList.Add(Dimple);
                            break;

                        case PrimitiveShapeType.Torus:
                        case PrimitiveShapeType.Tube:
                        case PrimitiveShapeType.Ring:
                            paramList.Add(HoleSize);
                            paramList.Add(TopShear);
                            paramList.Add(AdvancedCut);
                            paramList.Add(Taper);
                            paramList.Add(Revolutions);
                            paramList.Add(RadiusOffset);
                            paramList.Add(Skew);
                            break;
                    }
#endif
                }
            }

            public static PrimitiveShape FromPrimitiveParams(AnArray.MarkEnumerator enumerator)
            {
                PrimitiveShape shape = new PrimitiveShape();
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
                    PrimitiveHoleShape holeShape = (PrimitiveHoleShape)ParamsHelper.GetInteger(enumerator, "PRIM_TYPE");
                    if(holeShape != PrimitiveHoleShape.Circle &&
                        holeShape != PrimitiveHoleShape.Default &&
                        holeShape != PrimitiveHoleShape.Square &&
                        holeShape != PrimitiveHoleShape.Triangle)
                    {
                        holeShape = PrimitiveHoleShape.Default;
                    }
                    PrimitiveProfileShape profileShape = PrimitiveProfileShape.Circle;
                    PrimitiveExtrusion extrusion = PrimitiveExtrusion.Straight;
                    switch (shape.Type)
                    {
                        case PrimitiveShapeType.Box:
                            profileShape = PrimitiveProfileShape.Square;
                            extrusion = PrimitiveExtrusion.Straight;
                            break;

                        case PrimitiveShapeType.Cylinder:
                            profileShape = PrimitiveProfileShape.Circle;
                            extrusion = PrimitiveExtrusion.Straight;
                            break;

                        case PrimitiveShapeType.Prism:
                            profileShape = PrimitiveProfileShape.EquilateralTriangle;
                            extrusion = PrimitiveExtrusion.Straight;
                            break;

                        case PrimitiveShapeType.Sphere:
                            profileShape = PrimitiveProfileShape.HalfCircle;
                            extrusion = PrimitiveExtrusion.Curve1;
                            break;

                        case PrimitiveShapeType.Torus:
                            profileShape = PrimitiveProfileShape.Circle;
                            extrusion = PrimitiveExtrusion.Curve1;
                            break;

                        case PrimitiveShapeType.Tube:
                            profileShape = PrimitiveProfileShape.Square;
                            extrusion = PrimitiveExtrusion.Curve1;
                            break;

                        case PrimitiveShapeType.Ring:
                            profileShape = PrimitiveProfileShape.EquilateralTriangle;
                            extrusion = PrimitiveExtrusion.Curve1;
                            break;

                        case PrimitiveShapeType.Sculpt:
                            extrusion = PrimitiveExtrusion.Curve1;
                            break;
                    }

                    shape.PathCurve = (byte)extrusion;
                    shape.ProfileCurve = (byte)holeShape;
                    shape.ProfileCurve |= (byte)profileShape;
                    Vector3 cut = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                    double hollow = ParamsHelper.GetDouble(enumerator, "PRIM_TYPE");
                    Vector3 twist = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");

                    if (cut.X < 0f)
                    {
                        cut.X = 0f;
                    }
                    if (cut.X > 1f)
                    {
                        cut.X = 1f;
                    }
                    if (cut.Y < 0f)
                    {
                        cut.Y = 0f;
                    }
                    if (cut.Y > 1f)
                    {
                        cut.Y = 1f;
                    }
                    if (cut.Y - cut.X < 0.05f)
                    {
                        cut.Y = cut.Y - 0.05f;
                        if (cut.X < 0.0f)
                        {
                            cut.X = 0.0f;
                            cut.Y = 0.05f;
                        }
                    }
                    shape.ProfileBegin = (ushort)(50000 * cut.X);
                    shape.ProfileEnd = (ushort)(50000 * (1 - cut.Y));

                    if (hollow < 0f)
                    {
                        hollow = 0f;
                    }
                    // If the prim is a Cylinder, Prism, Sphere, Torus or Ring (or not a
                    // Box or Tube) and the hole shape is a square, hollow is limited to
                    // a max of 70%. The viewer performs its own check on this value but
                    // we need to do it here also so llGetPrimitiveParams can have access
                    // to the correct value.
                    if (profileShape != PrimitiveProfileShape.Square &&
                        holeShape == PrimitiveHoleShape.Square)
                    {
                        if (hollow > 0.70f)
                        {
                            hollow = 0.70f;
                        }
                    }
                    // Otherwise, hollow is limited to 95%.
                    else
                    {
                        if (hollow > 0.95f)
                        {
                            hollow = 0.95f;
                        }
                    }
                    shape.ProfileHollow = (ushort)(50000 * hollow);
                    if (twist.X < -1.0f)
                    {
                        twist.X = -1.0f;
                    }
                    if (twist.X > 1.0f)
                    {
                        twist.X = 1.0f;
                    }
                    if (twist.Y < -1.0f)
                    {
                        twist.Y = -1.0f;
                    }
                    if (twist.Y > 1.0f)
                    {
                        twist.Y = 1.0f;
                    }

                    double tempFloat = (100.0d * twist.X);
                    shape.PathTwistBegin = (sbyte)tempFloat;
                    tempFloat = (100.0d * twist.Y);
                    shape.PathTwist = (sbyte)tempFloat;

                    Vector3 topSize;
                    Vector3 topShear;
                    Vector3 holeSize;
                    Vector3 dimple;

                    switch (shape.Type)
                    {
                        case PrimitiveShapeType.Box:
                        case PrimitiveShapeType.Cylinder:
                        case PrimitiveShapeType.Prism:
                            topSize = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                            topShear = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");

                            if (topSize.X < 0f)
                            {
                                topSize.X = 0f;
                            }
                            if (topSize.X > 2f)
                            {
                                topSize.X = 2f;
                            }
                            if (topSize.Y < 0f)
                            {
                                topSize.Y = 0f;
                            }
                            if (topSize.Y > 2f)
                            {
                                topSize.Y = 2f;
                            }
                            tempFloat = (float)(100.0d * (2.0d - topSize.X));
                            shape.PathScaleX = (byte)tempFloat;
                            tempFloat = (float)(100.0d * (2.0d - topSize.Y));
                            shape.PathScaleY = (byte)tempFloat;
                            if (topShear.X < -0.5f)
                            {
                                topShear.X = -0.5f;
                            }
                            if (topShear.X > 0.5f)
                            {
                                topShear.X = 0.5f;
                            }
                            if (topShear.Y < -0.5f)
                            {
                                topShear.Y = -0.5f;
                            }
                            if (topShear.Y > 0.5f)
                            {
                                topShear.Y = 0.5f;
                            }
                            tempFloat = (float)(100.0d * topShear.X);
                            shape.PathShearX = (byte)tempFloat;
                            tempFloat = (float)(100.0d * topShear.Y);
                            shape.PathShearY = (byte)tempFloat;
                            break;

                        case PrimitiveShapeType.Sphere:
                            dimple = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");

                            // profile/path swapped for a sphere
                            shape.PathBegin = shape.ProfileBegin;
                            shape.PathEnd = shape.ProfileEnd;

                            shape.PathScaleX = 100;
                            shape.PathScaleY = 100;

                            if (dimple.X < 0f)
                            {
                                dimple.X = 0f;
                            }
                            if (dimple.X > 1f)
                            {
                                dimple.X = 1f;
                            }
                            if (dimple.Y < 0f)
                            {
                                dimple.Y = 0f;
                            }
                            if (dimple.Y > 1f)
                            {
                                dimple.Y = 1f;
                            }
                            if (dimple.Y - cut.X < 0.05f)
                            {
                                dimple.X = cut.Y - 0.05f;
                            }
                            shape.ProfileBegin = (ushort)(50000 * dimple.X);
                            shape.ProfileEnd = (ushort)(50000 * (1 - dimple.Y));
                            break;

                        case PrimitiveShapeType.Torus:
                        case PrimitiveShapeType.Tube:
                        case PrimitiveShapeType.Ring:
                            holeSize = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                            topShear = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                            Vector3 advancedCut = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                            Vector3 taper = ParamsHelper.GetVector(enumerator, "PRIM_TYPE");
                            double revolutions = ParamsHelper.GetDouble(enumerator, "PRIM_TYPE");
                            double radiusOffset = ParamsHelper.GetDouble(enumerator, "PRIM_TYPE");
                            double skew = ParamsHelper.GetDouble(enumerator, "PRIM_TYPE");

                            // profile/path swapped for a torrus, tube, ring
                            shape.PathBegin = shape.ProfileBegin;
                            shape.PathEnd = shape.ProfileEnd;

                            if (holeSize.X < 0.05f)
                            {
                                holeSize.X = 0.05f;
                            }
                            if (holeSize.X > 1f)
                            {
                                holeSize.X = 1f;
                            }
                            if (holeSize.Y < 0.05f)
                            {
                                holeSize.Y = 0.05f;
                            }
                            if (holeSize.Y > 0.5f)
                            {
                                holeSize.Y = 0.5f;
                            }
                            tempFloat = (float)(100.0d * (2.0d - holeSize.X));
                            shape.PathScaleX = (byte)tempFloat;
                            tempFloat = (float)(100.0d * (2.0d - holeSize.Y));
                            shape.PathScaleY = (byte)tempFloat;
                            if (topShear.X < -0.5f)
                            {
                                topShear.X = -0.5f;
                            }
                            if (topShear.X > 0.5f)
                            {
                                topShear.X = 0.5f;
                            }
                            if (topShear.Y < -0.5f)
                            {
                                topShear.Y = -0.5f;
                            }
                            if (topShear.Y > 0.5f)
                            {
                                topShear.Y = 0.5f;
                            }
                            tempFloat = (float)(100.0d * topShear.X);
                            shape.PathShearX = (byte)tempFloat;
                            tempFloat = (float)(100.0d * topShear.Y);
                            shape.PathShearY = (byte)tempFloat;
                            if (advancedCut.X < 0f)
                            {
                                advancedCut.X = 0f;
                            }
                            if (advancedCut.X > 1f)
                            {
                                advancedCut.X = 1f;
                            }
                            if (advancedCut.Y < 0f)
                            {
                                advancedCut.Y = 0f;
                            }
                            if (advancedCut.Y > 1f)
                            {
                                advancedCut.Y = 1f;
                            }
                            if (advancedCut.Y - advancedCut.X < 0.05f)
                            {
                                advancedCut.X = advancedCut.Y - 0.05f;
                                if (advancedCut.X < 0.0f)
                                {
                                    advancedCut.X = 0.0f;
                                    advancedCut.Y = 0.05f;
                                }
                            }
                            shape.ProfileBegin = (ushort)(50000 * advancedCut.X);
                            shape.ProfileEnd = (ushort)(50000 * (1 - advancedCut.Y));
                            if (taper.X < -1f)
                            {
                                taper.X = -1f;
                            }
                            if (taper.X > 1f)
                            {
                                taper.X = 1f;
                            }
                            if (taper.Y < -1f)
                            {
                                taper.Y = -1f;
                            }
                            if (taper.Y > 1f)
                            {
                                taper.Y = 1f;
                            }
                            tempFloat = (float)(100.0d * taper.X);
                            shape.PathTaperX = (sbyte)tempFloat;
                            tempFloat = (float)(100.0d * taper.Y);
                            shape.PathTaperY = (sbyte)tempFloat;
                            if (revolutions < 1f)
                            {
                                revolutions = 1f;
                            }
                            if (revolutions > 4f)
                            {
                                revolutions = 4f;
                            }
                            tempFloat = 66.66667f * (revolutions - 1.0f);
                            shape.PathRevolutions = (byte)tempFloat;
                            // limits on radiusoffset depend on revolutions and hole size (how?) seems like the maximum range is 0 to 1
                            if (radiusOffset < 0f)
                            {
                                radiusOffset = 0f;
                            }
                            if (radiusOffset > 1f)
                            {
                                radiusOffset = 1f;
                            }
                            tempFloat = 100.0f * radiusOffset;
                            shape.PathRadiusOffset = (sbyte)tempFloat;
                            if (skew < -0.95f)
                            {
                                skew = -0.95f;
                            }
                            if (skew > 0.95f)
                            {
                                skew = 0.95f;
                            }
                            tempFloat = 100.0f * skew;
                            shape.PathSkew = (sbyte)tempFloat;
                            break;

                        default:
                            throw new ArgumentException(String.Format("Invalid primitive type {0}", shape.Type));
                    }
                }

                return shape;
            }

            public int NumberOfSides
            {
                get
                {
                    int ret = 0;
                    bool hasCut;
                    bool hasHollow;
                    bool hasDimple;
                    bool hasProfileCut;

                    PrimitiveShapeType primType = Type;
                    if (primType == PrimitiveShapeType.Box
                        ||
                        primType == PrimitiveShapeType.Cylinder
                        ||
                        primType == PrimitiveShapeType.Prism)
                    {

                        hasCut = (ProfileBegin > 0) || (ProfileEnd > 0);
                    }
                    else
                    {
                        hasCut = (PathBegin > 0) || (PathEnd > 0);
                    }

                    hasHollow = ProfileHollow > 0;
                    hasDimple = (ProfileBegin > 0) || (ProfileEnd > 0); // taken from llSetPrimitiveParms
                    hasProfileCut = hasDimple; // is it the same thing?

                    switch (primType)
                    {
                        case PrimitiveShapeType.Box:
                            ret = 6;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret += 1;
                            }
                            break;
                        case PrimitiveShapeType.Cylinder:
                            ret = 3;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret += 1;
                            }
                            break;
                        case PrimitiveShapeType.Prism:
                            ret = 5;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret += 1;
                            }
                            break;
                        case PrimitiveShapeType.Sphere:
                            ret = 1;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasDimple)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret += 1;
                            }
                            break;
                        case PrimitiveShapeType.Torus:
                            ret = 1;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasProfileCut)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret += 1;
                            }
                            break;
                        case PrimitiveShapeType.Tube:
                            ret = 4;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasProfileCut)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret += 1;
                            }
                            break;
                        case PrimitiveShapeType.Ring:
                            ret = 3;
                            if (hasCut)
                            {
                                ret += 2;
                            }
                            if (hasProfileCut)
                            {
                                ret += 2;
                            }
                            if (hasHollow)
                            {
                                ret += 1;
                            }
                            break;
                        case PrimitiveShapeType.Sculpt:
                            // Special mesh handling
                            if (SculptType == PrimitiveSculptType.Mesh)
                            {
                                ret = 32; // if it's a mesh then max 32 faces
                            }
                            else
                            {
                                ret = 1; // if it's a sculpt then max 1 face
                            }
                            break;
                    }

                    return ret;
                }
            }
            public ushort PathBegin;
            public byte PathCurve;
            public ushort PathEnd;
            public sbyte PathRadiusOffset;
            public byte PathRevolutions;
            public byte PathScaleX;
            public byte PathScaleY;
            public byte PathShearX;
            public byte PathShearY;
            public sbyte PathSkew;
            public sbyte PathTaperX;
            public sbyte PathTaperY;
            public sbyte PathTwist;
            public sbyte PathTwistBegin;
            public ushort ProfileBegin;
            public byte ProfileCurve;
            public ushort ProfileEnd;
            public ushort ProfileHollow;

            public void CopyFrom(PrimitiveShape shape)
            {
                Type = shape.Type;
                SculptMap = shape.SculptMap;
                SculptType = shape.SculptType;
                IsSculptInverted = shape.IsSculptInverted;
                IsSculptMirrored = shape.IsSculptMirrored;

                PathBegin = shape.PathBegin;
                PathCurve = shape.PathCurve;
                PathEnd = shape.PathEnd;
                PathRadiusOffset = shape.PathRadiusOffset;
                PathRevolutions = shape.PathRevolutions;
                PathScaleX = shape.PathScaleX;
                PathScaleY = shape.PathScaleY;
                PathShearX = shape.PathShearX;
                PathShearY = shape.PathShearY;
                PathSkew = shape.PathSkew;
                PathTaperX = shape.PathTaperX;
                PathTaperY = shape.PathTaperY;
                PathTwist = shape.PathTwist;
                PathTwistBegin = shape.PathTwistBegin;
                ProfileBegin = shape.ProfileBegin;
                ProfileCurve = shape.ProfileCurve;
                ProfileEnd = shape.ProfileEnd;
                ProfileHollow = shape.ProfileHollow;
                ProfileEnd = shape.ProfileEnd;
            }

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
            m_ObjectUpdateInfo = new ObjectUpdateInfo(this);
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            m_ObjectUpdateInfo.KillObject();
            Group.Scene.ScheduleUpdate(m_ObjectUpdateInfo);
        }
        #endregion

        public void SendKillObject()
        {
            m_ObjectUpdateInfo.KillObject();
            Group.Scene.ScheduleUpdate(m_ObjectUpdateInfo);
        }

        public void SendObjectUpdate()
        {
            Group.Scene.ScheduleUpdate(m_ObjectUpdateInfo);
        }

        internal void TriggerOnUpdate(ChangedEvent.ChangedFlags flags)
        {
            Group.OriginalAssetID = UUID.Zero;

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
            Group.Scene.ScheduleUpdate(m_ObjectUpdateInfo);
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
            Group.Scene.ScheduleUpdate(m_ObjectUpdateInfo);
        }

        public AssetServiceInterface AssetService /* specific for attachments usage */
        {
            get
            {
                return Group.AssetService;
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

        private void Float2LEBytes(float v, byte[] b, int offset)
        {
            byte[] i = BitConverter.GetBytes(v);
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(i);
            }
            Buffer.BlockCopy(i, 0, b, offset, 4);
        }

        private float LEBytes2Float(byte[] b, int offset)
        {
            if (!BitConverter.IsLittleEndian)
            {
                byte[] i = new byte[4];
                Buffer.BlockCopy(b, offset, i, 0, 4);
                Array.Reverse(i);
                return BitConverter.ToSingle(i, 0);
            }
            else
            {
                return BitConverter.ToSingle(b, offset);
            }
        }

        public byte[] ExtraParamsBytes
        {
            get
            {
                m_ExtraParamsLock.AcquireReaderLock(-1);
                try
                {
                    byte[] b = new byte[m_ExtraParamsBytes.Length];
                    Buffer.BlockCopy(m_ExtraParamsBytes, 0, b, 0, m_ExtraParamsBytes.Length);
                    return b;
                }
                finally
                {
                    m_ExtraParamsLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_ExtraParamsLock.AcquireWriterLock(-1);
                try
                {
                    m_ExtraParamsBytes = value;
                    PointLightParam light = new PointLightParam();
                    ProjectionParam proj = new ProjectionParam();
                    FlexibleParam flexi = new FlexibleParam();

                    flexi.IsFlexible = false;
                    proj.IsProjecting = false;
                    light.IsLight = false;
                    bool isSculpt = false;

                    if(value.Length < 1)
                    {
                        m_ExtraParamsBytes = new byte[1];
                        m_ExtraParamsBytes[0] = 0;
                        Flexible = flexi;
                        Projection = proj;
                        PointLight = light;
                        PrimitiveShape shape = Shape;
                        if(shape.Type == PrimitiveShapeType.Sculpt)
                        {
                            shape.Type = PrimitiveShapeType.Sphere;
                            shape.SculptType = PrimitiveSculptType.Sphere;
                            Shape = shape;
                        }
                    }
                    else
                    {
                        const ushort FlexiEP = 0x10;
                        const ushort LightEP = 0x20;
                        const ushort SculptEP = 0x30;
                        const ushort ProjectionEP = 0x40;

                        int paramCount = value[0];
                        int pos = 0;
                        for (int paramIdx = 0; paramIdx < paramCount; ++paramIdx )
                        {
                            if (pos + 6 > value.Length)
                            {
                                break;
                            }
                            ushort type = (ushort)(value[pos] | (value[pos + 1] << 8));
                            UInt32 len = (UInt32)(value[pos + 2] | (value[pos + 3] << 8) | (value[pos + 4] << 16) | (value[pos + 5] << 24));
                            pos += 6;

                            if (pos + len > value.Length)
                            {
                                break;
                            }

                            switch (type)
                            {
                                case FlexiEP:
                                    if (len < 16)
                                    {
                                        break;
                                    }
                                    flexi.IsFlexible = true;
                                    flexi.Softness = ((value[pos] & 0x80) >> 6) | ((value[pos + 1] & 0x80) >> 7);
                                    flexi.Tension = (float)(value[pos++] & 0x7F) / 10.0f;
                                    flexi.Friction = (float)(value[pos++] & 0x7F) / 10.0f;
                                    flexi.Gravity = (float)(value[pos++] / 10.0f) - 10.0f;
                                    flexi.Wind = (float)value[pos++] / 10.0f;
                                    flexi.Force.FromBytes(value, pos);
                                    pos += 12;
                                    break;

                                case LightEP:
                                    if (len < 16)
                                    {
                                        break;
                                    }
                                    light.IsLight = true;
                                    light.LightColor.R_AsByte = value[pos++];
                                    light.LightColor.G_AsByte = value[pos++];
                                    light.LightColor.B_AsByte = value[pos++];
                                    light.Intensity = value[pos++] / 255f;
                                    light.Radius = LEBytes2Float(value, pos);
                                    pos += 4;
                                    light.Cutoff = LEBytes2Float(value, pos);
                                    pos += 4;
                                    light.Falloff = LEBytes2Float(value, pos);
                                    pos += 4;
                                    break;

                                case SculptEP:
                                    if (len < 17)
                                    {
                                        break;
                                    }
                                    lock (m_Shape)
                                    {
                                        m_Shape.SculptMap.FromBytes(value, pos);
                                        m_Shape.SculptType = (PrimitiveSculptType)value[pos + 16];
                                    }
                                    pos += 17;
                                    isSculpt = true;
                                    break;

                                case ProjectionEP:
                                    if (len < 28)
                                    {
                                        break;
                                    }
                                    proj.IsProjecting = true;
                                    proj.ProjectionTextureID.FromBytes(value, pos);
                                    pos += 16;
                                    proj.ProjectionFOV = LEBytes2Float(value, pos);
                                    pos += 4;
                                    proj.ProjectionFocus = LEBytes2Float(value, pos);
                                    pos += 4;
                                    proj.ProjectionAmbience = LEBytes2Float(value, pos);
                                    pos += 4;
                                    break;
                            }
                        }
                    }

                    Projection = proj;
                    PointLight = light;
                    Flexible = flexi;

                    if(!isSculpt)
                    {
                        lock(m_Shape)
                        {
                            if(m_Shape.SculptType < PrimitiveSculptType.Sphere || m_Shape.SculptType > PrimitiveSculptType.Sphere)
                            {
                                m_Shape.SculptType = PrimitiveSculptType.Sphere;
                                m_Shape.Type = PrimitiveShapeType.Sphere;
                            }
                        }
                    }
                }
                finally
                {
                    m_ExtraParamsLock.ReleaseWriterLock();
                }
            }
        }

        private void UpdateExtraParams()
        {
            const ushort FlexiEP = 0x10;
            const ushort LightEP = 0x20;
            const ushort SculptEP = 0x30;
            const ushort ProjectionEP = 0x40;

            m_ExtraParamsLock.AcquireWriterLock(-1);
            try
            {
                int i = 0;
                uint totalBytesLength = 1;
                uint extraParamsNum = 0;

                FlexibleParam flexi = Flexible;
                PointLightParam light = PointLight;
                ProjectionParam proj = Projection;
                PrimitiveShape shape = Shape;
                if(flexi.IsFlexible)
                {
                    ++extraParamsNum;
                    totalBytesLength += 16;
                    totalBytesLength += 2 + 4;
                }

                if(shape.Type == PrimitiveShapeType.Sculpt)
                {
                    ++extraParamsNum;
                    totalBytesLength += 17;
                    totalBytesLength += 2 + 4;
                }

                if(light.IsLight)
                {
                    ++extraParamsNum;
                    totalBytesLength += 16;
                    totalBytesLength += 2 + 4;
                }

                if(proj.IsProjecting)
                {
                    ++extraParamsNum;
                    totalBytesLength += 28;
                    totalBytesLength += 2 + 4;
                }

                byte[] updatebytes = new byte[totalBytesLength];
                updatebytes[i++] = (byte)extraParamsNum;

                if(flexi.IsFlexible)
                {
                    updatebytes[i++] = (byte)(FlexiEP % 256);
                    updatebytes[i++] = (byte)(FlexiEP / 256);

                    updatebytes[i++] = 16;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;

                    updatebytes[i++] = (byte)((byte)((byte)(flexi.Tension * 10.01f) & 0x7F) | (byte)((flexi.Softness & 2) << 6));
                    updatebytes[i++] = (byte)((byte)((byte)(flexi.Friction * 10.01f) & 0x7F) | (byte)((flexi.Softness & 1) << 7));
                    updatebytes[i++] = (byte)((flexi.Gravity + 10.0f) * 10.01f);
                    updatebytes[i++] = (byte)(flexi.Wind * 10.01f);
                    flexi.Force.ToBytes(updatebytes, i);
                    i += 16;
                }

                if (shape.Type == PrimitiveShapeType.Sculpt)
                {
                    updatebytes[i++] = (byte)(SculptEP % 256);
                    updatebytes[i++] = (byte)(SculptEP / 256);
                    updatebytes[i++] = 17;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    shape.SculptMap.ToBytes(updatebytes, i);
                    i += 16;
                    updatebytes[i++] = (byte) shape.SculptType;
                }

                if (light.IsLight &&
                    (!m_IsAttachmentLightsDisabled || Group.AttachPoint != SilverSim.Types.Agent.AttachmentPoint.NotAttached) &&
                    (!m_IsFacelightDisabled || (Group.AttachPoint != SilverSim.Types.Agent.AttachmentPoint.LeftHand && Group.AttachPoint != SilverSim.Types.Agent.AttachmentPoint.RightHand)))
                {
                    updatebytes[i++] = (byte)(LightEP % 256);
                    updatebytes[i++] = (byte)(LightEP / 256);
                    updatebytes[i++] = 16;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    Buffer.BlockCopy(light.LightColor.AsByte, 0, updatebytes, i, 4);
                    updatebytes[i + 3] = (byte)(light.Intensity * 255f);
                    i += 4;
                    Float2LEBytes((float)light.Radius, updatebytes, i);
                    i += 4;
                    Float2LEBytes((float)light.Cutoff, updatebytes, i);
                    i += 4;
                    Float2LEBytes((float)light.Falloff, updatebytes, i);
                    i += 4;
                }

                if(proj.IsProjecting)
                {
                    updatebytes[i++] = (byte)(ProjectionEP % 256);
                    updatebytes[i++] = (byte)(ProjectionEP / 256);
                    updatebytes[i++] = 28;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    updatebytes[i++] = 0;
                    proj.ProjectionTextureID.ToBytes(updatebytes, i);
                    i += 16;
                    Float2LEBytes((float)proj.ProjectionFOV, updatebytes, i);
                    i += 4;
                    Float2LEBytes((float)proj.ProjectionFocus, updatebytes, i);
                    i += 4;
                    Float2LEBytes((float)proj.ProjectionAmbience, updatebytes, i);
                    i += 4;
                }
            }
            finally
            {
                m_ExtraParamsLock.ReleaseWriterLock();
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
                UpdateExtraParams();
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
                UpdateExtraParams();
                IsChanged = true;
                TriggerOnUpdate(ChangedEvent.ChangedFlags.Shape);
            }
        }

        public ProjectionParam Projection
        {
            get
            {
                ProjectionParam res = new ProjectionParam();
                lock(m_Projection)
                {
                    res.IsProjecting = m_Projection.IsProjecting;
                    res.ProjectionTextureID = m_Projection.ProjectionTextureID;
                    res.ProjectionFocus = m_Projection.ProjectionFocus;
                    res.ProjectionFOV = m_Projection.ProjectionFOV;
                    res.ProjectionAmbience = m_Projection.ProjectionAmbience;
                }
                return res;
            }
            set
            {
                lock(m_Projection)
                {
                    m_Projection.IsProjecting = value.IsProjecting;
                    m_Projection.ProjectionTextureID = value.ProjectionTextureID;
                    m_Projection.ProjectionFocus = value.ProjectionFocus;
                    m_Projection.ProjectionFOV = value.ProjectionFOV;
                    m_Projection.ProjectionAmbience = value.ProjectionAmbience;
                }
                UpdateExtraParams();
                IsChanged = true;
                TriggerOnUpdate(0);
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
                UpdateExtraParams();
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
                    res.CopyFrom(m_Shape);
                }
                return res;
            }
            set
            {
                bool sculptChanged = false;
                lock(m_Shape)
                {
                    if(m_Shape.SculptMap != value.SculptMap || m_Shape.SculptType != value.SculptType)
                    {
                        sculptChanged = true;
                    }
                    m_Shape.CopyFrom(value);
                }
                if(sculptChanged)
                {
                    UpdateExtraParams();
                }
                IsChanged = true;
                TriggerOnUpdate(ChangedEvent.ChangedFlags.Shape);
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
                    Shape.ToPrimitiveParams(paramList);
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
                return Shape.NumberOfSides;
            }
        }

        public ICollection<TextureEntryFace> GetFaces(int face)
        {
            if(face == ALL_SIDES)
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
                    Shape = PrimitiveShape.FromPrimitiveParams(enumerator);
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

                data[pos++] = (byte)Group.AttachPoint;
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
