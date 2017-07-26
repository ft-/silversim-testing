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
using UUID = SilverSim.Types.UUID;

namespace SilverSim.Scene.Agent.Bakery.SubBakers.Clothing
{
    public class AlphaMaskSubBaker : AbstractSubBaker
    {
        private Image m_HairBake;
        private Image m_HeadBake;
        private Image m_UpperbodyBake;
        private Image m_LowerbodyBake;
        private Image m_EyesBake;

        private UUID m_HairTextureId;
        private UUID m_HeadTextureId;
        private UUID m_UpperbodyTextureId;
        private UUID m_LowerbodyTextureId;
        private UUID m_EyesTextureId;

        public AlphaMaskSubBaker(Wearable alpha)
        {
            if(alpha.Type != WearableType.Alpha)
            {
                throw new ArgumentException(nameof(alpha));
            }

            if(!alpha.Textures.TryGetValue(AvatarTextureIndex.EyesAlpha, out m_EyesTextureId))
            {
                m_EyesTextureId = UUID.Zero;
            }
            if (!alpha.Textures.TryGetValue(AvatarTextureIndex.HairAlpha, out m_HairTextureId))
            {
                m_HairTextureId = UUID.Zero;
            }
            if (!alpha.Textures.TryGetValue(AvatarTextureIndex.LowerAlpha, out m_LowerbodyTextureId))
            {
                m_LowerbodyTextureId = UUID.Zero;
            }
            if (!alpha.Textures.TryGetValue(AvatarTextureIndex.UpperAlpha, out m_UpperbodyTextureId))
            {
                m_UpperbodyTextureId = UUID.Zero;
            }
            if (!alpha.Textures.TryGetValue(AvatarTextureIndex.HeadAlpha, out m_HeadTextureId))
            {
                m_HeadTextureId = UUID.Zero;
            }
        }

        public override WearableType Type => throw new NotImplementedException();

        public override Image BakeAlphaMaskOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            Image img;

            switch(target)
            {
                case BakeTarget.Eyes:
                    if(m_EyesTextureId == UUID.Zero)
                    {
                        return null;
                    }

                    if(m_EyesBake != null)
                    {
                        return m_EyesBake;
                    }

                    if(cache.TryGetTexture(m_EyesTextureId, target, out img))
                    {
                        m_EyesBake = new Bitmap(img);
                    }
                    return m_EyesBake;

                case BakeTarget.Hair:
                    if (m_HairTextureId == UUID.Zero)
                    {
                        return null;
                    }

                    if (m_HairBake != null)
                    {
                        return m_HairBake;
                    }

                    if (cache.TryGetTexture(m_HairTextureId, target, out img))
                    {
                        m_HairBake = new Bitmap(img);
                    }
                    return m_HairBake;

                case BakeTarget.Head:
                    if (m_HeadTextureId == UUID.Zero)
                    {
                        return null;
                    }

                    if (m_HeadBake != null)
                    {
                        return m_HeadBake;
                    }

                    if (cache.TryGetTexture(m_HeadTextureId, target, out img))
                    {
                        m_HeadBake = new Bitmap(img);
                    }
                    return m_HeadBake;

                case BakeTarget.LowerBody:
                    if (m_LowerbodyTextureId == UUID.Zero)
                    {
                        return null;
                    }

                    if (m_LowerbodyBake != null)
                    {
                        return m_LowerbodyBake;
                    }

                    if (cache.TryGetTexture(m_LowerbodyTextureId, target, out img))
                    {
                        m_LowerbodyBake = new Bitmap(img);
                    }
                    return m_LowerbodyBake;

                case BakeTarget.UpperBody:
                    if (m_UpperbodyTextureId == UUID.Zero)
                    {
                        return null;
                    }

                    if (m_UpperbodyBake != null)
                    {
                        return m_UpperbodyBake;
                    }

                    if (cache.TryGetTexture(m_UpperbodyTextureId, target, out img))
                    {
                        m_UpperbodyBake = new Bitmap(img);
                    }
                    return m_UpperbodyBake;
            }

            return null;
        }

        public override void Dispose()
        {
            m_EyesBake?.Dispose();
            m_HairBake?.Dispose();
            m_HeadBake?.Dispose();
            m_LowerbodyBake?.Dispose();
            m_UpperbodyBake?.Dispose();
        }
    }
}
