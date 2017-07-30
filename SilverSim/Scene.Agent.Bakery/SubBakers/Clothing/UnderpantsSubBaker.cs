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
    public sealed class UnderpantsSubBaker : AbstractSubBaker
    {
        private Bitmap m_LowerBake;
        private byte[] m_LowerBump;

        private Color3 m_Color;
        private double m_PantsLength;
        private double m_PantsLengthBump;
        private double m_PantsWaist;
        private double m_PantsWaistBump;
        private UUID m_TextureId;

        public UnderpantsSubBaker(Wearable underpants)
        {
            if(underpants.Type != WearableType.Underpants)
            {
                throw new ArgumentException(nameof(underpants));
            }
            m_Color = GetUnderpantsColor(underpants);
            m_PantsLength = underpants.GetParamValueOrDefault(1054, 0.3);
            m_PantsLengthBump = underpants.GetParamValueOrDefault(1055, 0.3);
            m_PantsWaist = underpants.GetParamValueOrDefault(1056, 0.8);
            m_PantsWaistBump = underpants.GetParamValueOrDefault(1057, 0.8);
            underpants.Textures.TryGetValue(AvatarTextureIndex.LowerUnderpants, out m_TextureId);
            if (m_TextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_TextureId = UUID.Zero;
            }
        }

        public override bool IsBaked => m_LowerBake != null && m_LowerBump != null;

        public override WearableType Type => WearableType.Underpants;

        public override Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.LowerBody)
            {
                if(m_LowerBake != null)
                {
                    return m_LowerBake;
                }

                Image img;

                m_LowerBake = cache.TryGetTexture(m_TextureId, target, out img) ?
                    new Bitmap(img) : CreateWhiteBakeImage(target);

                InsideAlphaBlend(m_LowerBake, (rawdata) =>
                {
                    BlendAlpha(rawdata, BaseBakes.PantsLengthAlpha, m_PantsLength);
                    BlendAlpha(rawdata, BaseBakes.PantsWaistAlpha, m_PantsWaist);
                });

                return m_LowerBake;
            }
            return null;
        }

        public override ColorAlpha BakeImageColor(BakeTarget target) => (ColorAlpha)m_Color;

        public override byte[] BakeBumpOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.LowerBody)
            {
                if(m_LowerBump == null)
                {
                    if (!cache.TryGetBump(m_TextureId, target, out m_LowerBump))
                    {
                        m_LowerBump = BaseBakes.LowerBodyBump;
                    }
                    BlendBump(m_LowerBump, BaseBakes.PantsLengthAlpha, m_PantsLengthBump);
                    BlendBump(m_LowerBump, BaseBakes.PantsWaistAlpha, m_PantsWaistBump);
                }
                return m_LowerBump;
            }
            return null;
        }

        public override void Dispose()
        {
            m_LowerBake?.Dispose();
        }

        private static Color3 GetUnderpantsColor(Wearable underpants)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (underpants.Params.TryGetValue(824, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (underpants.Params.TryGetValue(825, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (underpants.Params.TryGetValue(826, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }
    }
}
