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

using SilverSim.LL.Messages.LayerData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SilverSim.Archiver.OAR
{
    public static partial class OAR
    {
        static class TerrainLoader
        {
            public static List<LayerPatch> LoadStream(Stream input, int suggested_width, int suggested_height)
            {
                List<LayerPatch> patches = new List<LayerPatch>();
                float[,] vals = new float[LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, suggested_width / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];

                using (BinaryReader bs = new BinaryReader(input))
                {
                    for (uint patchy = 0; patchy < suggested_height / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++patchy)
                    {
                        /* we have to load 16 lines at a time */
                        for (uint liney = 0; liney < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++liney)
                        {
                            for (uint x = 0; x < suggested_width; ++x)
                            {
                                vals[liney, x] = bs.ReadSingle();
                            }
                        }

                        /* now build patches from those 16 lines */
                        for (uint patchx = 0; patchx < suggested_width / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++patchx)
                        {
                            LayerPatch patch = new LayerPatch();
                            patch.X = patchx;
                            patch.Y = patchy;
                            for (uint y = 0; y < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                            {
                                for (uint x = 0; x < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                                {
                                    patch[x, y] = vals[y, x + patchx * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
                                }
                            }
                            patches.Add(patch);
                        }
                    }
                }

                return patches;
            }
        }
    }
}
