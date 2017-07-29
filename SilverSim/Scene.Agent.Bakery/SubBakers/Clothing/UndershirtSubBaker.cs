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
    public sealed class UndershirtSubBaker : AbstractSubBaker
    {
        private Bitmap m_UpperBake;
        private byte[] m_UpperBump;

        private Color3 m_Color;
        private double m_SleeveLength;
        private double m_SleeveLengthBump;
        private double m_BottomLength;
        private double m_BottomLengthBump;
        private double m_CollarFront;
        private double m_CollarFrontBump;
        private double m_CollarBack;
        private double m_CollarBackBump;
        private UUID m_TextureId;

        public UndershirtSubBaker(Wearable undershirt)
        {
            if(undershirt.Type != WearableType.Undershirt)
            {
                throw new ArgumentException(nameof(undershirt));
            }
            m_Color = GetUndershirtColor(undershirt);
            m_SleeveLength = undershirt.GetParamValueOrDefault(1042, 0.4);
            m_SleeveLengthBump = undershirt.GetParamValueOrDefault(1043, 0.4);
            m_BottomLength = undershirt.GetParamValueOrDefault(1044, 0.8);
            m_BottomLengthBump = undershirt.GetParamValueOrDefault(1045, 0.8);
            m_CollarFront = undershirt.GetParamValueOrDefault(1046, 0.8);
            m_CollarFrontBump = undershirt.GetParamValueOrDefault(1047, 0.8);
            m_CollarBack = undershirt.GetParamValueOrDefault(1048, 0.8);
            m_CollarBackBump = undershirt.GetParamValueOrDefault(1049, 0.8);
            undershirt.Textures.TryGetValue(AvatarTextureIndex.UpperShirt, out m_TextureId);
        }

        public override bool IsBaked => m_UpperBake != null && m_UpperBump != null;

        public override WearableType Type => WearableType.Undershirt;

        public override Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.UpperBody)
            {
                if(m_UpperBake != null)
                {
                    return m_UpperBake;
                }

                Image img;
                m_UpperBake = cache.TryGetTexture(m_TextureId, target, out img) ?
                    new Bitmap(img) : CreateWhiteBakeImage(target);

                InsideAlphaBlend(m_UpperBake, (rawdata) =>
                {
                    BlendAlpha(rawdata, BaseBakes.ShirtSleeveAlpha, m_SleeveLength);
                    BlendAlpha(rawdata, BaseBakes.ShirtBottomAlpha, m_BottomLength);
                    BlendAlpha(rawdata, BaseBakes.ShirtCollarFrontAlpha, m_CollarFront);
                    BlendAlpha(rawdata, BaseBakes.ShirtCollarBackAlpha, m_CollarBack);
                });
                return m_UpperBake;
            }
            return null;
        }

        public override ColorAlpha BakeImageColor(BakeTarget target) => (ColorAlpha)m_Color;

        public override byte[] BakeBumpOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.UpperBody)
            {
                if(m_UpperBump == null)
                {
                    if (!cache.TryGetBump(m_TextureId, target, out m_UpperBump))
                    {
                        m_UpperBump = BaseBakes.UpperBodyBump;
                    }
                    BlendBump(m_UpperBump, BaseBakes.ShirtSleeveAlpha, m_SleeveLengthBump);
                    BlendBump(m_UpperBump, BaseBakes.ShirtBottomAlpha, m_BottomLengthBump);
                    BlendBump(m_UpperBump, BaseBakes.ShirtCollarFrontAlpha, m_CollarFrontBump);
                    BlendBump(m_UpperBump, BaseBakes.ShirtCollarBackAlpha, m_CollarBackBump);
                }
                return m_UpperBump;
            }
            return null;
        }

        public override void Dispose()
        {
            m_UpperBake?.Dispose();
        }

        private static Color3 GetUndershirtColor(Wearable undershirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (undershirt.Params.TryGetValue(821, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (undershirt.Params.TryGetValue(822, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (undershirt.Params.TryGetValue(823, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }
    }
}
