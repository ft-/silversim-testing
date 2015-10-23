// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Viewer.Messages.LayerData;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Terrain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SilverSim.LoadStore.Terrain.Formats
{
    public abstract class DrawingSaveCommon : ITerrainFileStorage, IPlugin
    {
        public DrawingSaveCommon()
        {

        }

        public abstract string Name { get; }

        protected abstract ImageFormat TargetImageFormat { get; }

        public bool SupportsLoading
        {
            get
            {
                return false;
            }
        }

        public bool SupportsSaving
        {
            get
            {
                return true;
            }
        }

        public List<LayerPatch> LoadFile(string filename, int suggested_width, int suggested_height)
        {
            throw new NotSupportedException();
        }

        public List<LayerPatch> LoadStream(Stream input, int suggested_width, int suggested_height)
        {
            throw new NotSupportedException();
        }

        public void SaveFile(string filename, List<LayerPatch> terrain)
        {
            using (Bitmap bitmap = terrain.ToGrayScaleBitmap())
            {
                bitmap.Save(filename, TargetImageFormat);
            }
        }

        public void SaveStream(Stream output, List<LayerPatch> terrain)
        {
            using (Bitmap bitmap = terrain.ToGrayScaleBitmap())
            {
                bitmap.Save(output, TargetImageFormat);
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
    }
}
