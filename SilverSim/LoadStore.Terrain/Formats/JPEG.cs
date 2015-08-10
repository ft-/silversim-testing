// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.LL.Messages.LayerData;
using SilverSim.Main.Common;
using SilverSim.Scene.ServiceInterfaces.Terrain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SilverSim.LoadStore.Terrain.Formats
{
    [TerrainStorageType]
    public class JPEG : ITerrainFileStorage, IPlugin
    {
        static readonly Color[] m_GradientMap;

        static JPEG()
        {
            /* load gradient map from assembly resources */
            using (Stream s = typeof(MapConverters).Assembly.GetManifestResourceStream("SilverSim.LoadStore.Terrain.Resources.defaultstripe.png"))
            {
                using (Bitmap stripe = new Bitmap(s))
                {
                    m_GradientMap = new Color[stripe.Height];
                    for (int i = 0; i < stripe.Height; ++i)
                    {
                        m_GradientMap[i] = stripe.GetPixel(0, i);
                    }
                }
            }
        }


        public JPEG()
        {

        }

        public string Name 
        { 
            get
            {
                return "jpeg";
            }
        }

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
            throw new NotImplementedException();
        }

        public List<LayerPatch> LoadStream(Stream input, int suggested_width, int suggested_height)
        {
            throw new NotImplementedException();
        }

        public void SaveFile(string filename, List<LayerPatch> terrain)
        {
            using (Bitmap bitmap = terrain.ToBitmap(m_GradientMap))
            {
                bitmap.Save(filename, ImageFormat.Jpeg);
            }
        }

        public void SaveStream(Stream output, List<LayerPatch> terrain)
        {
            using (Bitmap bitmap = terrain.ToBitmap(m_GradientMap))
            {
                bitmap.Save(output, ImageFormat.Jpeg);
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
    }
}
