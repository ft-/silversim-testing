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

using log4net;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Types;
using System;
using ThreadedClasses;

namespace SilverSim.Main.Common.Caps
{
    public class CapsHttpRedirector : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("CAPS HTTP REDIRECTOR");
        private BaseHttpServer m_HttpServer;

        public readonly RwLockedDictionaryAutoAdd<string, RwLockedDictionary<UUID, Action<HttpRequest>>> Caps = new RwLockedDictionaryAutoAdd<string, RwLockedDictionary<UUID, Action<HttpRequest>>>(delegate() { return new RwLockedDictionary<UUID, Action<HttpRequest>>();});

        void RequestHandler(HttpRequest httpreq)
        {
            Action<HttpRequest> del;

            if((httpreq.RawUrl.Length == 47 && httpreq.RawUrl.StartsWith("/CAPS/") && httpreq.RawUrl.EndsWith("0000/")) ||
                (httpreq.RawUrl.Length == 46 && httpreq.RawUrl.StartsWith("/CAPS/") && httpreq.RawUrl.EndsWith("0000")))
            {
                /* region seed */
                UUID regionSeedUuid;
                try
                {
                     regionSeedUuid = UUID.Parse(httpreq.RawUrl.Substring(6, 36));
                }
                catch
                {
                    HttpResponse res = httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found");
                    res.Close();
                    return;
                }

                if(!Caps.ContainsKey("SEED"))
                {
                    HttpResponse res = httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found");
                    res.Close();
                    return;
                }
                else if(!Caps["SEED"].TryGetValue(regionSeedUuid, out del))
                {
                    HttpResponse res = httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found");
                    res.Close();
                    return;
                }
                else
                {
                    del(httpreq);
                }
            }

            string[] parts = httpreq.RawUrl.Substring(1).Split('/');

            if(parts.Length != 3)
            {
                HttpResponse res = httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found");
                res.Close();
                return;
            }

            UUID capsUUID;
            try
            {
                capsUUID = UUID.Parse(parts[2]);
            }
            catch
            {
                HttpResponse res = httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found");
                res.Close();
                return;
            }

            RwLockedDictionary<UUID, Action<HttpRequest>> dict;
            if(!Caps.TryGetValue(parts[1], out dict))
            {
                HttpResponse res = httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found");
                res.Close();
                return;
            }
            else if(!dict.TryGetValue(capsUUID, out del))
            {
                HttpResponse res = httpreq.BeginResponse(HttpStatusCode.NotFound, "Not Found");
                res.Close();
                return;
            }
            else
            {
                del(httpreq);
            }
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.Any;
            }
        }

        public string ExternalHostName
        {
            get
            {
                return m_HttpServer.ExternalHostName;
            }
        }

        public uint Port
        {
            get
            {
                return m_HttpServer.Port;
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Log.Info("Initializing Caps Http Redirector");
            m_HttpServer = loader.GetService<BaseHttpServer>("HttpServer");
            m_HttpServer.UriHandlers["/CAPS/"] = RequestHandler;
        }

        public void Shutdown()
        {
            m_Log.Info("Deinitializing Caps Http Redirector");
            Caps.Clear();
        }
    }
}
