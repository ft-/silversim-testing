// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;

namespace SilverSim.Viewer.Messages.LayerData
{
    public static partial class LayerCompressor
    {
        private struct PatchHeader
        {
            public float DCOffset;
            public int Range;
            public int QuantWBits;
            public uint PatchIDs;
        }

        public struct GroupHeader
        {
            public int Stride;
            public int PatchSize;
            public Messages.LayerData.LayerData.LayerDataType Type;
        }

        public const int MESSAGES_PER_WIND_LAYER_PACKET = 2;

        public const int LAYER_PATCH_ENTRY_WIDTH = 4;
        public const int LAYER_PATCH_NUM_XY_ENTRIES = 16;

        public const int PATCHES_PER_EDGE = 16;
        public const int END_OF_PATCHES = 97;

        private const float OO_SQRT2 = 0.7071067811865475244008443621049f;
        private const int STRIDE = 264;

        private const int ZERO_CODE = 0x0;
        private const int ZERO_EOB = 0x2;
        private const int POSITIVE_VALUE = 0x6;
        private const int NEGATIVE_VALUE = 0x7;

        private static readonly float[] DequantizeTable16 = new float[LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES];
        private static readonly float[] CosineTable16 = new float[LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES];
        private static readonly int[] CopyMatrix16 = new int[LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES];
        private static readonly float[] QuantizeTable16 = new float[LAYER_PATCH_NUM_XY_ENTRIES * LAYER_PATCH_NUM_XY_ENTRIES];


        static LayerCompressor()
        {
            for (int j = 0; j < LAYER_PATCH_NUM_XY_ENTRIES; j++)
            {
                for (int i = 0; i < LAYER_PATCH_NUM_XY_ENTRIES; i++)
                {
                    DequantizeTable16[j * 16 + i] = 1.0f + 2.0f * (float)(i + j);
                    QuantizeTable16[j * 16 + i] = 1.0f / (1.0f + 2.0f * ((float)i + (float)j));
                }
            }

            const float hposz = (float)Math.PI * 0.5f / 16.0f;

            for (int u = 0; u < LAYER_PATCH_NUM_XY_ENTRIES; u++)
            {
                for (int n = 0; n < LAYER_PATCH_NUM_XY_ENTRIES; n++)
                {
                    CosineTable16[u * 16 + n] = (float)Math.Cos((2.0f * (float)n + 1.0f) * (float)u * hposz);
                }
            }

            {
                bool diag = false;
                bool right = true;
                int i = 0;
                int j = 0;
                int count = 0;

                while (i < LAYER_PATCH_NUM_XY_ENTRIES && j < LAYER_PATCH_NUM_XY_ENTRIES)
                {
                    CopyMatrix16[j * 16 + i] = count++;

                    if (!diag)
                    {
                        if (right)
                        {
                            if (i < LAYER_PATCH_NUM_XY_ENTRIES - 1)
                            {
                                i++;
                            }
                            else
                            {
                                j++;
                            }

                            right = false;
                            diag = true;
                        }
                        else
                        {
                            if (j < LAYER_PATCH_NUM_XY_ENTRIES - 1)
                            {
                                j++;
                            }
                            else
                            {
                                i++;
                            }

                            right = true;
                            diag = true;
                        }
                    }
                    else
                    {
                        if (right)
                        {
                            i++;
                            j--;
                            if (i == LAYER_PATCH_NUM_XY_ENTRIES - 1 || j == 0)
                            {
                                diag = false;
                            }
                        }
                        else
                        {
                            i--;
                            j++;
                            if (j == LAYER_PATCH_NUM_XY_ENTRIES - 1 || i == 0)
                            {
                                diag = false;
                            }
                        }
                    }
                }
            }
        }
    }
}
