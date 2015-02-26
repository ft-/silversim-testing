/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Types;
using System;

namespace SilverSim.LL.Messages.LayerData
{
    public static partial class LayerCompressor
    {
        public static LayerData ToLayerMessage(LayerPatch[] patches, Messages.LayerData.LayerData.LayerDataType type)
        {
            return ToLayerMessage(patches, type, 0, patches.Length);
        }

        public static LayerData ToLayerMessage(LayerPatch[] patches, Messages.LayerData.LayerData.LayerDataType type, int offset, int length)
        {
            Messages.LayerData.LayerData layer = new Messages.LayerData.LayerData();
            layer.LayerType = type;

            bool extended = false;
            switch(type)
            {
                case LayerData.LayerDataType.CloudExtended:
                case LayerData.LayerDataType.LandExtended:
                case LayerData.LayerDataType.WaterExtended:
                case LayerData.LayerDataType.WindExtended:
                    extended = true;
                    break;

                default:
                    break;
            }

            GroupHeader header = new GroupHeader();
            header.Stride = STRIDE;
            header.PatchSize = LAYER_PATCH_NUM_XY_ENTRIES;
            header.Type = type;

            // Should be enough to fit even the most poorly packed data
            byte[] data = new byte[patches.Length * LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES * 2];
            BitPacker bitpack = new BitPacker(data, 0);
            bitpack.PackBits(header.Stride, 16);
            bitpack.PackBits(header.PatchSize, 8);
            bitpack.PackBits((uint)header.Type, 8);

            for (int i = 0; i < length; i++)
            {
                if (patches[i + offset].Data.Length != LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES)
                {
                    throw new ArgumentException("Patch data must be a 16x16 array");
                }

                PatchHeader pheader = PrescanPatch(patches[i]);
                pheader.QuantWBits = 136;
                if (extended)
                {
                    pheader.PatchIDs = (patches[i].Y & 0xFFFF);
                    pheader.PatchIDs += (patches[i].X << 16);
                }
                else
                {
                    pheader.PatchIDs = (patches[i].Y & 0x1F);
                    pheader.PatchIDs += (patches[i].X << 5);
                }

                // NOTE: No idea what prequant and postquant should be or what they do
                int[] patch = CompressPatch(patches[i], pheader, 10);
                int wbits = EncodePatchHeader(bitpack, pheader, patch, extended);
                EncodePatch(bitpack, patch, 0, wbits);
            }

            bitpack.PackBits(END_OF_PATCHES, 8);

            layer.Data = new byte[bitpack.NumBytes];
            Buffer.BlockCopy(bitpack.Data, 0, layer.Data, 0, bitpack.NumBytes);

            return layer;
        }

        private static PatchHeader PrescanPatch(LayerPatch patch)
        {
            PatchHeader header = new PatchHeader();
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
            uint minWbits = ((uint)wbits >> 1);

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

            wbits += 1;

            if(wbits > 17)
            {
                wbits = 17;
            }
            else if(wbits < 2)
            {
                wbits = 2;
            }

            header.QuantWBits &= 0xf0;

            header.QuantWBits |= (wbits - 2);

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

            return wbits;
        }

        private static void EncodePatch(BitPacker output, int[] patch, int postquant, int wbits)
        {
            int temp;
            bool eob;

            if (postquant > LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES || postquant < 0)
            {
                //Logger.Log("Postquant is outside the range of allowed values in EncodePatch()", Helpers.LogLevel.Error);
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

                        if (temp > (1 << wbits))
                        {
                            temp = (1 << wbits);
                        }

                        output.PackBits(NEGATIVE_VALUE, 3);
                        output.PackBits(temp, wbits);
                    }
                    else
                    {
                        if (temp > (1 << wbits))
                        {
                            temp = (1 << wbits);
                        }

                        output.PackBits(POSITIVE_VALUE, 3);
                        output.PackBits(temp, wbits);
                    }
                }
            }
        }

        #region Actual compression
        private static int[] CompressPatch(LayerPatch patchData, PatchHeader header, int prequant)
        {
            float[] block = new float[LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES];
            int wordsize = prequant;
            float oozrange = 1.0f / (float)header.Range;
            float range = (float)(1 << prequant);
            float premult = oozrange * range;
            float sub = (float)(1 << (prequant - 1)) + header.DCOffset * premult;

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

            float[] ftemp = new float[LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES];
            int[] itemp = new int[LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES];

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
