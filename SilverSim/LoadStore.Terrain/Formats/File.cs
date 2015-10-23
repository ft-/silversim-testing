// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Terrain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace SilverSim.LoadStore.Terrain.Formats
{
    [TerrainStorageType]
    public class File : ITerrainFileStorage, IPlugin
    {
        public File()
        {

        }

        public string Name
        {
            get 
            { 
                return "file"; 
            }
        }

        public List<LayerPatch> LoadFile(string filename, int suggested_width, int suggested_height)
        {
            using(Bitmap bitmap = new Bitmap(filename))
            {
                return bitmap.ToPatchesFromGrayscale();
            }
        }

        public List<LayerPatch> LoadStream(Stream input, int suggested_width, int suggested_height)
        {
            using(Bitmap bitmap = new Bitmap(input))
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
        }
    }
}
