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
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Maptile;
using SilverSim.Types;
using SilverSim.Types.Maptile;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace SilverSim.Grid.Mapserver
{
    [PluginName("MaptileHandler")]
    [Description("Map server")]
    public class MapHandler : IPlugin, ILoginResponseServiceInterface
    {
        private readonly string m_MaptileServiceName;
        private MaptileServiceInterface m_MaptileService;
        private Regex m_Regex = new Regex("/^map-(?<ZOOM>[0-9]+)-(?<X>[0-9]+)-(?<Y>[0-9]+)-.+\\.jpg$/");
        private UUID m_ScopeID = UUID.Zero;
        private BaseHttpServer m_HttpServer;
        private BaseHttpServer m_HttpsServer;

        public MapHandler(IConfig ownSection)
        {
            m_MaptileServiceName = ownSection.GetString("MaptileService", "MaptileService");
            m_ScopeID = UUID.Parse(ownSection.GetString("ScopeID", UUID.Zero.ToString()));
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_MaptileService = loader.GetService<MaptileServiceInterface>(m_MaptileServiceName);
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers.Add("/map-", HandleMap);
            try
            {
                m_HttpsServer = loader.HttpsServer;
            }
            catch
            {
                m_HttpsServer = null;
            }
            if(m_HttpsServer != null)
            {
                m_HttpsServer.StartsWithUriHandlers.Add("/map-", HandleMap);
            }
        }

        public void HandleMap(HttpRequest req)
        {
            if(req.Method != "GET")
            {
                req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            Match m = m_Regex.Match(req.RawUrl);
            if(m.Success)
            {
                string x_str = m.Groups["X"].Value;
                string y_str = m.Groups["Y"].Value;
                string zoom_str = m.Groups["ZOOM"].Value;
                ushort x;
                ushort y;
                int zoom;
                if(ushort.TryParse(x_str, out x) && ushort.TryParse(y_str, out y) && int.TryParse(zoom_str, out zoom))
                {
                    var gv = new GridVector
                    {
                        GridX = x,
                        GridY = y
                    };

                    MaptileData maptile;
                    if(m_MaptileService.TryGetValue(m_ScopeID, gv, zoom, out maptile))
                    {
                        using (HttpResponse res = req.BeginResponse(maptile.ContentType))
                        {
                            using (Stream s = res.GetOutputStream(maptile.Data.Length))
                            {
                                s.Write(maptile.Data, 0, maptile.Data.Length);
                            }
                        }
                    }
                    else
                    {
                        req.ErrorResponse(HttpStatusCode.NotFound, "Not found");
                    }
                }
                else
                {
                    req.ErrorResponse(HttpStatusCode.NotFound, "Not found");
                }
            }
            else
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not found");
            }
        }

        public void AppendLoginResponse(Map m)
        {
            m.Add("map-server-url", m_HttpServer.ServerURI);
        }
    }
}
