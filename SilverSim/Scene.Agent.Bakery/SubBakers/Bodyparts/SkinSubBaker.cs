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
using ColorAlpha = SilverSim.Types.ColorAlpha;
using UUID = SilverSim.Types.UUID;

namespace SilverSim.Scene.Agent.Bakery.SubBakers.Bodyparts
{
    public sealed class SkinSubBaker : AbstractSubBaker
    {
        private Image HeadBake;
        private Image UpperBake;
        private Image LowerBake;

        /* parameters */
        private Color3 m_SkinColor;
        private ColorAlpha m_RosyComplexionColor;
        private ColorAlpha m_LipPinknessColor;
        private ColorAlpha m_LipstickColor;
        private ColorAlpha m_BlushColor;
        private ColorAlpha m_OutershadowColor;
        private ColorAlpha m_InnershadowColor;
        private ColorAlpha m_EyelinerColor;
        private ColorAlpha m_NailpolishColor;
        private double m_Innershadow;
        private double m_Outershadow;
        private UUID m_HeadTextureId;
        private UUID m_UpperTextureId;
        private UUID m_LowerTextureId;

        public SkinSubBaker(Wearable skin)
        {
            if(skin.Type != WearableType.Skin)
            {
                throw new ArgumentException(nameof(skin));
            }
            m_SkinColor = GetSkinColor(skin);
            m_RosyComplexionColor = GetRosyComplexionColor(skin);
            m_LipPinknessColor = GetLipPinknessColor(skin);
            m_LipstickColor = GetLipstickColor(skin);
            m_BlushColor = GetBlushColor(skin);
            m_OutershadowColor = GetOuterShadowColor(skin);
            m_InnershadowColor = GetInnerShadowColor(skin);
            m_EyelinerColor = GetEyelinerColor(skin);
            m_NailpolishColor = GetNailPolishColor(skin);
            m_Innershadow = skin.GetParamValueOrDefault(709, 0);
            m_Outershadow = skin.GetParamValueOrDefault(707, 0);
            skin.Textures.TryGetValue(AvatarTextureIndex.HeadBodypaint, out m_HeadTextureId);
            skin.Textures.TryGetValue(AvatarTextureIndex.UpperBodypaint, out m_UpperTextureId);
            skin.Textures.TryGetValue(AvatarTextureIndex.LowerBodypaint, out m_LowerTextureId);
            if (m_HeadTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_HeadTextureId = UUID.Zero;
            }
            if (m_UpperTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_UpperTextureId = UUID.Zero;
            }
            if (m_LowerTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_LowerTextureId = UUID.Zero;
            }
        }

        public override bool IsBaked => HeadBake != null && UpperBake != null && LowerBake != null;

        public override WearableType Type => WearableType.Skin;

        public override Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            Image img;
            if(target == BakeTarget.Head)
            {
                if(HeadBake != null)
                {
                    return HeadBake;
                }
                Rectangle bakeRectangle = GetTargetBakeDimensions(target);
                HeadBake = CreateTargetBakeImage(target);
                using (Graphics gfx = Graphics.FromImage(HeadBake))
                {
                    using (var brush = new SolidBrush(m_SkinColor.ToDrawing()))
                    {
                        gfx.FillRectangle(brush, bakeRectangle);
                    }
                    gfx.CompositingMode = CompositingMode.SourceOver;
                    gfx.DrawTinted(bakeRectangle, BaseBakes.HeadColorAndSkinGrain, m_SkinColor);
                    if (m_HeadTextureId != UUID.Zero && cache.TryGetTexture(m_HeadTextureId, target, out img))
                    {
                        gfx.DrawUntinted(bakeRectangle, img);
                    }
                    else
                    {
                        gfx.DrawColorKeyed(bakeRectangle, BaseBakes.RosyfaceAlpha, m_RosyComplexionColor);
                        gfx.DrawColorKeyed(bakeRectangle, BaseBakes.LipsMask, m_LipPinknessColor);
                        gfx.DrawColorKeyed(bakeRectangle, BaseBakes.LipstickAlpha, m_LipstickColor);
                        gfx.DrawColorKeyed(bakeRectangle, BaseBakes.EyelinerAlpha, m_EyelinerColor);
                        gfx.DrawColorKeyed(bakeRectangle, BaseBakes.BlushAlpha, m_BlushColor);
                        gfx.DrawColorKeyed(bakeRectangle, BaseBakes.InnershadowAlpha, m_InnershadowColor, m_Innershadow);
                        gfx.DrawColorKeyed(bakeRectangle, BaseBakes.OutershadowAlpha, m_OutershadowColor, m_Outershadow);
                    }
                }

                return HeadBake;
            }
            else if(target == BakeTarget.LowerBody)
            {
                if (LowerBake != null)
                {
                    return LowerBake;
                }
                Rectangle bakeRectangle = GetTargetBakeDimensions(target);
                LowerBake = CreateTargetBakeImage(target);
                using (Graphics gfx = Graphics.FromImage(LowerBake))
                {
                    using (var brush = new SolidBrush(m_SkinColor.ToDrawing()))
                    {
                        gfx.FillRectangle(brush, bakeRectangle);
                    }
                    gfx.DrawUntinted(bakeRectangle, BaseBakes.LowerBodyColorAndSkinGrain);
                    if (m_LowerTextureId != UUID.Zero && cache.TryGetTexture(m_LowerTextureId, target, out img))
                    {
                        gfx.DrawUntinted(bakeRectangle, img);
                    }
                }

                return LowerBake;
            }
            else if(target == BakeTarget.UpperBody)
            {
                if (UpperBake != null)
                {
                    return UpperBake;
                }
                Rectangle bakeRectangle = GetTargetBakeDimensions(target);
                UpperBake = CreateTargetBakeImage(target);
                using (Graphics gfx = Graphics.FromImage(UpperBake))
                {
                    using (var brush = new SolidBrush(m_SkinColor.ToDrawing()))
                    {
                        gfx.FillRectangle(brush, bakeRectangle);
                    }

                    gfx.DrawUntinted(bakeRectangle, BaseBakes.UpperBodyColorAndSkinGrain);
                    if (m_UpperTextureId != UUID.Zero && cache.TryGetTexture(m_UpperTextureId, target, out img))
                    {
                        gfx.DrawUntinted(bakeRectangle, img);
                    }
                    else
                    {
                        gfx.DrawColorKeyed(bakeRectangle, BaseBakes.NailpolishAlpha, m_NailpolishColor);
                    }
                }

                return UpperBake;
            }
            else
            {
                return null;
            }
        }

        public override void Dispose()
        {
            HeadBake?.Dispose();
            UpperBake?.Dispose();
            LowerBake?.Dispose();
        }

#region Skin parameters
        private static readonly Color3[] SkinColors =
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

        private static readonly Color3[] PigmentColors =
        {
            Color3.FromRgb(252, 215, 200),
            Color3.FromRgb(240, 177, 112),
            Color3.FromRgb(90, 40, 16),
            Color3.FromRgb(29, 9, 6)
        };

        private static Color3 GetSkinColor(Wearable skin)
        {
            var skinColor = new Color3(0, 0, 0);

            double val;
            if (skin.Params.TryGetValue(108, out val))
            {
                skinColor += CalcColor(val, SkinColors);
            }
            if (skin.Params.TryGetValue(110, out val))
            {
                skinColor = skinColor.Lerp(new Color3(218, 41, 37), val);
            }
            if (!skin.Params.TryGetValue(111, out val))
            {
                val = 0.5;
            }
            skinColor += CalcColor(val, PigmentColors);

            return skinColor;
        }

        private static ColorAlpha GetRosyComplexionColor(Wearable skin)
        {
            double value;
            ColorAlpha color = ColorAlpha.FromRgba(198, 71, 71, 0);
            /* Rosy complextion */
            if (skin.Params.TryGetValue(116, out value))
            {
                color.A = value.LimitRange(0, 1);
            }

            return color;
        }

        private static ColorAlpha GetLipPinknessColor(Wearable skin)
        {
            double value;
            ColorAlpha color = ColorAlpha.FromRgba(220, 115, 115, 0);
            /* Lip pinkness */
            if (skin.Params.TryGetValue(117, out value))
            {
                color.A = value.LimitRange(0, 1) * 0.5;
            }

            return color;
        }

        private static readonly ColorAlpha[] LipstickColors = new ColorAlpha[]
        {
            ColorAlpha.FromRgba(245, 161, 177, 200),
            ColorAlpha.FromRgba(216, 37, 67, 200),
            ColorAlpha.FromRgba(178, 48, 76, 200),
            ColorAlpha.FromRgba(68, 0, 11, 200),
            ColorAlpha.FromRgba(252, 207, 184, 200),
            ColorAlpha.FromRgba(241, 136, 106, 200),
            ColorAlpha.FromRgba(208, 110, 85, 200),
            ColorAlpha.FromRgba(106, 28, 18, 200),
            ColorAlpha.FromRgba(58, 26, 49, 200),
            ColorAlpha.FromRgba(14, 14, 14, 200)
        };

        private static ColorAlpha GetLipstickColor(Wearable skin)
        {
            double value;
            /* Limp pinkness */
            if (!skin.Params.TryGetValue(700, out value))
            {
                value = 0.25;
            }
            ColorAlpha color = CalcColor(value, LipstickColors);
            if (!skin.Params.TryGetValue(701, out value))
            {
                value = 0;
            }
            color.A *= value * 0.2;

            return color;
        }

        private static readonly ColorAlpha[] BlushColors = new ColorAlpha[]
        {
            ColorAlpha.FromRgba(253, 162, 193, 200),
            ColorAlpha.FromRgba(247, 131, 152, 200),
            ColorAlpha.FromRgba(213, 122, 140, 200),
            ColorAlpha.FromRgba(253, 152, 144, 200),
            ColorAlpha.FromRgba(236, 138, 103, 200),
            ColorAlpha.FromRgba(195, 128, 122, 200),
            ColorAlpha.FromRgba(148, 103, 100, 200),
            ColorAlpha.FromRgba(168, 95, 62, 200)
        };

        private static ColorAlpha GetBlushColor(Wearable skin)
        {
            double value;
            ColorAlpha color = BlushColors[0];
            if (skin.Params.TryGetValue(705, out value))
            {
                color = CalcColor(value, BlushColors);
            }

            if (!skin.Params.TryGetValue(711, out value))
            {
                value = 0;
            }
            color.A *= value * 0.3;

            return color;
        }

        /*
            Params[707] = new VisualParam(707, "Outer Shadow", 0, "skin", String.Empty, "No Eyeshadow", "More Eyeshadow", 0f, 0f, 0.7f, false, null, new VisualAlphaParam(0.05f, "eyeshadow_outer_alpha.tga", true, false), null);
            Params[709] = new VisualParam(709, "Inner Shadow", 0, "skin", String.Empty, "No Eyeshadow", "More Eyeshadow", 0f, 0f, 1f, false, null, new VisualAlphaParam(0.2f, "eyeshadow_inner_alpha.tga", true, false), null);
         */
        private static Color3[] OuterShadowColors = new Color3[]
        {
            Color3.FromRgb(252, 247, 246),
            Color3.FromRgb(255, 206, 206),
            Color3.FromRgb(233, 135, 149),
            Color3.FromRgb(220, 168, 192),
            Color3.FromRgb(228, 203, 232),
            Color3.FromRgb(255, 234, 195),
            Color3.FromRgb(230, 157, 101),
            Color3.FromRgb(255, 147, 86),
            Color3.FromRgb(228, 110, 89),
            Color3.FromRgb(228, 150, 120),
            Color3.FromRgb(223, 227, 213),
            Color3.FromRgb(96, 116, 87),
            Color3.FromRgb(88, 143, 107),
            Color3.FromRgb(194, 231, 223),
            Color3.FromRgb(207, 227, 234),
            Color3.FromRgb(41, 171, 212),
            Color3.FromRgb(180, 137, 130),
            Color3.FromRgb(173, 125, 105),
            Color3.FromRgb(144, 95, 98),
            Color3.FromRgb(115, 70, 77),
            Color3.FromRgb(155, 78, 47),
            Color3.FromRgb(239, 239, 239),
            Color3.FromRgb(194, 194, 194),
            Color3.FromRgb(120, 120, 120),
            Color3.FromRgb(10, 10, 10)
        };

        private static ColorAlpha GetOuterShadowColor(Wearable skin)
        {
            double value;
            var color = (ColorAlpha)OuterShadowColors[0];
            if (!skin.Params.TryGetValue(708, out value))
            {
                color = (ColorAlpha)CalcColor(value, OuterShadowColors);
            }
            if (!skin.Params.TryGetValue(706, out value))
            {
                value = 0.6;
            }
            color.A = value * 0.05;

            return color;
        }

        private static Color3[] InnerShadowColors = new Color3[]
        {
            Color3.FromRgb(252, 247, 246),
            Color3.FromRgb(255, 206, 206),
            Color3.FromRgb(233, 135, 149),
            Color3.FromRgb(220, 168, 192),
            Color3.FromRgb(228, 203, 232),
            Color3.FromRgb(255, 234, 195),
            Color3.FromRgb(230, 157, 101),
            Color3.FromRgb(255, 147, 86),
            Color3.FromRgb(228, 110, 89),
            Color3.FromRgb(228, 150, 120),
            Color3.FromRgb(223, 227, 213),
            Color3.FromRgb(96, 116, 87),
            Color3.FromRgb(88, 143, 107),
            Color3.FromRgb(194, 231, 223),
            Color3.FromRgb(207, 227, 234),
            Color3.FromRgb(41, 171, 212),
            Color3.FromRgb(180, 137, 130),
            Color3.FromRgb(173, 125, 105),
            Color3.FromRgb(144, 95, 98),
            Color3.FromRgb(115, 70, 77),
            Color3.FromRgb(155, 78, 47),
            Color3.FromRgb(239, 239, 239),
            Color3.FromRgb(194, 194, 194),
            Color3.FromRgb(120, 120, 120),
            Color3.FromRgb(10, 10, 10)
        };

        private static ColorAlpha GetInnerShadowColor(Wearable skin)
        {
            double value;
            var color = (ColorAlpha)InnerShadowColors[0];
            if (!skin.Params.TryGetValue(712, out value))
            {
                color = (ColorAlpha)CalcColor(value, InnerShadowColors);
            }
            if (!skin.Params.TryGetValue(713, out value))
            {
                value = 0.7;
            }
            color.A = value * 0.2;

            return color;
        }

        private static readonly ColorAlpha[] EyelinerColors = new ColorAlpha[]
        {
            ColorAlpha.FromRgba(24, 98, 40, 250),
            ColorAlpha.FromRgba(9, 100, 127, 250),
            ColorAlpha.FromRgba(61, 93, 134, 250),
            ColorAlpha.FromRgba(70, 29, 27, 250),
            ColorAlpha.FromRgba(115, 75, 65, 250),
            ColorAlpha.FromRgba(100, 100, 100, 250),
            ColorAlpha.FromRgba(91, 80, 74, 250),
            ColorAlpha.FromRgba(112, 42, 76, 250),
            ColorAlpha.FromRgba(14, 14, 14, 250)
        };

        private static ColorAlpha GetEyelinerColor(Wearable skin)
        {
            double value;
            ColorAlpha color = EyelinerColors[0];
            if (skin.Params.TryGetValue(714, out value))
            {
                color = CalcColor(value, EyelinerColors);
            }

            if(!skin.Params.TryGetValue(703, out value))
            {
                value = 0;
            }
            color.A *= value * 0.1;

            return color;
        }

        private static Color3[] NailPolishColors = new Color3[]
        {
            Color3.FromRgb(255, 187, 200),
            Color3.FromRgb(194, 102, 127),
            Color3.FromRgb(227, 34, 99),
            Color3.FromRgb(168, 41, 60),
            Color3.FromRgb(97, 28, 59),
            Color3.FromRgb(234, 115, 93),
            Color3.FromRgb(142, 58, 47),
            Color3.FromRgb(114, 30, 46),
            Color3.FromRgb(14, 14, 14)
        };

        private static ColorAlpha GetNailPolishColor(Wearable skin)
        {
            double value;
            var color = (ColorAlpha)NailPolishColors[0];
            if (skin.Params.TryGetValue(715, out value))
            {
                color = (ColorAlpha)CalcColor(value, NailPolishColors);
            }

            if (!skin.Params.TryGetValue(710, out value))
            {
                value = 0;
            }
            color.A *= value;

            return color;
        }
#endregion

    }
}
