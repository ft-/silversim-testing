using System;

namespace SilverSim.LL.Core.LayerData
{
    public static partial class LayerCompressor
    {
        private struct PatchHeader
        {
            public float DCOffset;
            public int Range;
            public int QuantWBits;
            public int PatchIDs;

            public int X
            {
                get { return PatchIDs >> 5; }
                set { PatchIDs += (value << 5); }
            }

            public int Y
            {
                get { return PatchIDs & 0x1F; }
                set { PatchIDs |= value & 0x1F; }
            }
        }

        public struct GroupHeader
        {
            public int Stride;
            public int PatchSize;
            public Messages.LayerData.LayerData.LayerDataType Type;
        }

        public const int TERRAIN_PATCH_SIZE = 16;

        public const int PATCHES_PER_EDGE = 16;
        public const int END_OF_PATCHES = 97;

        private const float OO_SQRT2 = 0.7071067811865475244008443621049f;
        private const int STRIDE = 264;

        private const int ZERO_CODE = 0x0;
        private const int ZERO_EOB = 0x2;
        private const int POSITIVE_VALUE = 0x6;
        private const int NEGATIVE_VALUE = 0x7;

        private static readonly float[] DequantizeTable16 = new float[TERRAIN_PATCH_SIZE * TERRAIN_PATCH_SIZE];
        private static readonly float[] CosineTable16 = new float[TERRAIN_PATCH_SIZE * TERRAIN_PATCH_SIZE];
        private static readonly int[] CopyMatrix16 = new int[TERRAIN_PATCH_SIZE * TERRAIN_PATCH_SIZE];
        private static readonly float[] QuantizeTable16 = new float[TERRAIN_PATCH_SIZE * TERRAIN_PATCH_SIZE];


        static LayerCompressor()
        {
            for (int j = 0; j < TERRAIN_PATCH_SIZE; j++)
            {
                for (int i = 0; i < TERRAIN_PATCH_SIZE; i++)
                {
                    DequantizeTable16[j * 16 + i] = 1.0f + 2.0f * (float)(i + j);
                    QuantizeTable16[j * 16 + i] = 1.0f / (1.0f + 2.0f * ((float)i + (float)j));
                }
            }

            const float hposz = (float)Math.PI * 0.5f / 16.0f;

            for (int u = 0; u < TERRAIN_PATCH_SIZE; u++)
            {
                for (int n = 0; n < TERRAIN_PATCH_SIZE; n++)
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

                while (i < TERRAIN_PATCH_SIZE && j < TERRAIN_PATCH_SIZE)
                {
                    CopyMatrix16[j * 16 + i] = count++;

                    if (!diag)
                    {
                        if (right)
                        {
                            if (i < TERRAIN_PATCH_SIZE - 1) i++;
                            else j++;

                            right = false;
                            diag = true;
                        }
                        else
                        {
                            if (j < TERRAIN_PATCH_SIZE - 1) j++;
                            else i++;

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
                            if (i == TERRAIN_PATCH_SIZE - 1 || j == 0) diag = false;
                        }
                        else
                        {
                            i--;
                            j++;
                            if (j == TERRAIN_PATCH_SIZE - 1 || i == 0) diag = false;
                        }
                    }
                }
            }
        }
    }
}
