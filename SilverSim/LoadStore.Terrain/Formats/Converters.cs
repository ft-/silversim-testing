// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
            SortedList<uint, LayerPatch> outpatches = new SortedList<uint, LayerPatch>();
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
            List<LayerPatch> patches = new List<LayerPatch>();
            if(bitmap.Width % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES != 0 ||
                (bitmap.Height % LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES) != 0)
            {
                throw new ArgumentException();
            }

            uint x, y, px, py;
            for(y = 0; y < bitmap.Height / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
            {
                for(x = 0; x < bitmap.Width / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                {
                    LayerPatch patch = new LayerPatch();
                    patch.X = x;
                    patch.Y = (uint)bitmap.Height / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES - 1 - y;
                    for (py = 0; py < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++py)
                    {
                        for (px = 0; px < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++px)
                        {
                            patch[px, py] = bitmap.GetPixel((int)(x * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES + px),
                                (int)(bitmap.Height - y * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES - py - 1)).GetBrightness() * 128f;
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

            Bitmap bitmap = new Bitmap((int)(highX - lowX) * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, (int)(highY - lowY) * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES);
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

        public static Bitmap ToGrayScaleBitmap(this List<LayerPatch> patches)
        {
            return patches.ToBitmap(m_GrayScaleMap);
        }
        #endregion

        public static uint XYToYInverted(this LayerPatch p, uint lineWidth, uint maxY)
        {
            return (maxY - p.Y) * lineWidth + p.X;
        }

        public static uint XYToYNormal(this LayerPatch p, uint lineWidth, uint minY)
        {
            return (p.Y - minY) * lineWidth + p.X;
        }
    }
}
