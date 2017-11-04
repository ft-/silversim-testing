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
    public sealed class GlovesSubBaker : AbstractSubBaker
    {
        private Bitmap m_BakeGloves;
        private byte[] m_BumpGloves;

        private readonly UUID m_GlovesTextureId;
        private readonly Color3 m_GlovesColor;
        private readonly double m_GlovesLength;
        private readonly double m_GlovesFingers;
        private readonly double m_GlovesFingersBump;
        private readonly double m_GlovesLengthBump;

        public GlovesSubBaker(Wearable gloves)
        {
            if(gloves.Type != WearableType.Gloves)
            {
                throw new ArgumentException(nameof(gloves));
            }

            m_GlovesLength = gloves.GetParamValueOrDefault(1058, 0.01);
            m_GlovesFingers = gloves.GetParamValueOrDefault(1060, 1);
            m_GlovesFingersBump = gloves.GetParamValueOrDefault(1061, 1);
            m_GlovesLengthBump = gloves.GetParamValueOrDefault(1059, 0.8);
            gloves.Textures.TryGetValue(AvatarTextureIndex.UpperGloves, out m_GlovesTextureId);
            if (m_GlovesTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_GlovesTextureId = UUID.Zero;
            }
            m_GlovesColor = GetGlovesColor(gloves);
        }

        public override bool IsBaked => m_BakeGloves != null && m_BumpGloves != null;

        public override WearableType Type => WearableType.Gloves;

        public override Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.UpperBody)
            {
                if(m_BakeGloves != null)
                {
                    return m_BakeGloves;
                }

                Image img;
                m_BakeGloves = cache.TryGetTexture(m_GlovesTextureId, target, out img) ?
                    new Bitmap(img) : CreateWhiteBakeImage(target);

                InsideAlphaBlend(m_BakeGloves, (rawdata) =>
                {
                    BlendAlpha(rawdata, BaseBakes.GlovesFingersAlpha, m_GlovesFingers);
                    BlendAlpha(rawdata, BaseBakes.GlovesLengthAlpha, m_GlovesLength);
                });
            }
            return null;
        }

        public override byte[] BakeBumpOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.UpperBody)
            {
                if(m_BumpGloves == null)
                {
                    if (!cache.TryGetBump(m_GlovesTextureId, target, out m_BumpGloves))
                    {
                        m_BumpGloves = BaseBakes.UpperBodyBump;
                    }
                    BlendBump(m_BumpGloves, BaseBakes.GlovesFingersAlpha, m_GlovesFingersBump);
                    BlendBump(m_BumpGloves, BaseBakes.GlovesLengthAlpha, m_GlovesLengthBump);
                }
                return m_BumpGloves;
            }
            return null;
        }

        public override ColorAlpha BakeImageColor(BakeTarget target) => (ColorAlpha)m_GlovesColor;

        public override void Dispose()
        {
            m_BakeGloves?.Dispose();
        }

        private static Color3 GetGlovesColor(Wearable gloves)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (gloves.Params.TryGetValue(827, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (gloves.Params.TryGetValue(829, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (gloves.Params.TryGetValue(830, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }
    }
}
