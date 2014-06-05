/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using ArribaSim.Types;
using System;
using System.Collections.Generic;
using ThreadedClasses;
using ArribaSim.Scene.Types.Script.Events;

namespace ArribaSim.Scene.Types.Object
{
    public class ObjectPart : IObject, IDisposable
    {
        #region Events
        public delegate void OnUpdateDelegate(ObjectPart part, int changed);
        public event OnUpdateDelegate OnUpdate;
        #endregion

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
        private readonly AnArray m_ParticleSystem = new AnArray();
        private bool m_IsAllowedDrop = false;

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

        public readonly RwLockedSortedDictionary<int, PrimitiveFace> Faces = new RwLockedSortedDictionary<int, PrimitiveFace>();
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
            Faces.Clear();
        }
        #endregion

        #region Properties
        public ObjectGroup Group { get; private set; }
        public ObjectPartInventory Inventory { get; private set; }

        public bool IsChanged { get; private set; }

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
                OnUpdate(this, (int)ChangedEvent.ChangedFlags.AllowedDrop);
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
                OnUpdate(this, 0);
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
                OnUpdate(this, 0);
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
                OnUpdate(this, 0);
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
                OnUpdate(this, 0);
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
                OnUpdate(this, (int)ChangedEvent.ChangedFlags.Shape);
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
                OnUpdate(this, 0);
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
                OnUpdate(this, (int)ChangedEvent.ChangedFlags.Scale);
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
                OnUpdate(this, (int)ChangedEvent.ChangedFlags.Shape);
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
                OnUpdate(this, 0);
            }
        }

        public AnArray ParticleSystem
        {
            get
            {
                return m_ParticleSystem;
            }
            set
            {
                lock(m_ParticleSystem)
                {
                    m_ParticleSystem.Clear();
                    m_ParticleSystem.AddRange(value);
                    IsChanged = true;
                    OnUpdate(this, 0);
                }
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
                OnUpdate(this, (int)ChangedEvent.ChangedFlags.Shape);
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
                OnUpdate(this, 0);
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
                OnUpdate(this, 0);
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
                OnUpdate(this, (int)ChangedEvent.ChangedFlags.Shape);
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
                OnUpdate(this, 0);
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
                OnUpdate(this, 0);
            }
        }
        #endregion

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
                OnUpdate(this, 0);
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
                OnUpdate(this, 0);
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
                OnUpdate(this, 0);
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
                OnUpdate(this, 0);
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
                OnUpdate(this, 0);
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
                OnUpdate(this, 0);
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
                    {
                        ICollection<PrimitiveFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXTURE"));
                        foreach(PrimitiveFace face in faces)
                        {
                            face.GetPrimitiveParams(PrimitiveParamsType.Texture, ref paramList);
                        }
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
                    {
                        ICollection<PrimitiveFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_COLOR"));
                        foreach(PrimitiveFace face in faces)
                        {
                            face.GetPrimitiveParams(PrimitiveParamsType.Color, ref paramList);
                        }
                    }
                    break;

                case PrimitiveParamsType.BumpShiny:
                    {
                        ICollection<PrimitiveFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY"));
                        foreach(PrimitiveFace face in faces)
                        {
                            face.GetPrimitiveParams(PrimitiveParamsType.BumpShiny, ref paramList);
                        }
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
                    {
                        ICollection<PrimitiveFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_FULLBRIGHT"));
                        foreach (PrimitiveFace face in faces)
                        {
                            face.GetPrimitiveParams(PrimitiveParamsType.Texture, ref paramList);
                        }
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
                    {
                        ICollection<PrimitiveFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXGEN"));
                        foreach(PrimitiveFace face in faces)
                        {
                            face.GetPrimitiveParams(PrimitiveParamsType.Texture, ref paramList);
                        }
                    }
                    break;

                case PrimitiveParamsType.Glow:
                    {
                        ICollection<PrimitiveFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_GLOW"));
                        foreach(PrimitiveFace face in faces)
                        {
                            face.GetPrimitiveParams(PrimitiveParamsType.Texture, ref paramList);
                        }
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

        public ICollection<PrimitiveFace> GetFaces(int face)
        {
            if(face  == ALL_SIDES)
            {
                return Faces.Values;
            }
            else
            {
                List<PrimitiveFace> list = new List<PrimitiveFace>();
                list.Add(Faces[face]);
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
                    {
                        ICollection<PrimitiveFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXTURE"));
                        enumerator.MarkPosition();
                        foreach(PrimitiveFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            face.SetPrimitiveParams(PrimitiveParamsType.Texture, enumerator);
                        }
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
                    {
                        ICollection<PrimitiveFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXTURE"));
                        enumerator.MarkPosition();
                        foreach(PrimitiveFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            face.SetPrimitiveParams(PrimitiveParamsType.Color, enumerator);
                        }
                    }
                    break;

                case PrimitiveParamsType.BumpShiny:
                    {
                        ICollection<PrimitiveFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY"));
                        enumerator.MarkPosition();
                        foreach (PrimitiveFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            face.SetPrimitiveParams(PrimitiveParamsType.BumpShiny, enumerator);
                        }
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
                    {
                        ICollection<PrimitiveFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_FULLBRIGHT"));
                        enumerator.MarkPosition();
                        foreach (PrimitiveFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            face.SetPrimitiveParams(PrimitiveParamsType.FullBright, enumerator);
                        }
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
                    {
                        ICollection<PrimitiveFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_TEXGEN"));
                        enumerator.MarkPosition();
                        foreach(PrimitiveFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            face.SetPrimitiveParams(PrimitiveParamsType.TexGen, enumerator);
                        }
                    }
                    break;

                case PrimitiveParamsType.Glow:
                    {
                        ICollection<PrimitiveFace> faces = GetFaces(ParamsHelper.GetInteger(enumerator, "PRIM_GLOW"));
                        enumerator.MarkPosition();
                        foreach(PrimitiveFace face in faces)
                        {
                            enumerator.GoToMarkPosition();
                            face.SetPrimitiveParams(PrimitiveParamsType.Texture, enumerator);
                        }
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

        #region Object Details Methods
        public void GetObjectDetails(AnArray.Enumerator enumerator, ref AnArray paramList)
        {
            Group.GetObjectDetails(enumerator, ref paramList);
        }
        #endregion
    }
}
