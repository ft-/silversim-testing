// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
        private UUID m_TextureID = DEFAULT_TEXTURE;
        private UUID m_MaterialID = UUID.Zero;

        private TextureEntryFace m_DefaultTexture;

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
        public List<UUID> References
        {
            get
            {
                List<UUID> reflist = new List<UUID>();
                reflist.Add(m_TextureID);
                reflist.Add(m_MaterialID);
                return reflist;
            }
        }
        #endregion

        internal byte Material
        {
            get
            {
                if((m_AttributeFlags & TextureAttributes.Material) != 0)
                {
                    return m_Material;
                }
                else
                {
                    return m_DefaultTexture.Material;
                }
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
                if((m_AttributeFlags & TextureAttributes.Media) != 0)
                {
                    return m_MediaByte;
                }
                else
                {
                    return m_DefaultTexture.Media;
                }
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
                if((m_AttributeFlags & TextureAttributes.RGBA) != 0)
                {
                    return m_TextureColor;
                }
                else
                {
                    return m_DefaultTexture.TextureColor;
                }
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
                if((m_AttributeFlags & TextureAttributes.RepeatU) != 0)
                {
                    return m_RepeatU;
                }
                else
                {
                    return m_DefaultTexture.RepeatU;
                }
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
                if ((m_AttributeFlags & TextureAttributes.RepeatV) != 0)
                {
                    return m_RepeatV;
                }
                else
                {
                    return m_DefaultTexture.RepeatV;
                }
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
                if ((m_AttributeFlags & TextureAttributes.OffsetU) != 0)
                {
                    return m_OffsetU;
                }
                else
                {
                    return m_DefaultTexture.OffsetU;
                }
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
                if ((m_AttributeFlags & TextureAttributes.OffsetV) != 0)
                {
                    return m_OffsetV;
                }
                else
                {
                    return m_DefaultTexture.OffsetV;
                }
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
                if((m_AttributeFlags & TextureAttributes.Rotation) != 0)
                {
                    return m_Rotation;
                }
                else
                {
                    return m_DefaultTexture.Rotation;
                }
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
                if((m_AttributeFlags & TextureAttributes.Glow) != 0)
                {
                    return m_Glow;
                }
                else
                {
                    return m_DefaultTexture.Glow;
                }
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
                if((m_AttributeFlags & TextureAttributes.Material) != 0)
                {
                    return (Bumpiness)(Material & BUMP_MASK);
                }
                else
                {
                    return m_DefaultTexture.Bump;
                }
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
                if((m_AttributeFlags & TextureAttributes.Material) != 0)
                {
                    return (Shininess)(Material & SHINY_MASK);
                }
                else
                {
                    return m_DefaultTexture.Shiny;
                }
            }
            set
            {
                Material &= 0x3F;
                byte v = (byte)value;
                Material |= (byte)(v & SHINY_MASK);
            }
        }

        public bool FullBright
        {
            get
            {
                if((m_AttributeFlags & TextureAttributes.Material) != 0)
                {
                    return (Material & FULLBRIGHT_MASK) != 0;
                }
                else
                {
                    return m_DefaultTexture.FullBright;
                }
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
                if ((m_AttributeFlags & TextureAttributes.Media) != 0)
                {
                    return (Media & MEDIA_MASK) != 0;
                }
                else
                {
                    return m_DefaultTexture.MediaFlags;
                }
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
                if((m_AttributeFlags & TextureAttributes.Media) != 0)
                {
                    return (MappingType)(Media & TEX_MAP_MASK);
                }
                else
                {
                    return m_DefaultTexture.TexMapType;
                }
            }
            set
            {
                Media &= 0xF9;
                Media |= (byte)((byte)value & 0x6);
            }
        }

        private static readonly UUID DEFAULT_TEXTURE = new UUID("5748decc-f629-461c-9a36-a35a221fe21f");
        public UUID TextureID
        {
            get
            {
                if((m_AttributeFlags & TextureAttributes.TextureID) != 0)
                {
                    return m_TextureID;
                }
                else
                {
                    return m_DefaultTexture.TextureID;
                }
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
                if((m_AttributeFlags & TextureAttributes.MaterialID) != 0)
                {
                    return m_MaterialID;
                }
                else
                {
                    return m_DefaultTexture.MaterialID;
                }
            }
            set
            {
                m_MaterialID = value;
                m_AttributeFlags |= TextureAttributes.MaterialID;
            }
        }

        public object Clone()
        {
            TextureEntryFace ret = new TextureEntryFace(m_DefaultTexture);
            ret.TextureColor = TextureColor;
            ret.RepeatU = RepeatU;
            ret.RepeatV = RepeatV;
            ret.OffsetU = OffsetU;
            ret.OffsetV = OffsetV;
            ret.Rotation = Rotation;
            ret.Glow = Glow;
            ret.Material = Material;
            ret.Media = Media;
            ret.TextureID = TextureID;
            ret.MaterialID = MaterialID;
            ret.m_AttributeFlags = m_AttributeFlags;
            return ret;
        }

        public override int GetHashCode()
        {
            return
                TextureColor.GetHashCode() ^
                RepeatU.GetHashCode() ^
                RepeatV.GetHashCode() ^
                OffsetU.GetHashCode() ^
                OffsetV.GetHashCode() ^
                Rotation.GetHashCode() ^
                Glow.GetHashCode() ^
                Bump.GetHashCode() ^
                Shiny.GetHashCode() ^
                FullBright.GetHashCode() ^
                MediaFlags.GetHashCode() ^
                TexMapType.GetHashCode() ^
                TextureID.GetHashCode() ^
                MaterialID.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("Color: {0} RepeatU: {1} RepeatV: {2} OffsetU: {3} OffsetV: {4} " +
                "Rotation: {5} Bump: {6} Shiny: {7} Fullbright: {8} Mapping: {9} Media: {10} Glow: {11} ID: {12} MaterialID: {13}",
                TextureColor.ToString(), RepeatU, RepeatV, OffsetU, OffsetV, Rotation, Bump.ToString(), Shiny.ToString(), FullBright, TexMapType.ToString(),
                MediaFlags, Glow, TextureID.ToString(), MaterialID.ToString());
        }

        public TextureEntryFace(TextureEntryFace defaultTexture)
        {
            if (defaultTexture == null)
            {
                m_AttributeFlags = TextureAttributes.All;
            }
            else
            {
                m_DefaultTexture = defaultTexture;
            }
        }
    }
}
