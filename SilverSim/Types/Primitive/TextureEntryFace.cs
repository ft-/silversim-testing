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

using System;
using System.Collections.Generic;

namespace SilverSim.Types.Primitive
{
    public class TextureEntryFace : ICloneable, Asset.Format.IReferencesAccessor
    {
        private ColorAlpha m_TextureColor = new ColorAlpha(1, 1, 1, 1);
        private float m_RepeatU = 1;
        private float m_RepeatV = 1;
        private float m_OffsetU;
        private float m_OffsetV;
        private float m_Rotation;
        private float m_Glow;
        private byte m_Material;
        private byte m_MediaByte;
        private TextureAttributes m_AttributeFlags /* = TextureAttributes.None */;
        private UUID m_TextureID = TextureConstant.Default;
        private UUID m_MaterialID = UUID.Zero;

        private readonly TextureEntryFace m_DefaultTexture;

        // +----------+ S = Shiny
        // | SSFBBBBB | F = Fullbright
        // | 76543210 | B = Bumpmap
        // +----------+
        private const byte BUMP_MASK = 0x1F;
        private const byte FULLBRIGHT_MASK = 0x20;
        private const byte SHINY_MASK = 0xC0;
        // +----------+ M = Media Flags (web page)
        // | .....TTM | T = Texture Mapping
        // | 76543210 | . = Unused
        // +----------+
        private const byte MEDIA_MASK = 0x01;
        private const byte TEX_MAP_MASK = 0x06;

        #region References accessor
        public List<UUID> References => new List<UUID>
                {
                    m_TextureID,
                    m_MaterialID
                };
        #endregion

        public bool IsSame(TextureEntryFace face) => TextureColor == face.m_TextureColor &&
                RepeatU == face.RepeatU &&
                RepeatV == face.RepeatV &&
                OffsetU == face.OffsetU &&
                OffsetV == face.OffsetV &&
                Rotation == face.Rotation &&
                Glow == face.Glow &&
                Material == face.Material &&
                Media == face.Media &&
                TextureID == face.TextureID &&
                MaterialID == face.MaterialID;

        internal byte Material
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.Material) != 0 || m_DefaultTexture == null) ?
                    m_Material :
                    m_DefaultTexture.Material;
            }
            set
            {
                m_Material = value;
                m_AttributeFlags |= TextureAttributes.Material;
            }
        }

        internal byte Media
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.Media) != 0 || m_DefaultTexture == null) ?
                    m_MediaByte :
                    m_DefaultTexture.Media;
            }
            set
            {
                m_MediaByte = value;
                m_AttributeFlags |= TextureAttributes.Media;
            }
        }

        public ColorAlpha TextureColor
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.RGBA) != 0 || m_DefaultTexture == null) ?
                    m_TextureColor :
                    m_DefaultTexture.TextureColor;
            }
            set
            {
                m_TextureColor = value;
                m_AttributeFlags |= TextureAttributes.RGBA;
            }
        }

        public float RepeatU
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.RepeatU) != 0 || m_DefaultTexture == null) ?
                    m_RepeatU :
                    m_DefaultTexture.RepeatU;
            }
            set
            {
                m_RepeatU = value;
                m_AttributeFlags |= TextureAttributes.RepeatU;
            }
        }

        public float RepeatV
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.RepeatV) != 0 || m_DefaultTexture == null) ?
                    m_RepeatV :
                     m_DefaultTexture.RepeatV;
            }
            set
            {
                m_RepeatV = value;
                m_AttributeFlags |= TextureAttributes.RepeatV;
            }
        }

        public float OffsetU
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.OffsetU) != 0 || m_DefaultTexture == null) ?
                    m_OffsetU :
                    m_DefaultTexture.OffsetU;
            }
            set
            {
                m_OffsetU = value;
                m_AttributeFlags |= TextureAttributes.OffsetU;
            }
        }

        public float OffsetV
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.OffsetV) != 0 || m_DefaultTexture == null) ?
                    m_OffsetV :
                    m_DefaultTexture.OffsetV;
            }
            set
            {
                m_OffsetV = value;
                m_AttributeFlags |= TextureAttributes.OffsetV;
            }
        }

        public float Rotation
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.Rotation) != 0 || m_DefaultTexture == null) ?
                    m_Rotation :
                    m_DefaultTexture.Rotation;
            }
            set
            {
                m_Rotation = value;
                m_AttributeFlags |= TextureAttributes.Rotation;
            }
        }

        public float Glow
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.Glow) != 0 || m_DefaultTexture == null) ?
                    m_Glow :
                    m_DefaultTexture.Glow;
            }
            set
            {
                m_Glow = value;
                m_AttributeFlags |= TextureAttributes.Glow;
            }
        }

        public Bumpiness Bump
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.Material) != 0 || m_DefaultTexture == null) ?
                    (Bumpiness)(Material & BUMP_MASK) :
                    m_DefaultTexture.Bump;
            }
            set
            {
                Material &= 0xE0;
                byte v = (byte)value;
                Material |= (byte)(v & BUMP_MASK);
            }
        }

        public Shininess Shiny
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.Material) != 0 || m_DefaultTexture == null) ?
                    (Shininess)(Material & SHINY_MASK) :
                    m_DefaultTexture.Shiny;
            }
            set
            {
                Material &= 0x3F;
                var v = (byte)value;
                Material |= (byte)(v & SHINY_MASK);
            }
        }

        public bool FullBright
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.Material) != 0 || m_DefaultTexture == null) ?
                    (Material & FULLBRIGHT_MASK) != 0 :
                    m_DefaultTexture.FullBright;
            }
            set
            {
                Material &= 0xDF;
                if(value)
                {
                    Material |= FULLBRIGHT_MASK;
                }
            }
        }

        public bool MediaFlags
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.Media) != 0 || m_DefaultTexture == null) ?
                    (Media & MEDIA_MASK) != 0 :
                    m_DefaultTexture.MediaFlags;
            }
            set
            {
                Media &= 0xFE;
                if(value)
                {
                    Media |= 0x01;
                }
            }
        }

        public MappingType TexMapType
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.Media) != 0 || m_DefaultTexture == null) ?
                    (MappingType)(Media & TEX_MAP_MASK) :
                    m_DefaultTexture.TexMapType;
            }
            set
            {
                Media &= 0xF9;
                Media |= (byte)((byte)value & 0x6);
            }
        }

        public UUID TextureID
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.TextureID) != 0 || m_DefaultTexture == null) ?
                    m_TextureID :
                    m_DefaultTexture.TextureID;
            }
            set
            {
                m_TextureID = value;
                m_AttributeFlags |= TextureAttributes.TextureID;
            }
        }

        public UUID MaterialID
        {
            get
            {
                return ((m_AttributeFlags & TextureAttributes.MaterialID) != 0 || m_DefaultTexture == null) ?
                    m_MaterialID :
                    m_DefaultTexture.MaterialID;
            }
            set
            {
                m_MaterialID = value;
                m_AttributeFlags |= TextureAttributes.MaterialID;
            }
        }

        public object Clone() => new TextureEntryFace(m_DefaultTexture)
        {
            TextureColor = TextureColor,
            RepeatU = RepeatU,
            RepeatV = RepeatV,
            OffsetU = OffsetU,
            OffsetV = OffsetV,
            Rotation = Rotation,
            Glow = Glow,
            Material = Material,
            Media = Media,
            TextureID = TextureID,
            MaterialID = MaterialID,
            m_AttributeFlags = m_AttributeFlags
        };

        public override string ToString() => String.Format("Color: {0} RepeatU: {1} RepeatV: {2} OffsetU: {3} OffsetV: {4} " +
                "Rotation: {5} Bump: {6} Shiny: {7} Fullbright: {8} Mapping: {9} Media: {10} Glow: {11} ID: {12} MaterialID: {13}",
                TextureColor.ToString(), RepeatU, RepeatV, OffsetU, OffsetV, Rotation, Bump.ToString(), Shiny.ToString(), FullBright, TexMapType.ToString(),
                MediaFlags, Glow, TextureID.ToString(), MaterialID.ToString());

        public TextureEntryFace(TextureEntryFace defaultTexture)
        {
            if (defaultTexture == null)
            {
                m_AttributeFlags = TextureAttributes.All;
                m_TextureID = TextureConstant.Default;
            }
            else
            {
                m_DefaultTexture = defaultTexture;
            }
        }

        internal TextureEntryFace(TextureEntryFace defaultTexture, TextureEntryFace src, TextureAttributes attrs = TextureAttributes.None)
        {
            if (defaultTexture == null)
            {
                m_AttributeFlags = TextureAttributes.All;
                m_TextureID = TextureConstant.Default;
            }
            else
            {
                m_AttributeFlags = src.m_AttributeFlags | attrs;
                m_DefaultTexture = defaultTexture;
                m_TextureColor = new ColorAlpha(src.m_TextureColor);
                m_RepeatU = src.m_RepeatU;
                m_RepeatV = src.m_RepeatV;
                m_OffsetU = src.m_OffsetU;
                m_OffsetV = src.m_OffsetV;
                m_Rotation = src.m_Rotation;
                m_Glow = src.m_Glow;
                m_Material = src.m_Material;
                m_MediaByte = src.m_MediaByte;
                m_AttributeFlags = src.m_AttributeFlags;
                m_TextureID = src.m_TextureID;
                m_MaterialID = src.m_MaterialID;
            }
        }
    }
}
