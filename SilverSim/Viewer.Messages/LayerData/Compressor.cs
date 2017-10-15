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

using log4net;
using SilverSim.Types;
using System;
using System.Collections.Generic;

namespace SilverSim.Viewer.Messages.LayerData
{
    public static partial class LayerCompressor
    {
        public static LayerData ToLayerMessage(List<LayerPatch> patches, LayerData.LayerDataType type)
        {
            int outlength;
            return ToLayerMessage(patches, type, 0, patches.Count, out outlength);
        }

        public static LayerData ToLayerMessage(List<LayerPatch> patches, LayerData.LayerDataType type, int offset, int length, out int outlength)
        {
            outlength = 0;
            var layer = new LayerData
            {
                LayerType = type
            };
            bool extended = false;
            switch(type)
            {
                case LayerData.LayerDataType.CloudExtended:
                case LayerData.LayerDataType.LandExtended:
                case LayerData.LayerDataType.WaterExtended:
                case LayerData.LayerDataType.WindExtended:
                    extended = true;
                    break;
            }

            var header = new GroupHeader
            {
                Stride = STRIDE,
                PatchSize = LAYER_PATCH_NUM_XY_ENTRIES,
                Type = type
            };
            var data = new byte[1500];
            var bitpack = new BitPacker(data, 0);
            bitpack.PackBits(header.Stride, 16);
            bitpack.PackBits(header.PatchSize, 8);
            bitpack.PackBits((uint)header.Type, 8);
            int remainingbits = 1300 * 8; /* 1300 is a bit more than 2 perfectly bad compressed layer patches, wind needs two per packet */

            for (int i = 0; i < length; i++)
            {
                int patchno = i + offset;
                if (patches[patchno].Data.Length != LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES)
                {
                    throw new ArgumentException("Patch data must be a 16x16 array");
                }

                if(CompressPatch(bitpack, patches[patchno], extended, ref remainingbits))
                {
                    ++outlength;
                }
                else
                {
                    break;
                }
            }

            bitpack.PackBits(END_OF_PATCHES, 8);

            layer.Data = new byte[bitpack.NumBytes];
            Buffer.BlockCopy(bitpack.Data, 0, layer.Data, 0, bitpack.NumBytes);

            return layer;
        }

        private static bool CompressPatch(BitPacker pack, LayerPatch layerpatch, bool extended, ref int remainingbits)
        {
            lock (layerpatch)
            {
                /* check whether we have to run terrain compression */
                if(layerpatch.Serial != layerpatch.PackedSerial)
                {
                    layerpatch.PackedData.Reset();
                    if (layerpatch.Data.Length != LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES)
                    {
                        throw new ArgumentException("Patch data must be a 16x16 array");
                    }

                    var pheader = PrescanPatch(layerpatch);
                    pheader.QuantWBits = 136;
                    if (extended)
                    {
                        pheader.PatchIDs = layerpatch.Y & 0xFFFF;
                        pheader.PatchIDs += layerpatch.X << 16;
                    }
                    else
                    {
                        pheader.PatchIDs = layerpatch.Y & 0x1F;
                        pheader.PatchIDs += layerpatch.X << 5;
                    }

                    int[] patch = CompressPatch(layerpatch, pheader, 10);
                    int wbits = EncodePatchHeader(layerpatch.PackedData, pheader, patch, extended);
                    EncodePatch(layerpatch.PackedData, patch, 0, wbits);
                    layerpatch.PackedSerial = layerpatch.Serial;
                }

                if (layerpatch.PackedData.BitLength <= remainingbits)
                {
                    remainingbits -= layerpatch.PackedData.BitLength;
                    pack.PackBits(layerpatch.PackedData);
                    return true;
                }
                return false;
            }
        }

        #region Layer Bit Packing Processing
        private static PatchHeader PrescanPatch(LayerPatch patch)
        {
            var header = new PatchHeader();
            float zmax = -99999999.0f;
            float zmin = 99999999.0f;

            for (int y = 0; y < LAYER_PATCH_NUM_XY_ENTRIES; y++)
            {
                for (int x = 0; x < LAYER_PATCH_NUM_XY_ENTRIES; x++)
                {
                    float val = patch[x, y];
                    if (val > zmax)
                    {
                        zmax = val;
                    }
                    if (val < zmin)
                    {
                        zmin = val;
                    }
                }
            }

            header.DCOffset = zmin;
            header.Range = (int)((zmax - zmin) + 1.0f);

            return header;
        }

        private static int EncodePatchHeader(BitPacker output, PatchHeader header, int[] patch, bool extended)
        {
            int temp;
            int wbits = (header.QuantWBits & 0x0f) + 2;
            uint maxWbits = (uint)wbits + 5;
            uint minWbits = (uint)wbits >> 1;

            wbits = (int)minWbits;

            for (int i = 0; i < patch.Length; i++)
            {
                temp = patch[i];

                if (temp != 0)
                {
                    // Get the absolute value
                    if (temp < 0)
                    {
                        temp *= -1;
                    }

                    for (int j = (int)maxWbits; j > (int)minWbits; j--)
                    {
                        if ((temp & (1 << j)) != 0)
                        {
                            if (j > wbits)
                            {
                                wbits = j;
                            }
                            break;
                        }
                    }
                }
            }

            wbits++;

            if(wbits > 17)
            {
                wbits = 17;
            }
            else if(wbits < 2)
            {
                wbits = 2;
            }

            header.QuantWBits &= 0xf0;

            header.QuantWBits |= wbits - 2;

            output.PackBits(header.QuantWBits, 8);
            output.FloatValue = header.DCOffset;

            output.PackBits(header.Range, 16);
            if (extended)
            {
                output.PackBits(header.PatchIDs, 32);
            }
            else
            {
                output.PackBits(header.PatchIDs, 10);
            }

            /* Bit Length of Header in VarRegion format: 56 bits => 7 Bytes */

            return wbits;
        }

        private static readonly ILog m_Log = LogManager.GetLogger("LAYER COMPRESSOR");

        private static void EncodePatch(BitPacker output, int[] patch, int postquant, int wbits)
        {
            /* maximum possible length of patch data 640 Bytes */
            int temp;
            bool eob;

            if (postquant > LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES || postquant < 0)
            {
                m_Log.ErrorFormat("Postquant {0} is outside the range of allowed values in EncodePatch()", postquant);
                return;
            }

            if (postquant != 0)
            {
                patch[LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES - postquant] = 0;
            }

            for (int i = 0; i < LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES; i++)
            {
                eob = false;
                temp = patch[i];

                if (temp == 0)
                {
                    eob = true;

                    for (int j = i; j < LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES - postquant; j++)
                    {
                        if (patch[j] != 0)
                        {
                            eob = false;
                            break;
                        }
                    }

                    if (eob)
                    {
                        output.PackBits(ZERO_EOB, 2);
                        return;
                    }
                    else
                    {
                        output.PackBits(ZERO_CODE, 1);
                    }
                }
                else
                {
                    if (temp < 0)
                    {
                        temp *= -1;

                        if (temp >= (1 << wbits))
                        {
                            temp = (1 << wbits) - 1;
                        }

                        output.PackBits(NEGATIVE_VALUE, 3);
                        output.PackBits(temp, wbits);
                    }
                    else
                    {
                        if (temp >= (1 << wbits))
                        {
                            temp = (1 << wbits) - 1;
                        }

                        output.PackBits(POSITIVE_VALUE, 3);
                        output.PackBits(temp, wbits);
                    }
                }
            }
        }
        #endregion

        #region Actual compression
        private static int[] CompressPatch(LayerPatch patchData, PatchHeader header, int prequant)
        {
            var block = new float[LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES];
            int wordsize = prequant;
            float oozrange = 1.0f / (float)header.Range;
            var range = (float)(1 << prequant);
            float premult = oozrange * range;
            float sub = (1 << (prequant - 1)) + header.DCOffset * premult;

            header.QuantWBits = wordsize - 2;
            header.QuantWBits |= (prequant - 2) << 4;

            int k = 0;
            for (int y = 0; y < LAYER_PATCH_NUM_XY_ENTRIES; y++)
            {
                for (int x = 0; x < LAYER_PATCH_NUM_XY_ENTRIES; x++)
                {
                    block[k++] = patchData[x, y] * premult - sub;
                }
            }

            var ftemp = new float[LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES];
            var itemp = new int[LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES];

            for (int o = 0; o < LAYER_PATCH_NUM_XY_ENTRIES; o++)
            {
                DCTLine16(block, ftemp, o);
            }
            for (int o = 0; o < LAYER_PATCH_NUM_XY_ENTRIES; o++)
            {
                DCTColumn16(ftemp, itemp, o);
            }

            return itemp;
        }

        private static void DCTLine16(float[] linein, float[] lineout, int line)
        {
            float total = 0.0f;
            int lineSize = line * LAYER_PATCH_NUM_XY_ENTRIES;

            for (int n = 0; n < LAYER_PATCH_NUM_XY_ENTRIES; n++)
            {
                total += linein[lineSize + n];
            }

            lineout[lineSize] = OO_SQRT2 * total;

            for (int u = 1; u < LAYER_PATCH_NUM_XY_ENTRIES; u++)
            {
                total = 0.0f;

                for (int n = 0; n < LAYER_PATCH_NUM_XY_ENTRIES; n++)
                {
                    total += linein[lineSize + n] * CosineTable16[u * LAYER_PATCH_NUM_XY_ENTRIES + n];
                }

                lineout[lineSize + u] = total;
            }
        }

        private static void DCTColumn16(float[] linein, int[] lineout, int column)
        {
            float total = 0.0f;
            const float oosob = 2.0f / 16.0f;

            for (int n = 0; n < LAYER_PATCH_NUM_XY_ENTRIES; n++)
            {
                total += linein[LAYER_PATCH_NUM_XY_ENTRIES * n + column];
            }

            lineout[CopyMatrix16[column]] = (int)(OO_SQRT2 * total * oosob * QuantizeTable16[column]);

            for (int u = 1; u < LAYER_PATCH_NUM_XY_ENTRIES; u++)
            {
                total = 0.0f;

                for (int n = 0; n < LAYER_PATCH_NUM_XY_ENTRIES; n++)
                {
                    total += linein[16 * n + column] * CosineTable16[u * LAYER_PATCH_NUM_XY_ENTRIES + n];
                }

                lineout[CopyMatrix16[LAYER_PATCH_NUM_XY_ENTRIES * u + column]] = (int)(total * oosob * QuantizeTable16[LAYER_PATCH_NUM_XY_ENTRIES * u + column]);
            }
        }
        #endregion
    }
}
