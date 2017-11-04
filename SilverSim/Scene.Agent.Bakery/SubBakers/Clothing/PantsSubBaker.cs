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
    public sealed class PantsSubBaker : AbstractSubBaker
    {
        private Bitmap m_PantsBake;
        private byte[] m_PantsBump;

        private readonly UUID m_PantsTextureId;
        private readonly Color3 m_PantsColor;
        private readonly double m_Length;
        private readonly double m_Waist;
        private readonly double m_LengthBump;
        private readonly double m_WaistBump;
        private readonly double m_Displace;

        public PantsSubBaker(Wearable pants)
        {
            if(pants.Type != WearableType.Pants)
            {
                throw new ArgumentException(nameof(pants));
            }

            pants.Textures.TryGetValue(AvatarTextureIndex.LowerPants, out m_PantsTextureId);
            if(m_PantsTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_PantsTextureId = UUID.Zero;
            }
            m_PantsColor = GetPantColor(pants);
            m_Length = pants.GetParamValueOrDefault(615, 0.8);
            m_Waist = pants.GetParamValueOrDefault(614, 0.8);
            m_LengthBump = pants.GetMinParamOrDefault(0, 1018, 1036);
            m_WaistBump = pants.GetMinParamOrDefault(0, 1017, 1035);
            m_Displace = pants.GetParamValueOrDefault(516, 0);
        }

        public override bool IsBaked => m_PantsBake != null && m_PantsBump != null;

        public override WearableType Type => WearableType.Pants;

        public override Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.LowerBody)
            {
                if(m_PantsBake == null)
                {
                    Image img;
                    m_PantsBake = cache.TryGetTexture(m_PantsTextureId, target, out img) ?
                        new Bitmap(img) : CreateWhiteBakeImage(target);

                    InsideAlphaBlend(m_PantsBake, (rawdata) =>
                    {
                        BlendAlpha(rawdata, BaseBakes.PantsLengthAlpha, m_Length);
                        BlendAlpha(rawdata, BaseBakes.PantsWaistAlpha, m_Waist);
                    });
                }
                return m_PantsBake;
            }
            return null;
        }

        public override ColorAlpha BakeImageColor(BakeTarget target) => (ColorAlpha)m_PantsColor;

        public override byte[] BakeBumpOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.LowerBody)
            {
                if(m_PantsBump == null)
                {
                    if (!cache.TryGetBump(m_PantsTextureId, target, out m_PantsBump))
                    {
                        m_PantsBump = BaseBakes.LowerBodyBump;
                        MultiplyBump(m_PantsBump, m_Displace);
                    }
                    BlendBump(m_PantsBump, BaseBakes.PantsLengthAlpha, m_LengthBump);
                    BlendBump(m_PantsBump, BaseBakes.PantsWaistAlpha, m_WaistBump);
                }
                return m_PantsBump;
            }
            return null;
        }

        public override void Dispose()
        {
            m_PantsBake?.Dispose();
        }

        private static Color3 GetPantColor(Wearable pant)
        {
            var col = new Color3(1, 1, 1);
            double val;
            if (pant.Params.TryGetValue(806, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (pant.Params.TryGetValue(807, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (pant.Params.TryGetValue(808, out val))
            {
                col.B = val.LimitRange(0, 1);
            }
            return col;
        }
    }
}
