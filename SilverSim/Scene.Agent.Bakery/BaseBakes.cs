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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SilverSim.Scene.Agent.Bakery
{
    internal static class BaseBakes
    {
        public static Image HeadAlpha { get; }
        public static Image HeadColor { get; }
        public static Image HeadHair { get; }
        public static Image HeadSkinGrain { get; }
        public static Image LowerBodyColor { get; }
        public static Image LowerBodyColorAndSkinGrain { get; }
        public static Image UpperBodyColor { get; }
        public static Image UpperBodyColorAndSkinGrain { get; }
        public static Image BodySkingrain { get; }
        public static Image HeadColorAndSkinGrain { get; }
        public static Image LipstickAlpha { get; }
        public static Image LipglossAlpha { get; }
        public static Image LipsMask { get; }
        public static Image EyelinerAlpha { get; }
        public static Image BlushAlpha { get; }
        public static Image RosyfaceAlpha { get; }
        public static Image NailpolishAlpha { get; }
        public static Image InnershadowAlpha { get; }
        public static Image OutershadowAlpha { get; }

        public static byte[] HeadBump { get; }
        public static byte[] UpperBodyBump { get; }
        public static byte[] LowerBodyBump { get; }

        static BaseBakes()
        {
            HeadAlpha = LoadResourceImage("skin.head_alpha.png");
            HeadColor = LoadResourceImage("skin.head_color.png");
            HeadHair = LoadResourceImage("hair.head_hair.png");
            HeadSkinGrain = LoadResourceImage("skin.head_skingrain.png");
            LowerBodyColor = LoadResourceImage("skin.lowerbody_color.png");
            UpperBodyColor = LoadResourceImage("skin.upperbody_color.png");
            BlushAlpha = LoadResourceImage("skin.blush_alpha.png");
            RosyfaceAlpha = LoadResourceImage("skin.rosyface_alpha.png");
            NailpolishAlpha = LoadResourceImage("skin.nailpolish_alpha.png");
            LipstickAlpha = LoadResourceImage("skin.lipstick_alpha.png");
            LipglossAlpha = LoadResourceImage("skin.lipgloss_alpha.png");
            EyelinerAlpha = LoadResourceImage("skin.eyeliner_alpha.png");
            LipsMask = LoadResourceImage("skin.lips_mask.png");
            InnershadowAlpha = LoadResourceImage("skin.eyeshadow_inner_alpha.png");
            OutershadowAlpha = LoadResourceImage("skin.eyeshadow_outer_alpha.png");
            BodySkingrain = LoadResourceImage("skin.body_singrain.png");

            HeadBump = LoadResourceBumpmap("bump.bump_head_base.png");
            UpperBodyBump = LoadResourceBumpmap("bump.bump_upperbody_base.png");
            LowerBodyBump = LoadResourceBumpmap("bump.bump_lowerbody_base.png");

            {
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
                for (int i = 512 * 512 * 4; i != 0;)
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

            {
                Bitmap bmp = new Bitmap(UpperBodyColor);
                BitmapData outLockBits = bmp.LockBits(new Rectangle(0, 0, 512, 512), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                byte[] inData = new byte[512 * 512 * 4];
                byte[] outData = new byte[512 * 512 * 4];
                using (Bitmap skinGrain = new Bitmap(BodySkingrain))
                {
                    BitmapData inLockBits = skinGrain.LockBits(new Rectangle(0, 0, 512, 512), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(inLockBits.Scan0, inData, 0, 512 * 512 * 4);
                    skinGrain.UnlockBits(inLockBits);
                }
                Marshal.Copy(outLockBits.Scan0, outData, 0, 512 * 512 * 4);
                for (int i = 512 * 512 * 4; i != 0;)
                {
                    byte alpha = inData[--i];
                    --i;
                    outData[i] = (byte)((outData[i] * alpha) / 255);
                    --i;
                    outData[i] = (byte)((outData[i] * alpha) / 255);
                    --i;
                    outData[i] = (byte)((outData[i] * alpha) / 255);
                }
                Marshal.Copy(outData, 0, outLockBits.Scan0, 512 * 512 * 4);
                bmp.UnlockBits(outLockBits);
                UpperBodyColorAndSkinGrain = bmp;
            }

            {
                Bitmap bmp = new Bitmap(LowerBodyColor);
                BitmapData outLockBits = bmp.LockBits(new Rectangle(0, 0, 512, 512), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                byte[] inData = new byte[512 * 512 * 4];
                byte[] outData = new byte[512 * 512 * 4];
                using (Bitmap skinGrain = new Bitmap(BodySkingrain))
                {
                    BitmapData inLockBits = skinGrain.LockBits(new Rectangle(0, 0, 512, 512), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(inLockBits.Scan0, inData, 0, 512 * 512 * 4);
                    skinGrain.UnlockBits(inLockBits);
                }
                Marshal.Copy(outLockBits.Scan0, outData, 0, 512 * 512 * 4);
                for (int i = 512 * 512 * 4; i != 0;)
                {
                    byte alpha = inData[--i];
                    --i;
                    outData[i] = (byte)((outData[i] * alpha) / 255);
                    --i;
                    outData[i] = (byte)((outData[i] * alpha) / 255);
                    --i;
                    outData[i] = (byte)((outData[i] * alpha) / 255);
                }
                Marshal.Copy(outData, 0, outLockBits.Scan0, 512 * 512 * 4);
                bmp.UnlockBits(outLockBits);
                LowerBodyColorAndSkinGrain = bmp;
            }
        }

        private static Image LoadResourceImage(string name)
        {
            var assembly = typeof(BaseBakes).Assembly;
            using (var resource = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Resources." + name))
            {
                return Image.FromStream(resource);
            }
        }

        private static byte[] LoadResourceBumpmap(string name)
        {
            var assembly = typeof(BaseBakes).Assembly;
            byte[] bumpmap;
            using (var resource = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Resources." + name))
            {
                using (Image img = Image.FromStream(resource))
                {
                    using (Bitmap bmp = new Bitmap(img))
                    {
                        BitmapData inLockBits = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                        bumpmap = new byte[bmp.Width * bmp.Height];
                        byte[] pixeldata = new byte[bmp.Width * bmp.Height * 3];
                        Marshal.Copy(inLockBits.Scan0, pixeldata, 0, bmp.Width * bmp.Height * 3);
                        int inpos = 0;
                        for (int i = 0; i < bmp.Width * bmp.Height; ++i)
                        {
                            bumpmap[i] = pixeldata[inpos];
                            inpos += 3;
                        }
                        bmp.UnlockBits(inLockBits);
                    }
                }
            }

            return bumpmap;
        }
    }
}
