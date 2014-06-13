/*

ArribaSim is distributed under the terms of the
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;

namespace ArribaSim.Scene.Types.Object
{
    public class PrimitiveFace : IPrimitiveParamsInterface
    {
        #region Events
        public delegate void OnUpdateDelegate(PrimitiveFace face);
        public event OnUpdateDelegate OnUpdate;
        #endregion

        #region Fields
        public class TextureParam
        {
            public static readonly string TEXTURE_BLANK = "5748decc-f629-461c-9a36-a35a221fe21f";
            public static readonly string TEXTURE_MEDIA = "8b5fec65-8d8d-9dc5-cda8-8fdf2716e361";
            public static readonly string TEXTURE_PLYWOOD = "89556747-24cb-43ed-920b-47caed15465f";
            public static readonly string TEXTURE_TRANSPARENT = "8dcd4a48-2d37-4909-9f78-f7a9eb4ef903"; 
            #region Constructor
            public TextureParam()
            {

            }
            #endregion

            public string Texture = TEXTURE_PLYWOOD;
            public Vector3 Repeats = Vector3.One;
            public Vector3 Offsets = Vector3.Zero;
            public Angle Rotation = Angle.Zero;
        }
        private readonly TextureParam m_Texture = new TextureParam();

        private readonly ColorAlpha m_Color = new ColorAlpha(1, 1, 1, 1);

        private bool m_IsFullBright = false;
        private double m_Glow = 0f;

        public class BumpShinyParam
        {
            #region Enumerations
            public enum ShinyType : int
            {
                None = 0,
                Low = 1,
                Medium = 2,
                High = 3
            }
            public enum BumpType : int
            {
                None = 0,
                Bright = 1,
                Dark = 2,
                Wood = 3,
                Bark = 4,
                Bricks = 5,
                Checker = 6,
                Concrete = 7,
                Tile = 8,
                Stone = 9,
                Disks = 10,
                Gravel = 11,
                Blobs = 12,
                Siding = 13,
                LargeTile = 14,
                Stucco = 15,
                Suction = 16,
                Weave = 17
            }
            #endregion

            #region Constructor
            public BumpShinyParam()
            {

            }
            #endregion

            #region Fields
            public ShinyType Shiny = ShinyType.None;
            public BumpType Bump = BumpType.None;
            #endregion
        }
        private readonly BumpShinyParam m_BumpShiny = new BumpShinyParam();

        public enum TexGenMode : int
        {
            Default,
            Planar
        }
        private TexGenMode m_TexGen = TexGenMode.Default;

        public readonly int FaceNumber;
        #endregion

        #region Constructor
        public PrimitiveFace(int faceNumber)
        {
            FaceNumber = faceNumber;
        }
        #endregion

        #region Properties
        public TexGenMode TexGen
        {
            get
            {
                return m_TexGen;
            }
            set
            {
                m_TexGen = value;
                OnUpdate.Invoke(this);
            }
        }

        public TextureParam Texture
        {
            get
            {
                TextureParam p = new TextureParam();
                lock(m_Texture)
                {
                    p.Texture = m_Texture.Texture;
                    p.Repeats = m_Texture.Repeats;
                    p.Offsets = m_Texture.Offsets;
                    p.Rotation = m_Texture.Rotation;
                }
                return p;
            }
            set
            {
                lock(m_Texture)
                {
                    m_Texture.Texture = value.Texture;
                    m_Texture.Repeats = value.Repeats;
                    m_Texture.Offsets = value.Offsets;
                    m_Texture.Rotation = value.Rotation;
                }
                OnUpdate.Invoke(this);
            }
        }

        public BumpShinyParam BumpShiny
        {
            get
            {
                BumpShinyParam p = new BumpShinyParam();
                lock(m_BumpShiny)
                {
                    p.Bump = m_BumpShiny.Bump;
                    p.Shiny = m_BumpShiny.Shiny;
                }
                return p;
            }
            set
            {
                lock(m_BumpShiny)
                {
                    m_BumpShiny.Bump = value.Bump;
                    m_BumpShiny.Shiny = value.Shiny;
                }
                OnUpdate.Invoke(this);
            }
        }

        public ColorAlpha Color
        {
            get
            {
                lock(m_Color)
                {
                    return new ColorAlpha(m_Color);
                }
            }
            set
            {
                lock(m_Color)
                {
                    m_Color.R = value.R;
                    m_Color.G = value.G;
                    m_Color.B = value.B;
                    m_Color.A = value.A;
                }
                OnUpdate.Invoke(this);
            }
        }

        public bool IsFullBright
        {
            get
            {
                return m_IsFullBright;
            }
            set
            {
                m_IsFullBright = value;
                OnUpdate.Invoke(this);
            }
        }

        public double Glow
        {
            get
            {
                lock(this)
                {
                    return m_Glow;
                }
            }
            set
            {
                lock(this)
                {
                    m_Glow = value;
                }
                OnUpdate.Invoke(this);
            }
        }
        #endregion

        #region Primitive Params Functions
        public void GetPrimitiveParams(PrimitiveParamsType type, ref AnArray paramList)
        {
            switch(type)
            {
                case PrimitiveParamsType.Texture:
                    {
                        TextureParam tex = Texture;
                        paramList.Add(tex.Texture);
                        paramList.Add(tex.Repeats);
                        paramList.Add(tex.Offsets);
                        paramList.Add(tex.Rotation.Radians);
                    }
                    break;

                case PrimitiveParamsType.Color:
                    {
                        ColorAlpha color = Color;
                        paramList.Add(color.AsVector3);
                        paramList.Add(color.A);
                    }
                    break;

                case PrimitiveParamsType.BumpShiny:
                    {
                        BumpShinyParam p = BumpShiny;
                        paramList.Add((int)p.Shiny);
                        paramList.Add((int)p.Bump);
                    }
                    break;

                case PrimitiveParamsType.FullBright:
                    paramList.Add(IsFullBright);
                    break;

                case PrimitiveParamsType.TexGen:
                    paramList.Add((int)TexGen);
                    break;

                case PrimitiveParamsType.Glow:
                    paramList.Add(Glow);
                    break;

                default:
                    throw new ArgumentException(String.Format("Internal error! Primitive parameter type {0} should not be passed to PrimitiveFace", type));
            }
        }

        public void SetPrimitiveParams(PrimitiveParamsType type, AnArray.MarkEnumerator enumerator)
        {
            switch (type)
            {
                case PrimitiveParamsType.Texture:
                    {
                        TextureParam p = new TextureParam();
                        p.Texture = ParamsHelper.GetString(enumerator, "PRIM_TEXTURE");
                        p.Repeats = ParamsHelper.GetVector(enumerator, "PRIM_TEXTURE");
                        p.Offsets = ParamsHelper.GetVector(enumerator, "PRIM_TEXTURE");
                        p.Rotation.Radians = ParamsHelper.GetDouble(enumerator, "PRIM_TEXTURE");
                        Texture = p;
                    }
                    break;

                case PrimitiveParamsType.Color:
                    {
                        Vector3 color = ParamsHelper.GetVector(enumerator, "PRIM_COLOR");
                        double alpha = ParamsHelper.GetDouble(enumerator, "PRIM_COLOR");
                        Color = new ColorAlpha(color, alpha);
                    }
                    break;

                case PrimitiveParamsType.BumpShiny:
                    {
                        BumpShinyParam p = new BumpShinyParam();
                        p.Shiny = (BumpShinyParam.ShinyType) ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY");
                        p.Bump = (BumpShinyParam.BumpType) ParamsHelper.GetInteger(enumerator, "PRIM_BUMP_SHINY");
                        BumpShiny = p;
                    }
                    break;

                case PrimitiveParamsType.FullBright:
                    IsFullBright = ParamsHelper.GetBoolean(enumerator, "PRIM_FULLBRIGHT");
                    break;

                case PrimitiveParamsType.TexGen:
                    TexGen = (TexGenMode) ParamsHelper.GetInteger(enumerator, "PRIM_TEXGEN");
                    break;

                case PrimitiveParamsType.Glow:
                    Glow = ParamsHelper.GetDouble(enumerator, "PRIM_GLOW");
                    break;

                default:
                    throw new ArgumentException(String.Format("Internal error! Primitive parameter type {0} should not be passed to PrimitiveFace", type));
            }
        }
        #endregion
    }
}
