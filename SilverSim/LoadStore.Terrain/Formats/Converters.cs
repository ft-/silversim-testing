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

using SilverSim.Viewer.Messages.LayerData;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace SilverSim.LoadStore.Terrain.Formats
{
    public static class MapConverters
    {
        static readonly Color[] m_GrayScaleMap;
        static MapConverters()
        {
            m_GrayScaleMap = new Color[256];
            for (int i = 0; i < m_GrayScaleMap.Length; ++i)
            {
                m_GrayScaleMap[i] = Color.FromArgb(i, i, i);
            }

        }

        #region Sort List by X and then Y
        public static SortedList<uint, LayerPatch> SortPatchesByXY(this List<LayerPatch> inpatches)
        {
            var outpatches = new SortedList<uint, LayerPatch>();
            foreach(LayerPatch patch in inpatches)
            {
                outpatches.Add((patch.Y << 16) | patch.X, patch);
            }

            return outpatches;
        }
        #endregion

        #region Bitmap -> LayerPatch Lists
        public static List<LayerPatch> ToPatchesFromGrayscale(this Bitmap bitmap)
        {
            var patches = new List<LayerPatch>();
            if(bitmap.Width % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES != 0 ||
                (bitmap.Height % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES) != 0)
            {
                throw new ArgumentException("bitmap wxh is not dividable by LAYER_PATCH_NUM_XY_ENTRIES");
            }

            for(uint y = 0; y < bitmap.Height / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
            {
                for(uint x = 0; x < bitmap.Width / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                {
                    LayerPatch patch = new LayerPatch();
                    patch.X = x;
                    patch.Y = (uint)bitmap.Height / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES - 1 - y;
                    for (uint py = 0; py < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++py)
                    {
                        for (uint px = 0; px < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++px)
                        {
                            long lx = (x * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES + px);
                            long ly = (bitmap.Height - (long)y * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES - py - 1);
                            patch[px, py] = bitmap.GetPixel((int)lx, (int)ly).GetBrightness() * 128f;
                        }
                    }
                    patches.Add(patch);
                }
            }

            return patches;
        }
        #endregion

        #region LayerPatch lists -> Bitmap
        internal static Bitmap ToBitmap(this List<LayerPatch> patches, Color[] colormap)
        {
            uint lowX = 65535;
            uint lowY = 65535;
            uint highX = 0;
            uint highY = 0;
            foreach (LayerPatch patch in patches)
            {
                lowX = Math.Min(patch.X, lowX);
                lowY = Math.Min(patch.Y, lowY);
                highX = Math.Max(patch.X + 16, highX);
                highY = Math.Max(patch.Y + 16, highY);
            }

            var bitmap = new Bitmap((int)(highX - lowX) * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, (int)(highY - lowY) * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES);
            int maxC = colormap.Length - 1;
            int dividerC = 512 / colormap.Length;

            foreach (LayerPatch patch in patches)
            {
                uint offsetx = patch.X * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
                uint offsety = patch.X * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;

                for (uint y = 0; y < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                {
                    for (uint x = 0; x < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                    {
                        long p = (((long)patch[x, y]) * maxC) / dividerC;
                        p = Math.Max(Math.Min(p, maxC), 0);

                        bitmap.SetPixel((int)(offsetx + x), (int)(highY + LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES - offsety - y - 1), colormap[(int)p]);
                    }
                }
            }

            return bitmap;
        }

        public static Bitmap ToGrayScaleBitmap(this List<LayerPatch> patches) => patches.ToBitmap(m_GrayScaleMap);
        #endregion

        public static uint XYToYInverted(this LayerPatch p, uint lineWidth, uint maxY) => (maxY - p.Y) * lineWidth + p.X;

        public static uint XYToYNormal(this LayerPatch p, uint lineWidth, uint minY) => (p.Y - minY) * lineWidth + p.X;
    }
}
