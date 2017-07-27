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

using SilverSim.Types.Asset.Format;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Color3 = SilverSim.Types.Color;
using ColorAlpha = SilverSim.Types.ColorAlpha;
using UUID = SilverSim.Types.UUID;

namespace SilverSim.Scene.Agent.Bakery.SubBakers.Clothing
{
    public sealed class TattooSubBaker : AbstractSubBaker
    {
        private Image HeadBake;
        private Image UpperBake;
        private Image LowerBake;

        /* parameters */
        private Color3 m_TattooUpperColor;
        private Color3 m_TattooLowerColor;
        private UUID m_HeadBodypaintId;
        private UUID m_UpperBodypaintId;
        private UUID m_LowerBodypaintId;

        public TattooSubBaker(Wearable tattoo)
        {
            if (tattoo.Type != WearableType.Tattoo)
            {
                throw new ArgumentException(nameof(tattoo));
            }

            m_TattooLowerColor = GetTattooLowerColor(tattoo);
            m_TattooUpperColor = GetTattooUpperColor(tattoo);
            if (tattoo.Textures.TryGetValue(SilverSim.Types.Agent.AvatarTextureIndex.HeadBodypaint, out m_HeadBodypaintId))
            {
                m_HeadBodypaintId = UUID.Zero;
            }
            if (tattoo.Textures.TryGetValue(SilverSim.Types.Agent.AvatarTextureIndex.UpperBodypaint, out m_UpperBodypaintId))
            {
                m_UpperBodypaintId = UUID.Zero;
            }
            if (tattoo.Textures.TryGetValue(SilverSim.Types.Agent.AvatarTextureIndex.LowerBodypaint, out m_LowerBodypaintId))
            {
                m_LowerBodypaintId = UUID.Zero;
            }
        }

        public override bool IsBaked => HeadBake != null && UpperBake != null && LowerBake != null;

        public override WearableType Type => WearableType.Tattoo;

        public override Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            Image img;

            switch (target)
            {
                case BakeTarget.Head:
                    if (HeadBake != null)
                    {
                        return HeadBake;
                    }

                    if (m_HeadBodypaintId != UUID.Zero && cache.TryGetTexture(m_HeadBodypaintId, target, out img))
                    {
                        HeadBake = new Bitmap(img);
                    }
                    else
                    {
                        HeadBake = CreateTargetBakeImage(target);
                        using (Graphics gfx = Graphics.FromImage(HeadBake))
                        {
                            gfx.CompositingMode = CompositingMode.SourceCopy;
                            using (var b = new SolidBrush(Color.FromArgb(0, 0, 0, 0)))
                            {
                                gfx.FillRectangle(b, GetTargetBakeDimensions(target));
                            }
                        }
                    }
                    return HeadBake;

                case BakeTarget.UpperBody:
                    if (UpperBake != null)
                    {
                        return UpperBake;
                    }

                    if (m_UpperBodypaintId != UUID.Zero && cache.TryGetTexture(m_UpperBodypaintId, target, out img))
                    {
                        UpperBake = new Bitmap(img);
                    }
                    else
                    {
                        UpperBake = CreateTargetBakeImage(target);
                        using (Graphics gfx = Graphics.FromImage(UpperBake))
                        {
                            gfx.CompositingMode = CompositingMode.SourceCopy;
                            using (var b = new SolidBrush(Color.FromArgb(0, 0, 0, 0)))
                            {
                                gfx.FillRectangle(b, GetTargetBakeDimensions(target));
                            }
                        }
                    }
                    return UpperBake;

                case BakeTarget.LowerBody:
                    if (LowerBake != null)
                    {
                        return LowerBake;
                    }

                    if (m_LowerBodypaintId != UUID.Zero && cache.TryGetTexture(m_LowerBodypaintId, target, out img))
                    {
                        LowerBake = new Bitmap(img);
                    }
                    else
                    {
                        LowerBake = CreateTargetBakeImage(target);
                        using (Graphics gfx = Graphics.FromImage(UpperBake))
                        {
                            gfx.CompositingMode = CompositingMode.SourceCopy;
                            using (var b = new SolidBrush(Color.FromArgb(0, 0, 0, 0)))
                            {
                                gfx.FillRectangle(b, GetTargetBakeDimensions(target));
                            }
                        }
                    }
                    return LowerBake;
            }

            return null;
        }

        public override ColorAlpha BakeImageColor(BakeTarget target)
        {
            switch(target)
            {
                case BakeTarget.Head:
                case BakeTarget.UpperBody:
                    return (ColorAlpha)m_TattooUpperColor;

                case BakeTarget.LowerBody:
                    return (ColorAlpha)m_TattooLowerColor;

                default:
                    return ColorAlpha.White;
            }
        }

        public override void Dispose()
        {
            HeadBake?.Dispose();
            UpperBake?.Dispose();
            LowerBake?.Dispose();
        }

        private static Color3 GetTattooUpperColor(Wearable tattoo)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (tattoo.Params.TryGetValue(1071, out val))
            {
                col.R = val;
            }
            if (tattoo.Params.TryGetValue(1072, out val))
            {
                col.G = val;
            }
            if (tattoo.Params.TryGetValue(1073, out val))
            {
                col.B = val;
            }

            if (tattoo.Params.TryGetValue(1065, out val))
            {
                col.R = val;
            }
            if (tattoo.Params.TryGetValue(1066, out val))
            {
                col.G = val;
            }
            if (tattoo.Params.TryGetValue(1067, out val))
            {
                col.B = val;
            }

            return col;
        }

        private static Color3 GetTattooLowerColor(Wearable tattoo)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (tattoo.Params.TryGetValue(1071, out val))
            {
                col.R = val;
            }
            if (tattoo.Params.TryGetValue(1072, out val))
            {
                col.G = val;
            }
            if (tattoo.Params.TryGetValue(1073, out val))
            {
                col.B = val;
            }

            if (tattoo.Params.TryGetValue(1068, out val))
            {
                col.R = val;
            }
            if (tattoo.Params.TryGetValue(1069, out val))
            {
                col.G = val;
            }
            if (tattoo.Params.TryGetValue(1070, out val))
            {
                col.B = val;
            }

            return col;
        }

    }
}
