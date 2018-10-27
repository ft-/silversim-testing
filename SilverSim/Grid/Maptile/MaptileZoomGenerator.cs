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

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Maptile;
using SilverSim.Types;
using SilverSim.Types.Maptile;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SilverSim.Grid.Maptile
{
    [Description("Generator for maptile zoom levels")]
    [PluginName("MaptileZoomGenerator")]
    public class MaptileZoomGenerator : MaptileServiceInterface, IPlugin
    {
        private readonly string m_MaptileServiceName;
        private MaptileServiceInterface m_MaptileService;
        private const int MaxZoomLevel = 10;

        public MaptileZoomGenerator(IConfig ownSection)
        {
            m_MaptileServiceName = ownSection.GetString("MaptileStorage", "MaptileStorage");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_MaptileService = loader.GetService<MaptileServiceInterface>(m_MaptileServiceName);
        }

        private static Image FromMaptileData(MaptileData data)
        {
            using (var ms = new MemoryStream(data.Data))
            {
                return new Bitmap(ms);
            }
        }

        public override bool TryGetValue(GridVector rawlocation, int zoomlevel, out MaptileData data)
        {
            data = null;
            if (zoomlevel < 1)
            {
                return false;
            }
            GridVector location = rawlocation.AlignToZoomlevel(zoomlevel);

            if(m_MaptileService.TryGetValue(location, zoomlevel, out data))
            {
                return true;
            }
            else if(zoomlevel > MaxZoomLevel || zoomlevel < 2)
            {
                return false;
            }
            var zoomsize = (uint)(256 << (zoomlevel - 1));

            MaptileData map00;
            MaptileData map01;
            MaptileData map10;
            MaptileData map11;
            MaptileData outmap;

            if (!TryGetValue(location, zoomlevel - 1, out map00))
            {
                map00 = null;
            }
            GridVector v = location;
            GridVector v2 = location;
            v2.X += zoomsize / 2;
            v2.Y += zoomsize / 2;

            if (m_MaptileService.TryGetValue(v, zoomlevel, out outmap))
            {
                List<MaptileInfo> upperInfo = m_MaptileService.GetUpdateTimes(v, v2, zoomlevel - 1);
                Date ownUpdate = outmap.LastUpdate;
                bool haveNewer = false;
                foreach (MaptileInfo up in upperInfo)
                {
                    if (up.LastUpdate.AsULong > ownUpdate.AsULong)
                    {
                        haveNewer = true;
                    }
                }
                if (!haveNewer)
                {
                    return true;
                }
            }
            else
            {
                outmap = new MaptileData
                {
                    Location = location,
                    ZoomLevel = zoomlevel,
                };
            }
            outmap.LastUpdate = Date.Now;
            outmap.ContentType = "image/jpeg";

            v.Y += zoomsize / 2;
            if (!TryGetValue(v, zoomlevel - 1, out map01))
            {
                map01 = null;
            }
            v = location;
            v.X += zoomsize / 2;
            if (!TryGetValue(location, zoomlevel - 1, out map10))
            {
                map10 = null;
            }
            v = location;
            v.X += zoomsize / 2;
            v.Y += zoomsize / 2;
            if (!TryGetValue(location, zoomlevel - 1, out map11))
            {
                map11 = null;
            }

            using (var bmp = new Bitmap(256, 256, PixelFormat.Format24bppRgb))
            {
                using (Graphics gfx = Graphics.FromImage(bmp))
                {
                    gfx.FillRectangle(Brushes.Blue, new Rectangle(0, 0, 256, 256));
                    if (map00 != null)
                    {
                        using (Image img = FromMaptileData(map00))
                        {
                            gfx.DrawImage(img, 0, 128, 128, 128);
                        }
                    }
                    if (map01 != null)
                    {
                        using (Image img = FromMaptileData(map01))
                        {
                            gfx.DrawImage(img, 0, 0, 128, 128);
                        }
                    }
                    if (map10 != null)
                    {
                        using (Image img = FromMaptileData(map10))
                        {
                            gfx.DrawImage(img, 128, 128, 128, 128);
                        }
                    }
                    if (map11 != null)
                    {
                        using (Image img = FromMaptileData(map11))
                        {
                            gfx.DrawImage(img, 128, 0, 128, 128);
                        }
                    }
                }
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, ImageFormat.Jpeg);
                    outmap.Data = ms.ToArray();
                }
            }

            m_MaptileService.Store(outmap);
            data = outmap;
            return true;
        }

        public override void Store(MaptileData data)
        {
            m_MaptileService.Store(data);
            int zoomlevel = data.ZoomLevel;
            GridVector location = data.Location;
            while (++zoomlevel < MaxZoomLevel)
            {
                location = location.AlignToZoomlevel(zoomlevel);
                m_MaptileService.Remove(location, zoomlevel);
            }
        }

        public override bool Remove(GridVector location, int zoomlevel)
        {
            bool removed = m_MaptileService.Remove(location, zoomlevel);
            while (++zoomlevel < MaxZoomLevel)
            {
                location = location.AlignToZoomlevel(zoomlevel);
                m_MaptileService.Remove(location, zoomlevel);
            }
            return removed;
        }

        public override List<MaptileInfo> GetUpdateTimes(GridVector minloc, GridVector maxloc, int zoomlevel)
        {
            return m_MaptileService.GetUpdateTimes(minloc, maxloc, zoomlevel);
        }
    }
}
