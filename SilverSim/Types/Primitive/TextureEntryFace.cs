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

using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;

namespace SilverSim.Types.Primitive
{
    public class TextureEntryFace : IReferencesAccessor
    {

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
                    TextureID,
                    MaterialID
                };
        #endregion

        public bool IsSame(TextureEntryFace face) => TextureColor == face.TextureColor &&
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

        internal byte Material { get; set; }

        internal byte Media { get; set; }

        public ColorAlpha TextureColor { get; set; } = new ColorAlpha(1, 1, 1, 1);

        public float RepeatU { get; set; } = 1;

        public float RepeatV { get; set; } = 1;

        public float OffsetU { get; set; }

        public float OffsetV { get; set; }

        public float Rotation { get; set; }

        public float Glow { get; set; }

        public Bumpiness Bump
        {
            get
            {
                return (Bumpiness)(Material & BUMP_MASK);
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
                return (Shininess)(Material & SHINY_MASK);
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
                return (Material & FULLBRIGHT_MASK) != 0;
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
                return (Media & MEDIA_MASK) != 0;
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
                return (MappingType)(Media & TEX_MAP_MASK);
            }
            set
            {
                Media &= 0xF9;
                Media |= (byte)((byte)value & 0x6);
            }
        }

        public UUID TextureID { get; set; } = TextureConstant.Default;

        public UUID MaterialID { get; set; } = UUID.Zero;

        public override string ToString() => String.Format("Color: {0} RepeatU: {1} RepeatV: {2} OffsetU: {3} OffsetV: {4} " +
                "Rotation: {5} Bump: {6} Shiny: {7} Fullbright: {8} Mapping: {9} Media: {10} Glow: {11} ID: {12} MaterialID: {13}",
                TextureColor.ToString(), RepeatU, RepeatV, OffsetU, OffsetV, Rotation, Bump.ToString(), Shiny.ToString(), FullBright, TexMapType.ToString(),
                MediaFlags, Glow, TextureID.ToString(), MaterialID.ToString());

        public TextureEntryFace()
        {
        }

        internal TextureEntryFace(TextureEntryFace src)
        {
            TextureColor = new ColorAlpha(src.TextureColor);
            RepeatU = src.RepeatU;
            RepeatV = src.RepeatV;
            OffsetU = src.OffsetU;
            OffsetV = src.OffsetV;
            Rotation = src.Rotation;
            Glow = src.Glow;
            Material = src.Material;
            Media = src.Media;
            TextureID = src.TextureID;
            MaterialID = src.MaterialID;
        }
    }
}
