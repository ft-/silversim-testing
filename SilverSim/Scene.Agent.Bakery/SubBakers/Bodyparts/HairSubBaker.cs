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

using SilverSim.Types.Agent;
using SilverSim.Types.Asset.Format;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Color3 = SilverSim.Types.Color;
using UUID = SilverSim.Types.UUID;

namespace SilverSim.Scene.Agent.Bakery.SubBakers.Bodyparts
{
    public sealed class HairSubBaker : AbstractSubBaker
    {
        private Image m_HairBake;

        //parameters
        private Color3 m_HairColor;
        private UUID m_HairTextureId;

        public HairSubBaker(Wearable hair)
        {
            if(hair.Type != WearableType.Hair)
            {
                throw new ArgumentException(nameof(hair));
            }

            if(!hair.Textures.TryGetValue(AvatarTextureIndex.Hair, out m_HairTextureId))
            {
                m_HairTextureId = UUID.Zero;
            }

            m_HairColor = GetHairColor(hair);
        }

        public override bool IsBaked => m_HairBake != null;

        public override WearableType Type => WearableType.Hair;

        public override Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.Hair)
            {
                if(m_HairBake != null)
                {
                    return m_HairBake;
                }
                m_HairBake = CreateTargetBakeImage(target);
                Rectangle bakeRectangle = GetTargetBakeDimensions(target);
                using (Graphics gfx = Graphics.FromImage(m_HairBake))
                {
                    gfx.CompositingMode = CompositingMode.SourceCopy;
                    using (var brush = new SolidBrush(Color.FromArgb(0, 255, 255, 255)))
                    {
                        gfx.FillRectangle(brush, bakeRectangle);
                    }
                    /* alpha blending */
                    gfx.CompositingMode = CompositingMode.SourceOver;
                    Image img;
                    if(m_HairTextureId != UUID.Zero && cache.TryGetTexture(m_HairTextureId, target, out img))
                    {
                        gfx.DrawTinted(bakeRectangle, img, m_HairColor);
                    }
                }
                return m_HairBake;
            }

            return null;
        }

        public override void Dispose()
        {
            m_HairBake?.Dispose();
        }

        #region Hair parameters
        private static readonly Color3[] RainbowHairColors =
        {
            Color3.FromRgb(0, 0, 0),
            Color3.FromRgb(255, 0, 255),
            Color3.FromRgb(255, 0, 0),
            Color3.FromRgb(255, 255, 0),
            Color3.FromRgb(0, 255, 0),
            Color3.FromRgb(0, 255, 255),
            Color3.FromRgb(0, 0, 255),
            Color3.FromRgb(255, 0, 255)
        };

        private static readonly Color3[] RedHairColors =
        {
            Color3.FromRgb(0, 0, 0),
            Color3.FromRgb(118, 47, 19)
        };

        private static readonly Color3[] BlondeHairColors =
        {
            Color3.FromRgb(0, 0, 0),
            Color3.FromRgb(22, 6, 6),
            Color3.FromRgb(29, 9, 6),
            Color3.FromRgb(45, 21, 11),
            Color3.FromRgb(78, 39, 11),
            Color3.FromRgb(90, 53, 16),
            Color3.FromRgb(136, 92, 21),
            Color3.FromRgb(150, 106, 33),
            Color3.FromRgb(198, 156, 74),
            Color3.FromRgb(233, 192, 103),
            Color3.FromRgb(238, 205, 136)
        };

        private static Color3 GetHairColor(Wearable hair)
        {
            var col = new Color3(0, 0, 0);
            double val;
            if (hair.Params.TryGetValue(112, out val))
            {
                col += CalcColor(val, RainbowHairColors);
            }

            if (hair.Params.TryGetValue(113, out val))
            {
                col += CalcColor(val, RedHairColors);
            }

            if (hair.Params.TryGetValue(114, out val))
            {
                col += CalcColor(val, BlondeHairColors);
            }

            if (hair.Params.TryGetValue(115, out val))
            {
                col += new Color3(val, val, val);
            }
            return col;
        }
        #endregion

    }
}
