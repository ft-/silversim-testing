// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Terrain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace SilverSim.LoadStore.Terrain.Formats
{
    [TerrainStorageType]
    [Description("Terragen Terrain Format")]
    public class Terragen : ITerrainFileStorage, IPlugin
    {
        public Terragen()
        {

        }

        public string Name
        {
            get
            {
                return "terragen";
            }
        }

        public List<LayerPatch> LoadFile(string filename, int suggested_width, int suggested_height)
        {
            using (Stream input = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                return LoadStream(input, suggested_width, suggested_height);
            }
        }

        public List<LayerPatch> LoadStream(System.IO.Stream input, int suggested_width, int suggested_height)
        {
            Encoding ascii = Encoding.ASCII;
            List<LayerPatch> patches = new List<LayerPatch>();

            using (BinaryReader bs = new BinaryReader(input))
            {
                if (ascii.GetString(bs.ReadBytes(16)) == "TERRAGENTERRAIN ")
                {
                    for (; ; )
                    {
                        switch (ascii.GetString(bs.ReadBytes(4)))
                        {
                            case "SIZE":
                                suggested_width = bs.ReadInt16() + 1;
                                suggested_height = suggested_width;
                                bs.ReadInt16();
                                break;

                            case "XPTS":
                                suggested_width = bs.ReadInt16();
                                bs.ReadInt16();
                                break;

                            case "YPTS":
                                suggested_height = bs.ReadInt16();
                                bs.ReadInt16();
                                break;

                            case "ALTW":
                                float heightScale = (float)bs.ReadInt16() / 65536f;
                                float baseHeight = (float)bs.ReadInt16();
                                float[,] vals = new float[LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES, suggested_width / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];

                                for (uint patchy = 0; patchy < suggested_height / LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++patchy)
                                {
                                    /* we have to load 16 lines at a time */
                                    for (uint liney = 0; liney < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++liney)
                                    {
                                        for (uint x = 0; x < suggested_width; ++x)
                                        {
                                            vals[liney, x] = baseHeight + (float)bs.ReadInt16() * heightScale;
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
                                return patches;

                            default:
                                bs.ReadInt32();
                                break;
                        }
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid file format");
                }
            }
        }

        public void SaveFile(string filename, List<LayerPatch> terrain)
        {
            using (Stream output = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                SaveStream(output, terrain);
            }
        }

        private byte[] ToLittleEndian(float number)
        {
            byte[] retVal = BitConverter.GetBytes(number);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(retVal);
            }
            return retVal;
        }

        public void SaveStream(System.IO.Stream output, List<LayerPatch> terrain)
        {
            Encoding ascii = Encoding.ASCII;
            short[] outdata = new short[terrain.Count * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES];
            uint linewidth = 0;
            double heightMax = terrain[0][0, 0];
            double heightMin = terrain[0][0, 0];
            uint minX = terrain[0].X;
            uint minY = terrain[0].Y;
            uint maxX = terrain[0].X;
            uint maxY = terrain[0].Y;

            /* determine line width */
            foreach (LayerPatch p in terrain)
            {
                minX = Math.Min(minX, p.X);
                minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X);
                maxY = Math.Max(maxY, p.Y);

                if (p.X + 1 > linewidth)
                {
                    linewidth = p.X + 1;
                }
                for(uint y = 0; y < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                {
                    for(uint x = 0; x < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                    {
                        heightMax = Math.Max(p[x, y], heightMax);
                        heightMin = Math.Min(p[x, y], heightMin);
                    }
                }
            }

            uint mapWidth = (maxX - minX + 1) * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;
            uint mapHeight = (maxY - minY + 1) * LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES;

            double baseHeight = Math.Floor((heightMax + heightMin) / 2);
            double horizontalScale = Math.Ceiling(heightMax - heightMin);
            double factor = 65536.0 / horizontalScale;

            /* build output data */
            foreach (LayerPatch p in terrain)
            {
                for (uint y = 0; y < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++y)
                {
                    for (uint x = 0; x < LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES; ++x)
                    {
                        float elev = (float)((p[x, y] - baseHeight) * factor);
                        elev = Math.Max(elev, Int16.MaxValue);
                        elev = Math.Min(elev, Int16.MinValue);
                        outdata[LayerCompressor.LAYER_PATCH_NUM_XY_ENTRIES * y + x + p.XYToYNormal(linewidth, minY)] = Convert.ToInt16(elev);
                    }
                }
            }

            using (BinaryWriter bs = new BinaryWriter(output))
            {
                bs.Write(ascii.GetBytes("TERRAGENTERRAIN "));

                bs.Write(ascii.GetBytes("SIZE"));
                bs.Write(Convert.ToInt16(mapWidth - 1));
                bs.Write(Convert.ToInt16(0));  // padding

                bs.Write(ascii.GetBytes("XPTS"));
                bs.Write(Convert.ToInt16(mapWidth));
                bs.Write(Convert.ToInt16(0));  // padding

                bs.Write(ascii.GetBytes("YPTS"));
                bs.Write(Convert.ToInt16(mapHeight));
                bs.Write(Convert.ToInt16(0));  // padding

                bs.Write(ascii.GetBytes("SCAL"));
                bs.Write(ToLittleEndian(1f)); // 1 terrain unit = 1 metre
                bs.Write(ToLittleEndian(1f));
                bs.Write(ToLittleEndian(1f));

                // now write the elevation data
                bs.Write(ascii.GetBytes("ALTW"));
                bs.Write(Convert.ToInt16(horizontalScale)); // range between max and min
                bs.Write(Convert.ToInt16(baseHeight)); // base height or mid point

                foreach (short f in outdata)
                {
                    bs.Write(f);
                }
            }
        }

        public bool SupportsLoading
        {
            get
            {
                return true;
            }
        }

        public bool SupportsSaving
        {
            get 
            {
                return true;
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
    }
}
