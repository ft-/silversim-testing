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
    public sealed class SocksSubBaker : AbstractSubBaker
    {
        private Bitmap m_LowerBake;
        private byte[] m_LowerBump;

        private UUID m_TextureId;
        private Color3 m_Color;
        private double m_SocksLength;
        private double m_SocksLengthBump;

        public SocksSubBaker(Wearable socks)
        {
            if(socks.Type != WearableType.Socks)
            {
                throw new ArgumentException(nameof(socks));
            }

            m_Color = GetSocksColor(socks);
            m_SocksLength = socks.GetParamValueOrDefault(617, 0.35);
            m_SocksLengthBump = socks.GetMinParamOrDefault(0.35, 1050, 1051);
            socks.Textures.TryGetValue(AvatarTextureIndex.LowerSocks, out m_TextureId);
        }

        public override bool IsBaked => false;

        public override WearableType Type => WearableType.Socks;

        public override Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if (target == BakeTarget.LowerBody)
            {
                if (m_LowerBake == null)
                {
                    Image img;

                    m_LowerBake = m_TextureId != UUID.Zero && cache.TryGetTexture(m_TextureId, target, out img) ?
                        new Bitmap(img) : CreateWhiteBakeImage(target);

                    InsideAlphaBlend(m_LowerBake, (rawdata) =>
                    {
                        BlendAlpha(rawdata, BaseBakes.ShoeHeightAlpha, m_SocksLength);
                    });
                }

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
                    BlendBump(m_LowerBump, BaseBakes.ShoeHeightAlpha, m_SocksLengthBump);
                }
                return m_LowerBump;
            }
            return null;
        }

        public override void Dispose()
        {
            m_LowerBake?.Dispose();
        }

        private static Color3 GetSocksColor(Wearable socks)
        {
            var col = new Color3(1, 1, 1);
            double val;
            if (socks.Params.TryGetValue(818, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (socks.Params.TryGetValue(819, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (socks.Params.TryGetValue(820, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }
    }
}
