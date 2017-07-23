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

using OpenJp2.Net;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SilverSim.Scene.Types.Agent
{
    public static partial class AgentBakeAppearance
    {
        #region Actual Baking Code
        private const int MAX_WEARABLES_PER_TYPE = 5;

        private static System.Drawing.Color GetTint(Wearable w, BakeType bType)
        {
            var wColor = new SilverSim.Types.Color(1, 1, 1);
            double val;
            switch (w.Type)
            {
                case WearableType.Tattoo:
                    if (w.Params.TryGetValue(1071, out val))
                    {
                        wColor.R = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(1072, out val))
                    {
                        wColor.G = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(1073, out val))
                    {
                        wColor.B = val.Clamp(0, 1);
                    }
                    switch (bType)
                    {
                        case BakeType.Head:
                            if (w.Params.TryGetValue(1062, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1063, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1064, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;
                        case BakeType.UpperBody:
                            if (w.Params.TryGetValue(1065, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1066, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1067, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;
                        case BakeType.LowerBody:
                            if (w.Params.TryGetValue(1068, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1069, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(1070, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;

                        default:
                            break;
                    }
                    break;

                case WearableType.Jacket:
                    if (w.Params.TryGetValue(834, out val))
                    {
                        wColor.R = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(835, out val))
                    {
                        wColor.G = val.Clamp(0, 1);
                    }
                    if (w.Params.TryGetValue(836, out val))
                    {
                        wColor.B = val.Clamp(0, 1);
                    }
                    switch (bType)
                    {
                        case BakeType.UpperBody:
                            if (w.Params.TryGetValue(831, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(832, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(833, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;
                        case BakeType.LowerBody:
                            if (w.Params.TryGetValue(809, out val))
                            {
                                wColor.R = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(810, out val))
                            {
                                wColor.G = val.Clamp(0, 1);
                            }
                            if (w.Params.TryGetValue(811, out val))
                            {
                                wColor.B = val.Clamp(0, 1);
                            }
                            break;

                        default:
                            break;
                    }
                    break;

                default:
                    wColor = w.GetTint();
                    break;
            }

            return System.Drawing.Color.FromArgb(wColor.R_AsByte, wColor.G_AsByte, wColor.B_AsByte);
        }

        private static AssetData BakeTexture(BakeStatus status, BakeType bake)
        {
            int bakeDimensions = (bake == BakeType.Eyes) ? 128 : 512;
            var data = new AssetData()
            {
                ID = UUID.RandomFixedFirst(0xFFFFFFFF),
                Type = AssetType.Texture,
                Local = true,
                Temporary = true,
                Flags = AssetFlags.Collectable | AssetFlags.Rewritable
            };

            AvatarTextureIndex[] bakeProcessTable;
            List<Image> alphaCompositeInputs = new List<Image>();

            switch (bake)
            {
                case BakeType.Head:
                    alphaCompositeInputs.Add(BaseBakes.HeadAlpha);
                    bakeProcessTable = IndexesForBakeHead;
                    data.Name = "Baked Head Texture";
                    break;

                case BakeType.Eyes:
                    bakeProcessTable = IndexesForBakeEyes;
                    data.Name = "Baked Eyes Texture";
                    break;

                case BakeType.Hair:
                    bakeProcessTable = IndexesForBakeHair;
                    data.Name = "Baked Hair Texture";
                    break;

                case BakeType.LowerBody:
                    bakeProcessTable = IndexesForBakeLowerBody;
                    data.Name = "Baked Lower Body Texture";
                    break;

                case BakeType.UpperBody:
                    bakeProcessTable = IndexesForBakeUpperBody;
                    data.Name = "Baked Upper Body Texture";
                    break;

                case BakeType.Skirt:
                    bakeProcessTable = IndexesForBakeSkirt;
                    data.Name = "Baked Skirt Texture";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(bake));
            }

            Rectangle bakeRectangle = new Rectangle(0, 0, bakeDimensions, bakeDimensions);
            using (var bitmap = new Bitmap(bakeDimensions, bakeDimensions, PixelFormat.Format32bppArgb))
            {
                using (var gfx = Graphics.FromImage(bitmap))
                {

                    /* alpha blending is enabled by changing the compositing mode of the graphics object */
                    gfx.CompositingMode = CompositingMode.SourceOver;

                    if (bake == BakeType.Eyes)
                    {
                        /* eyes have white base texture */
                        using (var brush = new SolidBrush(System.Drawing.Color.White))
                        {
                            gfx.FillRectangle(brush, bakeRectangle);
                        }
                    }
                    else
                    {
                        using (var brush = new SolidBrush(status.SkinColor.ToDrawing()))
                        {
                            gfx.FillRectangle(brush, bakeRectangle);
                        }

                        Image baseBake;
                        switch (bake)
                        {
                            case BakeType.Head:
                                baseBake = BaseBakes.HeadColorAndSkinGrain;
                                gfx.DrawImage(baseBake, bakeRectangle, 0, 0, baseBake.Width, baseBake.Height, GraphicsUnit.Pixel);
                                break;

                            case BakeType.UpperBody:
                                baseBake = BaseBakes.UpperBodyColor;
                                gfx.DrawImage(baseBake, bakeRectangle, 0, 0, baseBake.Width, baseBake.Height, GraphicsUnit.Pixel);
                                break;

                            case BakeType.LowerBody:
                                baseBake = BaseBakes.LowerBodyColor;
                                gfx.DrawImage(baseBake, bakeRectangle, 0, 0, baseBake.Width, baseBake.Height, GraphicsUnit.Pixel);
                                break;

                            default:
                                break;
                        }
                    }

                    foreach (var texIndex in bakeProcessTable)
                    {
                        foreach (var item in status.OutfitItems.Values)
                        {
                            UUID texture;
                            Image img;
                            if ((item.WearableData != null && item.WearableData.Textures.TryGetValue(texIndex, out texture)) &&
                                status.TryGetTexture(bake, texture, out img))
                            {
                                switch (texIndex)
                                {
                                    case AvatarTextureIndex.HeadBodypaint:
                                    case AvatarTextureIndex.UpperBodypaint:
                                    case AvatarTextureIndex.LowerBodypaint:
                                        /* no tinting here */
                                        gfx.DrawImage(img, new Rectangle(0, 0, bakeDimensions, bakeDimensions), 0, 0, bakeDimensions, bakeDimensions, GraphicsUnit.Pixel);
                                        break;

                                    case AvatarTextureIndex.LowerAlpha:
                                    case AvatarTextureIndex.UpperAlpha:
                                    case AvatarTextureIndex.HeadAlpha:
                                    case AvatarTextureIndex.HairAlpha:
                                    case AvatarTextureIndex.EyesAlpha:
                                        alphaCompositeInputs.Add(img);
                                        break;

                                    default:
                                        using (ImageAttributes attrs = new ImageAttributes())
                                        {
                                            ColorMatrix mat = new ColorMatrix();
                                            mat.ApplyTint(item.WearableData.GetTint());
                                            attrs.SetColorMatrix(mat);
                                            gfx.DrawImage(img, new Rectangle(0, 0, bakeDimensions, bakeDimensions), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attrs);
                                        }
                                        break;
                                }

                            }
                        }
                    }
                }

                /* Alpha baking */
                if (alphaCompositeInputs.Count != 0)
                {
                    BitmapData bmpData = bitmap.LockBits(bakeRectangle, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                    int scanLineBytes = bakeDimensions * bakeDimensions * 4;
                    byte[] dstBmpBytes = new byte[scanLineBytes];
                    byte[] srcBmpBytes = new byte[scanLineBytes];
                    Marshal.Copy(bmpData.Scan0, dstBmpBytes, 0, scanLineBytes);

                    foreach (Image alphaimg in alphaCompositeInputs)
                    {
                        using (Bitmap resizedalphaimg = new Bitmap(alphaimg, bakeDimensions, bakeDimensions))
                        {
                            BitmapData srcBmpData = resizedalphaimg.LockBits(bakeRectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                            Marshal.Copy(srcBmpData.Scan0, srcBmpBytes, 0, scanLineBytes);

                            for (int i = scanLineBytes; i-- != 0;)
                            {
                                dstBmpBytes[i] = Math.Min(dstBmpBytes[i], srcBmpBytes[i]);

                                /* skip RGB */
                                i -= 3;
                            }

                            resizedalphaimg.UnlockBits(srcBmpData);
                        }
                    }

                    Marshal.Copy(dstBmpBytes, 0, bmpData.Scan0, scanLineBytes);

                    bitmap.UnlockBits(bmpData);
                }

                data.Data = J2cEncoder.Encode(bitmap, true);
            }

            return data;
        }

        private static readonly AvatarTextureIndex[] IndexesForBakeHead = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.HeadAlpha,
            AvatarTextureIndex.HeadBodypaint,
            AvatarTextureIndex.HeadTattoo
        };

        private static readonly AvatarTextureIndex[] IndexesForBakeUpperBody = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.UpperBodypaint,
            AvatarTextureIndex.UpperGloves,
            AvatarTextureIndex.UpperUndershirt,
            AvatarTextureIndex.UpperShirt,
            AvatarTextureIndex.UpperJacket,
            AvatarTextureIndex.UpperAlpha
        };

        private static readonly AvatarTextureIndex[] IndexesForBakeLowerBody = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.LowerBodypaint,
            AvatarTextureIndex.LowerUnderpants,
            AvatarTextureIndex.LowerSocks,
            AvatarTextureIndex.LowerShoes,
            AvatarTextureIndex.LowerPants,
            AvatarTextureIndex.LowerJacket,
            AvatarTextureIndex.LowerAlpha
        };

        private static readonly AvatarTextureIndex[] IndexesForBakeEyes = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.EyesIris,
            AvatarTextureIndex.EyesAlpha
        };

        private static readonly AvatarTextureIndex[] IndexesForBakeHair = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.Hair,
            AvatarTextureIndex.HairAlpha
        };

        private static readonly AvatarTextureIndex[] IndexesForBakeSkirt = new AvatarTextureIndex[]
        {
            AvatarTextureIndex.Skirt
        };

        public static object Owner { get; }

        private static void CoreBakeLogic(this AppearanceInfo appearance, BakeStatus bakeStatus, AssetServiceInterface sceneAssetService)
        {
            for (int idx = 0; idx < AppearanceInfo.AvatarTextureData.TextureCount; ++idx)
            {
                appearance.AvatarTextures[idx] = AppearanceInfo.AvatarTextureData.DefaultAvatarTextureID;
            }
            foreach (OutfitItem item in bakeStatus.OutfitItems.Values)
            {
                foreach (KeyValuePair<AvatarTextureIndex, UUID> tex in item.WearableData.Textures)
                {
                    appearance.AvatarTextures[(int)tex.Key] = tex.Value;
                }
                if(item.WearableData.Type == WearableType.Skin)
                {
                    bakeStatus.SkinColor = item.WearableData.GetTint();
                }
            }


            var bakeHead = BakeTexture(bakeStatus, BakeType.Head);
            var bakeUpperBody = BakeTexture(bakeStatus, BakeType.UpperBody);
            var bakeLowerBody = BakeTexture(bakeStatus, BakeType.LowerBody);
            var bakeEyes = BakeTexture(bakeStatus, BakeType.Eyes);
            var bakeHair = BakeTexture(bakeStatus, BakeType.Hair);
            AssetData bakeSkirt = null;

            var haveSkirt = false;
            foreach (var item in bakeStatus.OutfitItems.Values)
            {
                if (item.WearableData?.Type == WearableType.Skirt)
                {
                    haveSkirt = true;
                    break;
                }
            }

            if (haveSkirt)
            {
                bakeSkirt = BakeTexture(bakeStatus, BakeType.Skirt);
            }

            sceneAssetService.Store(bakeEyes);
            sceneAssetService.Store(bakeHead);
            sceneAssetService.Store(bakeUpperBody);
            sceneAssetService.Store(bakeLowerBody);
            sceneAssetService.Store(bakeHair);
            if (bakeSkirt != null)
            {
                sceneAssetService.Store(bakeSkirt);
            }

            appearance.AvatarTextures[(int)AvatarTextureIndex.EyesBaked] = bakeEyes.ID;
            appearance.AvatarTextures[(int)AvatarTextureIndex.HeadBaked] = bakeHead.ID;
            appearance.AvatarTextures[(int)AvatarTextureIndex.UpperBaked] = bakeUpperBody.ID;
            appearance.AvatarTextures[(int)AvatarTextureIndex.LowerBaked] = bakeLowerBody.ID;
            appearance.AvatarTextures[(int)AvatarTextureIndex.HairBaked] = bakeHair.ID;
            if (bakeSkirt != null)
            {
                appearance.AvatarTextures[(int)AvatarTextureIndex.SkirtBaked] = bakeSkirt.ID;
            }
        }

        #endregion

        #region Base Bake textures
        private static class BaseBakes
        {
            public static readonly Image HeadAlpha;
            public static readonly Image HeadColor;
            public static readonly Image HeadHair;
            public static readonly Image HeadSkinGrain;
            public static readonly Image LowerBodyColor;
            public static readonly Image UpperBodyColor;
            public static readonly Image HeadColorAndSkinGrain;

            static BaseBakes()
            {
                HeadAlpha = LoadResourceImage("head_alpha.png");
                HeadColor = LoadResourceImage("head_color.png");
                HeadHair = LoadResourceImage("head_hair.png");
                HeadSkinGrain = LoadResourceImage("head_skingrain.png");
                LowerBodyColor = LoadResourceImage("lowerbody_color.png");
                UpperBodyColor = LoadResourceImage("upperbody_color.png");
                Bitmap bmp = new Bitmap(HeadColor);
                BitmapData outLockBits = bmp.LockBits(new Rectangle(0, 0, 512, 512), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                byte[] inData = new byte[512 * 512 * 4];
                byte[] outData = new byte[512 * 512 * 4];
                byte[] alphaData = new byte[512 * 512 * 4];
                using (Bitmap headAlpha = new Bitmap(HeadAlpha))
                {
                    BitmapData inLockBits = headAlpha.LockBits(new Rectangle(0, 0, 512, 512), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(inLockBits.Scan0, alphaData, 0, 512 * 512 * 4);
                    headAlpha.UnlockBits(inLockBits);
                }
                using (Bitmap headSkinGrain = new Bitmap(HeadSkinGrain))
                {
                    BitmapData inLockBits = headSkinGrain.LockBits(new Rectangle(0, 0, 512, 512), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(inLockBits.Scan0, inData, 0, 512 * 512 * 4);
                    headSkinGrain.UnlockBits(inLockBits);
                }
                Marshal.Copy(outLockBits.Scan0, outData, 0, 512 * 512 * 4);
                for(int i = 512 * 512 * 4; i != 0;)
                {
                    byte alpha = inData[--i];
                    outData[i] = Math.Min(outData[i], alphaData[i]);
                    --i;
                    outData[i] = (byte)((outData[i] * alpha) / 255);
                    --i;
                    outData[i] = (byte)((outData[i] * alpha) / 255);
                    --i;
                    outData[i] = (byte)((outData[i] * alpha) / 255);
                }
                Marshal.Copy(outData, 0, outLockBits.Scan0, 512 * 512 * 4);
                bmp.UnlockBits(outLockBits);
                HeadColorAndSkinGrain = bmp;
            }

            private static Image LoadResourceImage(string name)
            {
                var assembly = typeof(BaseBakes).Assembly;
                using (var resource = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Resources." + name))
                {
                    return Image.FromStream(resource);
                }
            }
        }
        #endregion
    }
}
