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
using Color3 = SilverSim.Types.Color;
using ColorAlpha = SilverSim.Types.ColorAlpha;
using UUID = SilverSim.Types.UUID;

namespace SilverSim.Scene.Agent.Bakery.SubBakers.Clothing
{
    public sealed class SkirtSubBaker : AbstractSubBaker
    {
        private Bitmap m_Bake;
        private byte[] m_Bump;

        private readonly UUID m_TextureId;
        private readonly double m_SkirtLength;
        private readonly double m_SlitFront;
        private readonly double m_SlitBack;
        private readonly double m_SlitLeft;
        private readonly double m_SlitRight;
        private readonly Color3 m_Color;

        public SkirtSubBaker(Wearable skirt)
        {
            if(skirt.Type != WearableType.Skirt)
            {
                throw new ArgumentException(nameof(skirt));
            }
            m_Color = GetSkirtColor(skirt);
            m_SkirtLength = skirt.GetParamValueOrDefault(858, 0.4);
            m_SlitFront = skirt.GetParamValueOrDefault(859, 1);
            m_SlitBack = skirt.GetParamValueOrDefault(860, 1);
            m_SlitLeft = skirt.GetParamValueOrDefault(861, 1);
            m_SlitRight = skirt.GetParamValueOrDefault(862, 1);
            skirt.Textures.TryGetValue(AvatarTextureIndex.Skirt, out m_TextureId);
            if (m_TextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_TextureId = UUID.Zero;
            }
        }

        public override bool IsBaked => m_Bake != null && m_Bump != null;

        public override WearableType Type => WearableType.Skirt;

        public override Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.Skirt)
            {
                if(m_Bake == null)
                {
                    Image img;
                    m_Bake = cache.TryGetTexture(m_TextureId, target, out img) ?
                        new Bitmap(img) : CreateWhiteBakeImage(target);

                    InsideAlphaBlend(m_Bake, (rawdata) =>
                    {
                        BlendAlpha(rawdata, BaseBakes.SkirtLengthAlpha, m_SkirtLength);
                        BlendAlpha(rawdata, BaseBakes.SkirtSlitBackAlpha, m_SlitBack);
                        BlendAlpha(rawdata, BaseBakes.SkirtSlitFrontAlpha, m_SlitFront);
                        BlendAlpha(rawdata, BaseBakes.SkirtSlitLeftAlpha, m_SlitLeft);
                        BlendAlpha(rawdata, BaseBakes.SkirtSlitRightAlpha, m_SlitRight);
                    });
                }
                return m_Bake;
            }
            return null;
        }

        public override ColorAlpha BakeImageColor(BakeTarget target) => (ColorAlpha)m_Color;

        public override byte[] BakeBumpOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.Skirt)
            {
                if (m_Bump == null && !cache.TryGetBump(m_TextureId, target, out m_Bump))
                {
                    m_Bump = new byte[512 * 512];
                }
                return m_Bump;
            }
            return null;
        }

        public override void Dispose()
        {
            m_Bake?.Dispose();
        }

        private static Color3 GetSkirtColor(Wearable skirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (skirt.Params.TryGetValue(921, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(922, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(923, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }
    }
}
