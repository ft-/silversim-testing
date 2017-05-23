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
using System.Collections.Generic;
using System.IO;

namespace SilverSim.Viewer.Messages.LayerData
{
    public static class LLRAWData
    {
        public struct HeightmapLookupValue : IComparable<HeightmapLookupValue>, IEquatable<HeightmapLookupValue>
        {
            public readonly ushort Index;
            public readonly float Value;

            public HeightmapLookupValue(ushort index, float value)
            {
                Index = index;
                Value = value;
            }

            public int CompareTo(HeightmapLookupValue val)
            {
                return Value.CompareTo(val.Value);
            }

            public static bool operator ==(HeightmapLookupValue a, HeightmapLookupValue b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(HeightmapLookupValue a, HeightmapLookupValue b)
            {
                return !a.Equals(b);
            }

            public static bool operator >(HeightmapLookupValue a, HeightmapLookupValue b)
            {
                return a.Value > b.Value;
            }

            public static bool operator <(HeightmapLookupValue a, HeightmapLookupValue b)
            {
                return a.Value < b.Value;
            }

            public override bool Equals(object obj)
            {
                if (obj is HeightmapLookupValue)
                {
                    return Value.Equals(((HeightmapLookupValue)obj).Value);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }

            public bool Equals(HeightmapLookupValue v)
            {
                return Value.Equals(v.Value);
            }
        }

        /// <summary>Lookup table to speed up terrain exports</summary>
        private static readonly HeightmapLookupValue[] LookupHeightTable;

        static LLRAWData()
        {
            LookupHeightTable = new HeightmapLookupValue[256 * 256];

            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    LookupHeightTable[i + (j * 256)] = new HeightmapLookupValue((ushort)(i + (j * 256)), (float)((double)i * ((double)j / 128.0d)));
                }
            }
            Array.Sort(LookupHeightTable);
        }

        private static float ReadLLRAWElev(Stream input)
        {
            var buffer = new byte[13];
            if (13 != input.Read(buffer, 0, 13))
            {
                throw new EndOfStreamException();
            }

            return buffer[0] * buffer[1] / 128f;
        }

        public static List<LayerPatch> LoadLLRawStream(this Stream input, int suggested_width, int suggested_height)
        {
            var res = new List<LayerPatch>();
            input.LoadLLRawStream(suggested_width, suggested_height, (LayerPatch lp) => res.Add(lp));
            return res;
        }

        public static void LoadLLRawStream(this Stream input, int suggested_width, int suggested_height, Action<LayerPatch> del)
        {
            var vals = new float[LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, suggested_width / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
            uint maxY = (uint)suggested_height / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES - 1;

            for (uint patchy = 0; patchy < suggested_height / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++patchy)
            {
                /* we have to load 16 lines at a time */
                for (uint liney = 0; liney < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++liney)
                {
                    for (uint x = 0; x < suggested_width; ++x)
                    {
                        vals[liney, x] = ReadLLRAWElev(input);
                    }
                }

                /* now build patches from those 16 lines */
                for (uint patchx = 0; patchx < suggested_width / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++patchx)
                {
                    var patch = new LayerPatch()
                    {
                        X = patchx,
                        Y = maxY - patchy
                    };
                    for (uint y = 0; y < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                    {
                        for (uint x = 0; x < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                        {
                            patch[x, LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES - 1 - y] = vals[y, x + patchx * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
                        }
                    }
                    del(patch);
                }
            }
        }

        private static uint XYToYInverted(this LayerPatch p, uint lineWidth, uint maxY)
        {
            return (maxY - p.Y) * lineWidth + p.X;
        }

        public static byte[] ToLLRaw(this List<LayerPatch> terrain)
        {
            var outdata = new byte[13 * terrain.Count * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
            uint maxY = terrain[0].Y;
            uint minY = terrain[0].Y;
            uint maxX = terrain[0].X;
            uint minX = terrain[0].X;

            /* determine line width */
            foreach (LayerPatch p in terrain)
            {
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);
            }

            uint linewidth = maxX - minX + 1;

            /* build output data */
            foreach (LayerPatch p in terrain)
            {
                for (uint y = 0; y < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                {
                    for (uint x = 0; x < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                    {
                        uint llraw_pos = (p.XYToYInverted(linewidth, maxY) +
                            (LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES - y) + x) * 13;
                        float elev = Math.Min(p[x, y], 0);
                        int index = Array.BinarySearch(LookupHeightTable, new HeightmapLookupValue(0, elev));
                        var r = (byte)(index & 0xFF);
                        var g = (byte)((index >> 8) & 0xFF);

                        outdata[llraw_pos + 0] = r;
                        outdata[llraw_pos + 1] = g;
                        outdata[llraw_pos + 2] = 20;
                        outdata[llraw_pos + 3] = 0;
                        outdata[llraw_pos + 4] = 0;
                        outdata[llraw_pos + 5] = 0;
                        outdata[llraw_pos + 6] = 0;
                        outdata[llraw_pos + 7] = 255;
                        outdata[llraw_pos + 8] = 255;
                        outdata[llraw_pos + 9] = 255;
                        outdata[llraw_pos + 10] = 255;
                        outdata[llraw_pos + 11] = r;
                        outdata[llraw_pos + 12] = g;
                    }
                }
            }

            return outdata;
        }
    }
}
