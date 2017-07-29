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
    public sealed class JacketSubBaker : AbstractSubBaker
    {
        private Bitmap m_UpperBake;
        private Bitmap m_LowerBake;
        private byte[] m_UpperBump;
        private byte[] m_LowerBump;

        private UUID m_UpperTextureId;
        private UUID m_LowerTextureId;

        private double m_JacketUpperLength;
        private double m_JacketLowerLength;
        private double m_JacketUpperOpen;
        private double m_JacketLowerOpen;
        private double m_JacketSleeveLength;
        private double m_JacketCollarFront;
        private double m_JacketCollarBack;

        private double m_JacketUpperLengthBump;
        private double m_JacketLowerLengthBump;
        private double m_JacketUpperOpenBump;
        private double m_JacketLowerOpenBump;
        private double m_JacketSleeveLengthBump;
        private double m_JacketCollarFrontBump;
        private double m_JacketCollarBackBump;

        private Color3 m_JacketUpperColor;
        private Color3 m_JacketLowerColor;

        public JacketSubBaker(Wearable jacket)
        {
            if(jacket.Type != WearableType.Jacket)
            {
                throw new ArgumentException(nameof(jacket));
            }

            m_JacketUpperOpen = jacket.GetParamValueOrDefault(622, 0.8);
            m_JacketLowerOpen = jacket.GetParamValueOrDefault(623, 0.8);
            m_JacketUpperLength = jacket.GetParamValueOrDefault(620, 0.8);
            m_JacketLowerLength = jacket.GetParamValueOrDefault(621, 0.8);
            m_JacketSleeveLength = jacket.GetParamValueOrDefault(1020, 0);
            m_JacketCollarFront = jacket.GetParamValueOrDefault(1022, 0);
            m_JacketCollarBack = jacket.GetParamValueOrDefault(1024, 0);
            m_JacketUpperColor = GetJacketUpperColor(jacket);
            m_JacketLowerColor = GetJacketLowerColor(jacket);

            m_JacketSleeveLengthBump = jacket.GetParamValueOrDefault(1019, 0);
            m_JacketCollarFrontBump = jacket.GetParamValueOrDefault(1021, 0);
            m_JacketCollarBackBump = jacket.GetParamValueOrDefault(1023, 0);
            m_JacketLowerLengthBump = jacket.GetMinParamOrDefault(0, 1033, 1025);
            m_JacketUpperLengthBump = jacket.GetMinParamOrDefault(0, 1037, 1027);
            m_JacketUpperOpenBump = jacket.GetMinParamOrDefault(0, 1026, 1038);
            m_JacketLowerOpenBump = jacket.GetMinParamOrDefault(0, 1028, 1034);

            jacket.Textures.TryGetValue(AvatarTextureIndex.UpperJacket, out m_UpperTextureId);
            jacket.Textures.TryGetValue(AvatarTextureIndex.LowerJacket, out m_LowerTextureId);
        }


        public override Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            Image img;
            switch (target)
            {
                case BakeTarget.UpperBody:
                    if(m_UpperBake != null)
                    {
                        return m_UpperBake;
                    }

                    m_UpperBake = cache.TryGetTexture(m_UpperTextureId, target, out img) ?
                        new Bitmap(img) : CreateWhiteBakeImage(target);

                    InsideAlphaBlend(m_UpperBake, (rawdata) =>
                    {
                        BlendAlpha(rawdata, BaseBakes.JacketLengthUpperAlpha, m_JacketUpperLength);
                        BlendAlpha(rawdata, BaseBakes.JacketOpenUpperAlpha, m_JacketUpperOpen);
                        BlendAlpha(rawdata, BaseBakes.ShirtSleeveAlpha, m_JacketSleeveLength);
                        BlendAlpha(rawdata, BaseBakes.ShirtCollarFrontAlpha, m_JacketCollarFront);
                        BlendAlpha(rawdata, BaseBakes.ShirtCollarBackAlpha, m_JacketCollarBack);
                    });
                    break;

                case BakeTarget.LowerBody:
                    if (m_LowerBake != null)
                    {
                        return m_LowerBake;
                    }

                    m_LowerBake = cache.TryGetTexture(m_LowerTextureId, target, out img) ?
                        new Bitmap(img) : CreateWhiteBakeImage(target);

                    InsideAlphaBlend(m_UpperBake, (rawdata) =>
                    {
                        BlendAlpha(rawdata, BaseBakes.JacketLengthLowerAlpha, m_JacketLowerLength);
                        BlendAlpha(rawdata, BaseBakes.JacketOpenLowerAlpha, m_JacketLowerOpen);
                    });
                    break;
            }
            return null;
        }

        public override ColorAlpha BakeImageColor(BakeTarget target)
        {
            switch(target)
            {
                case BakeTarget.UpperBody:
                    return (ColorAlpha)m_JacketUpperColor;

                case BakeTarget.LowerBody:
                    return (ColorAlpha)m_JacketLowerColor;

                default:
                    return ColorAlpha.White;
            }
        }

        public override byte[] BakeBumpOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            switch(target)
            {
                case BakeTarget.UpperBody:
                    if (m_UpperBump == null)
                    {
                        if (!cache.TryGetBump(m_UpperTextureId, target, out m_UpperBump))
                        {
                            m_UpperBump = BaseBakes.UpperBodyBump;
                        }
                        BlendBump(m_UpperBump, BaseBakes.JacketLengthUpperAlpha, m_JacketUpperLengthBump);
                        BlendBump(m_UpperBump, BaseBakes.JacketOpenUpperAlpha, m_JacketUpperOpenBump);
                        BlendBump(m_UpperBump, BaseBakes.ShirtSleeveAlpha, m_JacketSleeveLengthBump);
                        BlendBump(m_UpperBump, BaseBakes.ShirtCollarFrontAlpha, m_JacketCollarFrontBump);
                        BlendBump(m_UpperBump, BaseBakes.ShirtCollarBackAlpha, m_JacketCollarBackBump);
                    }
                    return m_UpperBump;

                case BakeTarget.LowerBody:
                    if (m_LowerBump == null)
                    {
                        if (!cache.TryGetBump(m_LowerTextureId, target, out m_LowerBump))
                        {
                            m_LowerBump = BaseBakes.LowerBodyBump;
                        }
                        BlendBump(m_LowerBump, BaseBakes.JacketLengthLowerAlpha, m_JacketLowerLengthBump);
                        BlendBump(m_LowerBump, BaseBakes.JacketOpenUpperAlpha, m_JacketLowerOpenBump);
                    }
                    return m_LowerBump;

                default:
                    return null;
            }
        }

        public override bool IsBaked => m_UpperBake != null && m_LowerBake != null && m_UpperBump != null && m_LowerBump != null;

        public override WearableType Type => WearableType.Jacket;

        public override void Dispose()
        {
            m_UpperBake?.Dispose();
            m_LowerBake?.Dispose();
        }

        private static Color3 GetJacketUpperColor(Wearable jacket)
        {
            var col = new Color3(1, 1, 1);
            double val;
            if (jacket.Params.TryGetValue(831, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (jacket.Params.TryGetValue(832, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (jacket.Params.TryGetValue(833, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }

        private static Color3 GetJacketLowerColor(Wearable jacket)
        {
            var col = new Color3(1, 1, 1);
            double val;
            if (jacket.Params.TryGetValue(809, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (jacket.Params.TryGetValue(810, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (jacket.Params.TryGetValue(811, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }
    }
}
