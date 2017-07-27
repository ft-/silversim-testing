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

using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using System;
using Image = System.Drawing.Image;
using Bitmap = System.Drawing.Bitmap;
using Rectangle = System.Drawing.Rectangle;
using System.Drawing.Imaging;

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
    }
}
