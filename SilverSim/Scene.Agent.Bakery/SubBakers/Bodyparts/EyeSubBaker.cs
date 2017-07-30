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
using UUID = SilverSim.Types.UUID;

namespace SilverSim.Scene.Agent.Bakery.SubBakers.Bodyparts
{
    public class EyeSubBaker : AbstractSubBaker
    {
        public Image EyeBake;

        //Parameters
        private Color3 m_EyeColor;
        private UUID m_EyeTextureId;

        public EyeSubBaker(Wearable eyes)
        {
            if(eyes.Type != WearableType.Eyes)
            {
                throw new ArgumentException(nameof(eyes));
            }

            m_EyeColor = GetEyeColor(eyes);
            eyes.Textures.TryGetValue(AvatarTextureIndex.EyesIris, out m_EyeTextureId);
            if (m_EyeTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_EyeTextureId = UUID.Zero;
            }
        }

        public override bool IsBaked => EyeBake != null;

        public override WearableType Type => WearableType.Eyes;

        public override Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            if(target == BakeTarget.Eyes)
            {
                if(EyeBake != null)
                {
                    return EyeBake;
                }

                EyeBake = CreateTargetBakeImage(target);
                using (Graphics gfx = Graphics.FromImage(EyeBake))
                {
                    Rectangle bakeRectangle = GetTargetBakeDimensions(target);
                    using (var brush = new SolidBrush(Color.White))
                    {
                        gfx.FillRectangle(brush, bakeRectangle);
                    }

                    Image img;
                    if (m_EyeTextureId != UUID.Zero && cache.TryGetTexture(m_EyeTextureId, target, out img))
                    {
                        gfx.DrawTinted(bakeRectangle, img, m_EyeColor);
                    }
                }
                return EyeBake;
            }

            return null;
        }

        public override void Dispose()
        {
            EyeBake?.Dispose();
        }

        private static readonly Color3[] EyeColors =
        {
            Color3.FromRgb(50, 25, 5),
            Color3.FromRgb(109, 55, 15),
            Color3.FromRgb(150, 93, 49),
            Color3.FromRgb(152, 118, 25),
            Color3.FromRgb(95, 179, 107),
            Color3.FromRgb(87, 192, 191),
            Color3.FromRgb(95, 172, 179),
            Color3.FromRgb(128, 128, 128),
            Color3.FromRgb(0, 0, 0),
            Color3.FromRgb(255, 255, 0),
            Color3.FromRgb(0, 255, 0),
            Color3.FromRgb(0, 255, 255),
            Color3.FromRgb(0, 0, 255),
            Color3.FromRgb(255, 0, 255),
            Color3.FromRgb(255, 0, 0)
        };

        private static Color3 GetEyeColor(Wearable eyes)
        {
            var col = new Color3(0, 0, 0);
            double val;
            if (eyes.Params.TryGetValue(99, out val))
            {
                col += CalcColor(val, EyeColors);
            }
            if (eyes.Params.TryGetValue(98, out val))
            {
                col += new Color3(val, val, val);
            }
            return col;
        }
    }
}
