// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
    public class MaptileZoomGenerator : MaptileServiceInterface, IPlugin
    {
        readonly string m_MaptileServiceName;
        MaptileServiceInterface m_MaptileService;
        const int MaxZoomLevel = 10;

        public MaptileZoomGenerator(IConfig ownSection)
        {
            m_MaptileServiceName = ownSection.GetString("MaptileStorage", "MaptileStorage");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_MaptileService = loader.GetService<MaptileServiceInterface>(m_MaptileServiceName);
        }

        static Image FromMaptileData(MaptileData data)
        {
            using (MemoryStream ms = new MemoryStream(data.Data))
            {
                return new Bitmap(ms);
            }
        }

        public override bool TryGetValue(UUID scopeid, GridVector location, int zoomlevel, out MaptileData data)
        {
            data = null;
            if(m_MaptileService.TryGetValue(scopeid, location, zoomlevel, out data))
            {
                return true;
            }
            else if(zoomlevel > MaxZoomLevel || zoomlevel < 2)
            {
                return false;
            }
            uint zoomsize = (uint)(256 << (zoomlevel - 1));

            MaptileData map00;
            MaptileData map01;
            MaptileData map10;
            MaptileData map11;
            MaptileData outmap;

            location = location.AlignToZoomlevel(zoomlevel);

            if (!TryGetValue(scopeid, location, zoomlevel - 1, out map00))
            {
                map00 = null;
            }
            GridVector v = location;
            GridVector v2 = location;
            v2.X += zoomsize / 2;
            v2.Y += zoomsize / 2;

            if (m_MaptileService.TryGetValue(scopeid, v, zoomlevel, out outmap))
            {
                List<MaptileInfo> upperInfo = m_MaptileService.GetUpdateTimes(scopeid, v, v2, zoomlevel - 1);
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
                outmap = new MaptileData();
                outmap.Location = location;
                outmap.ZoomLevel = zoomlevel;
                outmap.ScopeID = scopeid;
            }
            outmap.LastUpdate = Date.Now;
            outmap.ContentType = "image/jpeg";

            v.Y += zoomsize / 2;
            if (!TryGetValue(scopeid, v, zoomlevel - 1, out map01))
            {
                map01 = null;
            }
            v = location;
            v.X += zoomsize / 2;
            if (!TryGetValue(scopeid, location, zoomlevel - 1, out map10))
            {
                map10 = null;
            }
            v = location;
            v.X += zoomsize / 2;
            v.Y += zoomsize / 2;
            if (!TryGetValue(scopeid, location, zoomlevel - 1, out map11))
            {
                map11 = null;
            }

            using (Bitmap bmp = new Bitmap(256, 256, PixelFormat.Format24bppRgb))
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
                using (MemoryStream ms = new MemoryStream())
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
            UUID scopeid = data.ScopeID;
            while (++zoomlevel < MaxZoomLevel)
            {
                location = location.AlignToZoomlevel(zoomlevel);
                m_MaptileService.Remove(scopeid, location, zoomlevel);
            }
        }

        public override bool Remove(UUID scopeid, GridVector location, int zoomlevel)
        {
            bool removed = m_MaptileService.Remove(scopeid, location, zoomlevel);
            while (++zoomlevel < MaxZoomLevel)
            {
                location = location.AlignToZoomlevel(zoomlevel);
                m_MaptileService.Remove(scopeid, location, zoomlevel);
            }
            return removed;
        }

        public override List<MaptileInfo> GetUpdateTimes(UUID scopeid, GridVector minloc, GridVector maxloc, int zoomlevel)
        {
            return m_MaptileService.GetUpdateTimes(scopeid, minloc, maxloc, zoomlevel);
        }
    }

    [PluginName("MaptileZoomGenerator")]
    public class MaptileZoomGeneratorFactory : IPluginFactory
    {
        public MaptileZoomGeneratorFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new MaptileZoomGenerator(ownSection);
        }
    }
}
