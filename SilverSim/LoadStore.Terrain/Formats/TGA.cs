using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Terrain;
using SilverSim.Viewer.Messages.LayerData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;

namespace SilverSim.LoadStore.Terrain.Formats
{
    [TerrainStorageType]
    [Description("Targa Terrain Format")]
    public class TGA : ITerrainFileStorage, IPlugin
    {
        public TGA()
        {

        }

        public string Name
        {
            get
            {
                return "tga";
            }
        }

        public List<LayerPatch> LoadFile(string filename, int suggested_width, int suggested_height)
        {
            using (Bitmap bitmap = Paloma.TargaImage.LoadTargaImage(filename))
            {
                return bitmap.ToPatchesFromGrayscale();
            }
        }

        public List<LayerPatch> LoadStream(Stream input, int suggested_width, int suggested_height)
        {
            using (Bitmap bitmap = Paloma.TargaImage.LoadTargaImage(input))
            {
                return bitmap.ToPatchesFromGrayscale();
            }
        }

        public void SaveFile(string filename, List<LayerPatch> terrain)
        {
            throw new NotSupportedException();
        }

        public void SaveStream(Stream output, List<LayerPatch> terrain)
        {
            throw new NotSupportedException();
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
                return false;
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }
    }
}
