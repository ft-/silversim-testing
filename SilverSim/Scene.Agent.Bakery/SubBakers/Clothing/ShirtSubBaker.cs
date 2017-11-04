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
    public sealed class ShirtSubBaker : AbstractSubBaker
    {
        private Bitmap m_UpperBake;
        private byte[] m_UpperBump;

        private readonly Color3 m_ShirtColor;
        private readonly UUID m_UpperTextureId;
        private readonly double m_SleeveLength;
        private readonly double m_BottomLength;
        private readonly double m_CollarFrontHeight;
        private readonly double m_CollarBackHeight;
        private readonly double m_SleeveLengthBump;
        private readonly double m_BottomLengthBump;
        private readonly double m_CollarFrontHeightBump;
        private readonly double m_CollarBackHeightBump;
        private readonly double m_Displace;

        public ShirtSubBaker(Wearable shirt)
        {
            if(shirt.Type != WearableType.Shirt)
            {
                throw new ArgumentException(nameof(shirt));
            }

            m_ShirtColor = GetShirtColor(shirt);

            shirt.Textures.TryGetValue(AvatarTextureIndex.UpperShirt, out m_UpperTextureId);
            if (m_UpperTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_UpperTextureId = UUID.Zero;
            }
            m_SleeveLength = shirt.GetParamValueOrDefault(600, 0.7);
            m_BottomLength = shirt.GetParamValueOrDefault(601, 0.8);
            m_CollarFrontHeight = shirt.GetParamValueOrDefault(602, 0.8);
            m_CollarBackHeight = shirt.GetParamValueOrDefault(778, 0.8);
            m_BottomLengthBump = shirt.GetMinParamOrDefault(0, 1014, 1030);
            m_SleeveLengthBump = shirt.GetMinParamOrDefault(0, 1013, 1029);
            m_CollarFrontHeightBump = shirt.GetMinParamOrDefault(0, 1015, 1031);
            m_CollarBackHeightBump = shirt.GetMinParamOrDefault(0, 1016, 1032);
            m_Displace = shirt.GetMinParamOrDefault(0, 628, 828);
        }

        public override bool IsBaked => m_UpperBake != null && m_UpperBump != null;

        public override WearableType Type => WearableType.Shirt;

        public override Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.UpperBody)
            {
                if(m_UpperBake == null)
                {
                    Image img;
                    m_UpperBake = cache.TryGetTexture(m_UpperTextureId, target, out img) ?
                        new Bitmap(img) : CreateWhiteBakeImage(target);

                    InsideAlphaBlend(m_UpperBake, (rawdata) =>
                    {
                        BlendAlpha(rawdata, BaseBakes.ShirtSleeveAlpha, m_SleeveLength);
                        BlendAlpha(rawdata, BaseBakes.ShirtBottomAlpha, m_BottomLength);
                        BlendAlpha(rawdata, BaseBakes.ShirtCollarFrontAlpha, m_CollarFrontHeight);
                        BlendAlpha(rawdata, BaseBakes.ShirtCollarBackAlpha, m_CollarBackHeight);
                    });
                }
                return m_UpperBake;
            }
            return null;
        }

        public override ColorAlpha BakeImageColor(BakeTarget target) => (ColorAlpha)m_ShirtColor;

        public override byte[] BakeBumpOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.UpperBody)
            {
                if(m_UpperBump == null)
                {
                    if (!cache.TryGetBump(m_UpperTextureId, target, out m_UpperBump))
                    {
                        m_UpperBump = BaseBakes.UpperBodyBump;
                        MultiplyBump(m_UpperBump, m_Displace);
                    }
                    BlendBump(m_UpperBump, BaseBakes.ShirtSleeveAlpha, m_SleeveLengthBump);
                    BlendBump(m_UpperBump, BaseBakes.ShirtBottomAlpha, m_BottomLengthBump);
                    BlendBump(m_UpperBump, BaseBakes.ShirtCollarFrontAlpha, m_CollarFrontHeightBump);
                    BlendBump(m_UpperBump, BaseBakes.ShirtCollarBackAlpha, m_CollarBackHeightBump);
                }
                return m_UpperBump;
            }
            return null;
        }

        public override void Dispose()
        {
            m_UpperBake?.Dispose();
        }

        private static Color3 GetShirtColor(Wearable shirt)
        {
            var col = new Color3(1, 1, 1);
            double val;
            if (shirt.Params.TryGetValue(803, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (shirt.Params.TryGetValue(804, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (shirt.Params.TryGetValue(805, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }
    }
}
