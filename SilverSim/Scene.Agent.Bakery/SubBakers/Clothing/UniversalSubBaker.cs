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
    public class UniversalSubBaker : AbstractSubBaker
    {
        private Bitmap m_HairBake;
        private Bitmap m_HeadBake;
        private Bitmap m_UpperBake;
        private Bitmap m_LowerBake;
        private Bitmap m_EyesBake;
        private Bitmap m_SkirtBake;
        private Bitmap m_LeftArmBake;
        private Bitmap m_LeftLegBake;
        private Bitmap m_Aux1Bake;
        private Bitmap m_Aux2Bake;
        private Bitmap m_Aux3Bake;

        private readonly UUID m_TattooHairTextureId;
        private readonly Color3 m_TattooHairColor;

        private readonly UUID m_TattooHeadTextureId;
        private readonly Color3 m_TattooHeadColor;

        private readonly UUID m_TattooUpperTextureId;
        private readonly Color3 m_TattooUpperColor;

        private readonly UUID m_TattooLowerTextureId;
        private readonly Color3 m_TattooLowerColor;

        private readonly UUID m_TattooEyesTextureId;
        private readonly Color3 m_TattooEyesColor;

        private readonly UUID m_TattooSkirtTextureId;
        private readonly Color3 m_TattooSkirtColor;

        private readonly UUID m_TattooLeftArmTextureId;
        private readonly Color3 m_TattooLeftArmColor;

        private readonly UUID m_TattooLeftLegTextureId;
        private readonly Color3 m_TattooLeftLegColor;

        private readonly UUID m_TattooAux1TextureId;
        private readonly Color3 m_TattooAux1Color;

        private readonly UUID m_TattooAux2TextureId;
        private readonly Color3 m_TattooAux2Color;

        private readonly UUID m_TattooAux3TextureId;
        private readonly Color3 m_TattooAux3Color;

        public UniversalSubBaker(Wearable universal)
        {
            if (universal.Type != WearableType.Universal)
            {
                throw new ArgumentException(nameof(universal));
            }
            m_TattooHairColor = GetTattooHairColor(universal);
            universal.Textures.TryGetValue(AvatarTextureIndex.HairTattoo, out m_TattooHairTextureId);
            if (m_TattooHairTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_TattooHairTextureId = UUID.Zero;
            }

            m_TattooHeadColor = GetTattooHeadColor(universal);
            universal.Textures.TryGetValue(AvatarTextureIndex.HeadUniversalTattoo, out m_TattooHeadTextureId);
            if (m_TattooHeadTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_TattooHeadTextureId = UUID.Zero;
            }

            m_TattooUpperColor = GetTattooUpperColor(universal);
            universal.Textures.TryGetValue(AvatarTextureIndex.UpperUniversalTattoo, out m_TattooUpperTextureId);
            if (m_TattooUpperTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_TattooUpperTextureId = UUID.Zero;
            }

            m_TattooLowerColor = GetTattooLowerColor(universal);
            universal.Textures.TryGetValue(AvatarTextureIndex.LowerUniversalTattoo, out m_TattooLowerTextureId);
            if (m_TattooLowerTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_TattooLowerTextureId = UUID.Zero;
            }

            m_TattooEyesColor = GetTattooEyesColor(universal);
            universal.Textures.TryGetValue(AvatarTextureIndex.EyesTattoo, out m_TattooEyesTextureId);
            if (m_TattooEyesTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_TattooEyesTextureId = UUID.Zero;
            }

            m_TattooSkirtColor = GetTattooSkirtColor(universal);
            universal.Textures.TryGetValue(AvatarTextureIndex.SkirtTattoo, out m_TattooSkirtTextureId);
            if (m_TattooSkirtTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_TattooSkirtTextureId = UUID.Zero;
            }

            m_TattooLeftArmColor = GetTattooLeftArmColor(universal);
            universal.Textures.TryGetValue(AvatarTextureIndex.LeftArmTattoo, out m_TattooLeftArmTextureId);
            if (m_TattooLeftArmTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_TattooLeftArmTextureId = UUID.Zero;
            }

            m_TattooLeftLegColor = GetTattooLeftLegColor(universal);
            universal.Textures.TryGetValue(AvatarTextureIndex.LeftLegTattoo, out m_TattooLeftLegTextureId);
            if (m_TattooLeftLegTextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_TattooLeftLegTextureId = UUID.Zero;
            }

            m_TattooAux1Color = GetTattooAux1Color(universal);
            universal.Textures.TryGetValue(AvatarTextureIndex.Aux1Tattoo, out m_TattooAux1TextureId);
            if (m_TattooAux1TextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_TattooAux1TextureId = UUID.Zero;
            }

            m_TattooAux2Color = GetTattooAux2Color(universal);
            universal.Textures.TryGetValue(AvatarTextureIndex.Aux2Tattoo, out m_TattooAux2TextureId);
            if (m_TattooAux2TextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_TattooAux2TextureId = UUID.Zero;
            }

            m_TattooAux3Color = GetTattooAux3Color(universal);
            universal.Textures.TryGetValue(AvatarTextureIndex.Aux3Tattoo, out m_TattooAux3TextureId);
            if (m_TattooAux3TextureId == AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID)
            {
                m_TattooAux3TextureId = UUID.Zero;
            }
        }

        public override bool IsBaked => m_HairBake != null && m_HeadBake != null && m_UpperBake != null && m_LowerBake != null && m_EyesBake != null && m_SkirtBake != null && m_LeftArmBake != null && m_LeftLegBake != null && m_Aux1Bake != null && m_Aux2Bake != null && m_Aux3Bake != null;

        public override WearableType Type => WearableType.Universal;

        public override Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target)
        {
            switch(target)
            {
                case BakeTarget.Hair:
                    if (m_HairBake == null)
                    {
                        Image img;
                        m_HairBake = cache.TryGetTexture(m_TattooHairTextureId, target, out img) ?
                            new Bitmap(img) : CreateWhiteBakeImage(target);
                    }
                    return m_HairBake;

                case BakeTarget.Head:
                    if(m_HeadBake == null)
                    {
                        Image img;
                        m_HeadBake = cache.TryGetTexture(m_TattooHeadTextureId, target, out img) ?
                            new Bitmap(img) : CreateWhiteBakeImage(target);
                    }
                    return m_HeadBake;

                case BakeTarget.UpperBody:
                    if (m_UpperBake == null)
                    {
                        Image img;
                        m_UpperBake = cache.TryGetTexture(m_TattooUpperTextureId, target, out img) ?
                            new Bitmap(img) : CreateWhiteBakeImage(target);
                    }
                    return m_UpperBake;

                case BakeTarget.LowerBody:
                    if (m_LowerBake == null)
                    {
                        Image img;
                        m_LowerBake = cache.TryGetTexture(m_TattooLowerTextureId, target, out img) ?
                            new Bitmap(img) : CreateWhiteBakeImage(target);
                    }
                    return m_LowerBake;

                case BakeTarget.Eyes:
                    if (m_EyesBake == null)
                    {
                        Image img;
                        m_EyesBake = cache.TryGetTexture(m_TattooEyesTextureId, target, out img) ?
                            new Bitmap(img) : CreateWhiteBakeImage(target);
                    }
                    return m_EyesBake;

                case BakeTarget.Skirt:
                    if (m_SkirtBake == null)
                    {
                        Image img;
                        m_SkirtBake = cache.TryGetTexture(m_TattooSkirtTextureId, target, out img) ?
                            new Bitmap(img) : CreateWhiteBakeImage(target);
                    }
                    return m_SkirtBake;

                case BakeTarget.LeftArm:
                    if (m_LeftArmBake == null)
                    {
                        Image img;
                        m_LeftArmBake = cache.TryGetTexture(m_TattooLeftArmTextureId, target, out img) ?
                            new Bitmap(img) : CreateWhiteBakeImage(target);
                    }
                    return m_LeftArmBake;

                case BakeTarget.LeftLeg:
                    if (m_LeftLegBake == null)
                    {
                        Image img;
                        m_LeftLegBake = cache.TryGetTexture(m_TattooLeftLegTextureId, target, out img) ?
                            new Bitmap(img) : CreateWhiteBakeImage(target);
                    }
                    return m_LeftLegBake;

                case BakeTarget.Aux1:
                    if (m_Aux1Bake == null)
                    {
                        Image img;
                        m_Aux1Bake = cache.TryGetTexture(m_TattooAux1TextureId, target, out img) ?
                            new Bitmap(img) : CreateWhiteBakeImage(target);
                    }
                    return m_Aux1Bake;

                case BakeTarget.Aux2:
                    if (m_Aux2Bake == null)
                    {
                        Image img;
                        m_Aux2Bake = cache.TryGetTexture(m_TattooAux2TextureId, target, out img) ?
                            new Bitmap(img) : CreateWhiteBakeImage(target);
                    }
                    return m_Aux2Bake;

                case BakeTarget.Aux3:
                    if (m_Aux3Bake == null)
                    {
                        Image img;
                        m_Aux3Bake = cache.TryGetTexture(m_TattooAux3TextureId, target, out img) ?
                            new Bitmap(img) : CreateWhiteBakeImage(target);
                    }
                    return m_Aux3Bake;
            }
            return null;
        }

        public override ColorAlpha BakeImageColor(BakeTarget target)
        {
            switch (target)
            {
                case BakeTarget.Hair:
                    return (ColorAlpha)m_TattooHairColor;

                case BakeTarget.Head:
                    return (ColorAlpha)m_TattooHeadColor;

                case BakeTarget.UpperBody:
                    return (ColorAlpha)m_TattooUpperColor;

                case BakeTarget.LowerBody:
                    return (ColorAlpha)m_TattooLowerColor;

                case BakeTarget.Eyes:
                    return (ColorAlpha)m_TattooEyesColor;

                case BakeTarget.Skirt:
                    return (ColorAlpha)m_TattooSkirtColor;

                case BakeTarget.LeftArm:
                    return (ColorAlpha)m_TattooLeftArmColor;

                case BakeTarget.LeftLeg:
                    return (ColorAlpha)m_TattooLeftLegColor;

                case BakeTarget.Aux1:
                    return (ColorAlpha)m_TattooAux1Color;

                case BakeTarget.Aux2:
                    return (ColorAlpha)m_TattooAux2Color;

                case BakeTarget.Aux3:
                    return (ColorAlpha)m_TattooAux3Color;
            }
            return ColorAlpha.White;
        }

        public override byte[] BakeBumpOutput(IBakeTextureInputCache cache, BakeTarget target) => null;

        public override void Dispose()
        {
            m_HairBake?.Dispose();
            m_HeadBake?.Dispose();
            m_UpperBake?.Dispose();
            m_LowerBake?.Dispose();
            m_EyesBake?.Dispose();
            m_SkirtBake?.Dispose();
            m_LeftArmBake?.Dispose();
            m_LeftLegBake?.Dispose();
            m_Aux1Bake?.Dispose();
            m_Aux2Bake?.Dispose();
            m_Aux3Bake?.Dispose();
        }

        private static Color3 GetTattooHairColor(Wearable skirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (skirt.Params.TryGetValue(1211, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1212, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1213, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }

        private static Color3 GetTattooHeadColor(Wearable skirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (skirt.Params.TryGetValue(1229, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1230, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1231, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }

        private static Color3 GetTattooUpperColor(Wearable skirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (skirt.Params.TryGetValue(1232, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1233, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1234, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }

        private static Color3 GetTattooLowerColor(Wearable skirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (skirt.Params.TryGetValue(1235, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1236, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1237, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }

        private static Color3 GetTattooEyesColor(Wearable skirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (skirt.Params.TryGetValue(924, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(925, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(926, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }

        private static Color3 GetTattooSkirtColor(Wearable skirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (skirt.Params.TryGetValue(1208, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1209, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1230, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }

        private static Color3 GetTattooLeftArmColor(Wearable skirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (skirt.Params.TryGetValue(1214, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1215, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1216, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }

        private static Color3 GetTattooLeftLegColor(Wearable skirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (skirt.Params.TryGetValue(1217, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1218, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1219, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }

        private static Color3 GetTattooAux1Color(Wearable skirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (skirt.Params.TryGetValue(1220, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1221, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1222, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }

        private static Color3 GetTattooAux2Color(Wearable skirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (skirt.Params.TryGetValue(1223, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1224, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1225, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }

        private static Color3 GetTattooAux3Color(Wearable skirt)
        {
            var col = new Color3(1, 1, 1);
            double val;

            if (skirt.Params.TryGetValue(1226, out val))
            {
                col.R = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1227, out val))
            {
                col.G = val.LimitRange(0, 1);
            }
            if (skirt.Params.TryGetValue(1228, out val))
            {
                col.B = val.LimitRange(0, 1);
            }

            return col;
        }
    }
}
