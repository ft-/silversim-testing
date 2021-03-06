﻿// SilverSim is distributed under the terms of the
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

using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Bitmap = System.Drawing.Bitmap;
using Graphics = System.Drawing.Graphics;
using Image = System.Drawing.Image;
using Rectangle = System.Drawing.Rectangle;
using SolidBrush = System.Drawing.SolidBrush;

namespace SilverSim.Scene.Agent.Bakery
{
    public abstract class AbstractSubBaker : IDisposable
    {
        public abstract WearableType Type { get; }

        public int Ordinal { get; set; }

        public abstract bool IsBaked { get; }

        /*
         * may return null if bake does not output a bake image
         */
        public virtual Image BakeImageOutput(IBakeTextureInputCache cache, BakeTarget target) => null;
        public virtual ColorAlpha BakeImageColor(BakeTarget target) => ColorAlpha.White;
        /*
         * may return null if bake does not output an alpha mask 
         */
        public virtual Image BakeAlphaMaskOutput(IBakeTextureInputCache cache, BakeTarget target) => null;
        /*
         * may return null if bake does not output a bump 
         */
        public virtual byte[] BakeBumpOutput(IBakeTextureInputCache cache, BakeTarget target) => null;

        public abstract void Dispose();

        protected static Bitmap CreateTargetBakeImage(BakeTarget target)
        {
            if(target == BakeTarget.Eyes)
            {
                return new Bitmap(128, 128, PixelFormat.Format32bppArgb);
            }
            else
            {
                return new Bitmap(512, 512, PixelFormat.Format32bppArgb);
            }
        }

        protected static Bitmap CreateWhiteBakeImage(BakeTarget target)
        {
            int dimensions = target == BakeTarget.Eyes ? 128 : 512;
            Bitmap bmp = new Bitmap(dimensions, dimensions, PixelFormat.Format32bppArgb);
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                using (var b = new SolidBrush(System.Drawing.Color.White))
                {
                    gfx.FillRectangle(b, new Rectangle(0, 0, dimensions, dimensions));
                }
            }
            return bmp;
        }

        protected static Rectangle GetTargetBakeDimensions(BakeTarget target)
        {
            if(target == BakeTarget.Eyes)
            {
                return new Rectangle(0, 0, 128, 128);
            }
            else
            {
                return new Rectangle(0, 0, 512, 512);
            }
        }

        protected static Color CalcColor(double val, Color[] table)
        {
            var paramColor = new Color(0, 0, 0);

            if (table.Length == 1)
            {
                paramColor = table[0];
            }
            else
            {
                int tableLen = table.Length;
                val = val.Clamp(0, 1);
                int maxTableIndex = tableLen - 1;
                double stpos = val * maxTableIndex;

                int indexa = (int)Math.Floor(stpos);
                int indexb = Math.Min(indexa + 1, maxTableIndex);

                double mix = stpos - indexa;

                if (mix < Double.Epsilon || indexa == indexb)
                {
                    paramColor = table[indexa];
                }
                else
                {
                    Color ca = table[indexa];
                    Color cb = table[indexb];
                    paramColor.R = ca.R.Lerp(cb.R, mix);
                    paramColor.G = ca.G.Lerp(cb.G, mix);
                    paramColor.B = ca.B.Lerp(cb.B, mix);
                }
            }

            return paramColor;
        }

        protected static ColorAlpha CalcColor(double val, ColorAlpha[] table)
        {
            var paramColor = new ColorAlpha(0, 0, 0, 0);

            if (table.Length == 1)
            {
                paramColor = table[0];
            }
            else
            {
                int tableLen = table.Length;
                val = val.Clamp(0, 1);
                int maxTableIndex = tableLen - 1;
                double stpos = val * maxTableIndex;

                int indexa = (int)Math.Floor(stpos);
                int indexb = Math.Min(indexa + 1, maxTableIndex);

                double mix = stpos - indexa;

                if (mix < double.Epsilon || indexa == indexb)
                {
                    paramColor = table[indexa];
                }
                else
                {
                    ColorAlpha ca = table[indexa];
                    ColorAlpha cb = table[indexb];
                    paramColor.R = ca.R.Lerp(cb.R, mix);
                    paramColor.G = ca.G.Lerp(cb.G, mix);
                    paramColor.B = ca.B.Lerp(cb.B, mix);
                    paramColor.A = ca.A.Lerp(cb.A, mix);
                }
            }

            return paramColor;
        }

        protected void BlendAlpha(byte[] img, byte[] graymap, double val)
        {
            if (graymap.Length * 4 != img.Length)
            {
                throw new ArgumentException(nameof(graymap));
            }
            int bitmapLength = img.Length;
            var alphalevel = (byte)((1 - val) * 255);
            int grayPos = 0;
            int bitmapPos = 0;
            while(bitmapPos < bitmapLength)
            {
                bitmapPos += 3;
                img[bitmapPos] = Math.Min((byte)(graymap[grayPos] >= alphalevel ? 255 : 0), img[bitmapPos]);
                ++grayPos;
                ++bitmapPos;
            }
        }

        protected void MultiplyBump(byte[] img, double val)
        {
            var v = (uint)(256 * val);
            for(int i = 0; i < img.Length; ++i)
            {
                uint cal = img[i] * v;
                cal >>= 8;
                img[i] = (byte)cal;
            }
        }

        protected void BlendBump(byte[] img, byte[] graymap, double val)
        {
            if (graymap.Length != img.Length)
            {
                throw new ArgumentException(nameof(graymap));
            }
            int bitmapLength = img.Length;
            var alphalevel = (byte)((1 - val) * 255);
            int blendPos = 0;
            while (blendPos < bitmapLength)
            {
                img[blendPos] = Math.Min((byte)(graymap[blendPos] >= alphalevel ? 255 : 0), img[blendPos]);
                ++blendPos;
            }
        }

        protected void InsideAlphaBlend(Bitmap bmp, Action<byte[]> del)
        {
            int rawdatalength = bmp.Width * bmp.Height * 4;
            BitmapData bmpLock = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var rawdata = new byte[rawdatalength];
            Marshal.Copy(bmpLock.Scan0, rawdata, 0, rawdata.Length);
            del(rawdata);
            Marshal.Copy(rawdata, 0, bmpLock.Scan0, rawdata.Length);
            bmp.UnlockBits(bmpLock);
        }
    }
}
