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
            Messages.LayerData.LayerData layer = new Messages.LayerData.LayerData();
            layer.LayerType = type;

            GroupHeader header = new GroupHeader();
            header.Stride = STRIDE;
            header.PatchSize = TERRAIN_PATCH_SIZE;
            header.Type = type;

            // Should be enough to fit even the most poorly packed data
            byte[] data = new byte[patches.Length * TERRAIN_PATCH_SIZE * TERRAIN_PATCH_SIZE * 2];
            BitPacker bitpack = new BitPacker(data, 0);
            bitpack.PackBits(header.Stride, 16);
            bitpack.PackBits(header.PatchSize, 8);
            bitpack.PackBits((uint)header.Type, 8);

            for (int i = 0; i < patches.Length; i++)
            {
                if (patches[i].Data.Length != TERRAIN_PATCH_SIZE * TERRAIN_PATCH_SIZE)
                {
                    throw new ArgumentException("Patch data must be a 16x16 array");
                }

                PatchHeader pheader = PrescanPatch(patches[i].Data);
                pheader.QuantWBits = 136;
                pheader.PatchIDs = (patches[i].Y & 0x1F);
                pheader.PatchIDs += (patches[i].X << 5);

                // NOTE: No idea what prequant and postquant should be or what they do
                int[] patch = CompressPatch(patches[i].Data, pheader, 10);
                int wbits = EncodePatchHeader(bitpack, pheader, patch);
                EncodePatch(bitpack, patch, 0, wbits);
            }

            bitpack.PackBits(END_OF_PATCHES, 8);

            layer.Data = new byte[bitpack.NumBytes];
            Buffer.BlockCopy(bitpack.Data, 0, layer.Data, 0, bitpack.NumBytes);

            return layer;
        }

        private static PatchHeader PrescanPatch(float[,] patch)
        {
            PatchHeader header = new PatchHeader();
            float zmax = -99999999.0f;
            float zmin = 99999999.0f;

            for (int j = 0; j < TERRAIN_PATCH_SIZE; j++)
            {
                for (int i = 0; i < TERRAIN_PATCH_SIZE; i++)
                {
                    float val = patch[j, i];
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

        private static int EncodePatchHeader(BitPacker output, PatchHeader header, int[] patch)
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

            header.QuantWBits &= 0xf0;

            if (wbits > 17 || wbits < 2)
            {
                //Logger.Log("Bits needed per word in EncodePatchHeader() are outside the allowed range",
                //    Helpers.LogLevel.Error);
            }

            header.QuantWBits |= (wbits - 2);

            output.PackBits(header.QuantWBits, 8);
            output.FloatValue = header.DCOffset;
            output.PackBits(header.Range, 16);
            output.PackBits(header.PatchIDs, 10);

            return wbits;
        }

        private static void EncodePatch(BitPacker output, int[] patch, int postquant, int wbits)
        {
            int temp;
            bool eob;

            if (postquant > TERRAIN_PATCH_SIZE * TERRAIN_PATCH_SIZE || postquant < 0)
            {
                //Logger.Log("Postquant is outside the range of allowed values in EncodePatch()", Helpers.LogLevel.Error);
                return;
            }

            if (postquant != 0)
            {
                patch[TERRAIN_PATCH_SIZE * TERRAIN_PATCH_SIZE - postquant] = 0;
            }

            for (int i = 0; i < TERRAIN_PATCH_SIZE * TERRAIN_PATCH_SIZE; i++)
            {
                eob = false;
                temp = patch[i];

                if (temp == 0)
                {
                    eob = true;

                    for (int j = i; j < TERRAIN_PATCH_SIZE * TERRAIN_PATCH_SIZE - postquant; j++)
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
        private static int[] CompressPatch(float[,] patchData, PatchHeader header, int prequant)
        {
            float[] block = new float[TERRAIN_PATCH_SIZE * TERRAIN_PATCH_SIZE];
            int wordsize = prequant;
            float oozrange = 1.0f / (float)header.Range;
            float range = (float)(1 << prequant);
            float premult = oozrange * range;
            float sub = (float)(1 << (prequant - 1)) + header.DCOffset * premult;

            header.QuantWBits = wordsize - 2;
            header.QuantWBits |= (prequant - 2) << 4;

            int k = 0;
            for (int j = 0; j < TERRAIN_PATCH_SIZE; j++)
            {
                for (int i = 0; i < TERRAIN_PATCH_SIZE; i++)
                {
                    block[k++] = patchData[j, i] * premult - sub;
                }
            }

            float[] ftemp = new float[TERRAIN_PATCH_SIZE * TERRAIN_PATCH_SIZE];
            int[] itemp = new int[TERRAIN_PATCH_SIZE * TERRAIN_PATCH_SIZE];

            for (int o = 0; o < TERRAIN_PATCH_SIZE; o++)
            {
                DCTLine16(block, ftemp, o);
            }
            for (int o = 0; o < TERRAIN_PATCH_SIZE; o++)
            {
                DCTColumn16(ftemp, itemp, o);
            }

            return itemp;
        }

        private static void DCTLine16(float[] linein, float[] lineout, int line)
        {
            float total = 0.0f;
            int lineSize = line * TERRAIN_PATCH_SIZE;

            for (int n = 0; n < TERRAIN_PATCH_SIZE; n++)
            {
                total += linein[lineSize + n];
            }

            lineout[lineSize] = OO_SQRT2 * total;

            for (int u = 1; u < TERRAIN_PATCH_SIZE; u++)
            {
                total = 0.0f;

                for (int n = 0; n < TERRAIN_PATCH_SIZE; n++)
                {
                    total += linein[lineSize + n] * CosineTable16[u * TERRAIN_PATCH_SIZE + n];
                }

                lineout[lineSize + u] = total;
            }
        }

        private static void DCTColumn16(float[] linein, int[] lineout, int column)
        {
            float total = 0.0f;
            const float oosob = 2.0f / 16.0f;

            for (int n = 0; n < TERRAIN_PATCH_SIZE; n++)
            {
                total += linein[TERRAIN_PATCH_SIZE * n + column];
            }

            lineout[CopyMatrix16[column]] = (int)(OO_SQRT2 * total * oosob * QuantizeTable16[column]);

            for (int u = 1; u < TERRAIN_PATCH_SIZE; u++)
            {
                total = 0.0f;

                for (int n = 0; n < TERRAIN_PATCH_SIZE; n++)
                {
                    total += linein[16 * n + column] * CosineTable16[u * TERRAIN_PATCH_SIZE + n];
                }

                lineout[CopyMatrix16[TERRAIN_PATCH_SIZE * u + column]] = (int)(total * oosob * QuantizeTable16[TERRAIN_PATCH_SIZE * u + column]);
            }
        }
        #endregion
    }
}
