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

using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Terrain;
using SilverSim.Viewer.Messages.LayerData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace SilverSim.LoadStore.Terrain.Formats
{
    [TerrainStorageType]
    [Description("RAW32 Terrain Format")]
    public class RAW32 : ITerrainFileStorage, IPlugin
    {
        public string Name => "raw32";

        public List<LayerPatch> LoadFile(string filename, int suggested_width, int suggested_height)
        {
            using(var input = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                return LoadStream(input, suggested_width, suggested_height);
            }
        }

        public List<LayerPatch> LoadStream(Stream input, int suggested_width, int suggested_height)
        {
            var patches = new List<LayerPatch>();
            var vals = new float[LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, suggested_width];

            using(var bs = new BinaryReader(input))
            {
                for (uint patchy = 0; patchy < suggested_height / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++patchy)
                {
                    /* we have to load 16 lines at a time */
                    for(uint liney = 0; liney < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++liney)
                    {
                        for(uint x = 0; x < suggested_width; ++x)
                        {
                            vals[liney, x] = bs.ReadSingle();
                        }
                    }

                    /* now build patches from those 16 lines */
                    for(uint patchx = 0; patchx < suggested_width / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++patchx)
                    {
                        var patch = new LayerPatch
                        {
                            X = patchx,
                            Y = patchy
                        };
                        for (uint y = 0; y < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                        {
                            for(uint x = 0; x < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
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

        public void SaveFile(string filename, List<LayerPatch> terrain)
        {
            using(var output = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                SaveStream(output, terrain);
            }
        }

        public void SaveStream(Stream output, List<LayerPatch> terrain)
        {
            var outdata = new float[terrain.Count * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
            uint minX = terrain[0].X;
            uint minY = terrain[0].Y;
            uint maxX = terrain[0].X;
            uint maxY = terrain[0].Y;

            /* determine line width */
            foreach(LayerPatch p in terrain)
            {
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);
            }

            uint linewidth = maxX - minX + 1;

            /* build output data */
            foreach(LayerPatch p in terrain)
            {
                for(uint y = 0; y < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                {
                    for (uint x = 0; x < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                    {
                        outdata[LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES * y + x + p.XYToYNormal(linewidth, minY)] = p[x, y];
                    }
                }
            }

            using(var bs = new BinaryWriter(output))
            {
                foreach(float f in outdata)
                {
                    bs.Write(f);
                }
            }
        }

        public bool SupportsLoading => true;

        public bool SupportsSaving => true;

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
    }
}
