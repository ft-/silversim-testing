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

using SilverSim.Scene.Agent.Bakery.SubBakers.Bodyparts;
using SilverSim.Scene.Agent.Bakery.SubBakers.Clothing;
using SilverSim.Types.Asset.Format;
using System.Drawing;
using System.Drawing.Imaging;
using Color3 = SilverSim.Types.Color;
using ColorAlpha = SilverSim.Types.ColorAlpha;

namespace SilverSim.Scene.Agent.Bakery
{
    public static class ExtensionMethods
    {
        public static AbstractSubBaker CreateSubBaker(this Wearable wearable)
        {
            VisualParamsMapper.CompleteParams(wearable.Params);

            switch (wearable.Type)
            {
                case WearableType.Eyes:
                    return new EyeSubBaker(wearable);
                case WearableType.Skin:
                    return new SkinSubBaker(wearable);
                case WearableType.Hair:
                    return new HairSubBaker(wearable);
                case WearableType.Shape:
                    return new ShapeSubBaker(wearable);

                case WearableType.Tattoo:
                    return new TattooSubBaker(wearable);
                case WearableType.Alpha:
                    return new AlphaMaskSubBaker(wearable);
                case WearableType.Shirt:
                    return new ShirtSubBaker(wearable);
                case WearableType.Jacket:
                    return new JacketSubBaker(wearable);
                case WearableType.Socks:
                    return new SocksSubBaker(wearable);
                case WearableType.Pants:
                    return new PantsSubBaker(wearable);
                case WearableType.Underpants:
                    return new UnderpantsSubBaker(wearable);
                case WearableType.Undershirt:
                    return new UndershirtSubBaker(wearable);
                case WearableType.Shoes:
                    return new ShoesSubBaker(wearable);
                case WearableType.Skirt:
                    return new SkirtSubBaker(wearable);
                case WearableType.Gloves:
                    return new GlovesSubBaker(wearable);

                default:
                    return null; /* intentionally returning null here */
            }
        }

        public static double LimitRange(this double v, double min, double max)
        {
            if(v < min)
            {
                return min;
            }
            else if(v > max)
            {
                return max;
            }
            else
            {
                return v;
            }
        }

        public static void DrawUntinted(this Graphics gfx, Rectangle bakeRectangle, Image baseBake)
        {
            gfx.DrawImage(baseBake, bakeRectangle, 0, 0, baseBake.Width, baseBake.Height, GraphicsUnit.Pixel);
        }

        public static void DrawTinted(this Graphics gfx, Rectangle bakeRectangle, Image baseBake, Color3 color)
        {
            using (var attrs = new ImageAttributes())
            {
                var mat = new ColorMatrix();
                mat.ApplyTint(color);
                attrs.SetColorMatrix(mat);
                gfx.DrawImage(baseBake, bakeRectangle, 0, 0, baseBake.Width, baseBake.Height, GraphicsUnit.Pixel, attrs);
            }
        }

        public static void DrawTinted(this Graphics gfx, Rectangle bakeRectangle, Image baseBake, ColorAlpha color)
        {
            using (var attrs = new ImageAttributes())
            {
                var mat = new ColorMatrix();
                mat.ApplyTint(color);
                attrs.SetColorMatrix(mat);
                gfx.DrawImage(baseBake, bakeRectangle, 0, 0, baseBake.Width, baseBake.Height, GraphicsUnit.Pixel, attrs);
            }
        }

        public static void ApplyTint(this ColorMatrix mat, Color3 col)
        {
            mat.Matrix00 *= (float)col.R;
            mat.Matrix11 *= (float)col.G;
            mat.Matrix22 *= (float)col.B;
        }

        public static void ApplyTint(this ColorMatrix mat, ColorAlpha col)
        {
            mat.Matrix00 *= (float)col.R;
            mat.Matrix11 *= (float)col.G;
            mat.Matrix22 *= (float)col.B;
            mat.Matrix33 *= (float)col.A;
        }

        public static Color ToDrawing(this Color3 color)
        {
            return Color.FromArgb(color.R_AsByte, color.G_AsByte, color.B_AsByte);
        }

        public static Color ToDrawing(this ColorAlpha color)
        {
            return Color.FromArgb(color.A_AsByte, color.R_AsByte, color.G_AsByte, color.B_AsByte);
        }

        public static Color ToDrawingWithNewAlpha(this Color3 color, double newalpha)
        {
            return Color.FromArgb((int)(newalpha * 255), color.R_AsByte, color.G_AsByte, color.B_AsByte);
        }
    }
}
