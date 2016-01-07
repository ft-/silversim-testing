// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace SilverSim.Main.Common.Caps
{
    [Description("HTTP Capability Handler")]
    public class CapsHttpRedirector : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("CAPS HTTP REDIRECTOR");
        private BaseHttpServer m_HttpServer;

        public readonly RwLockedDictionaryAutoAdd<string, RwLockedDictionary<UUID, Action<HttpRequest>>> Caps = new RwLockedDictionaryAutoAdd<string, RwLockedDictionary<UUID, Action<HttpRequest>>>(delegate() { return new RwLockedDictionary<UUID, Action<HttpRequest>>();});

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void RequestHandler(HttpRequest httpreq)
        {
            Action<HttpRequest> del;

            if((httpreq.RawUrl.Length == 47 && httpreq.RawUrl.StartsWith("/CAPS/") && httpreq.RawUrl.EndsWith("0000/")) ||
                (httpreq.RawUrl.Length == 46 && httpreq.RawUrl.StartsWith("/CAPS/") && httpreq.RawUrl.EndsWith("0000")))
            {
                /* region seed */
                UUID regionSeedUuid;
                if(!UUID.TryParse(httpreq.RawUrl.Substring(6, 36), out regionSeedUuid))
                {
                    httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    return;
                }

                if (!Caps.ContainsKey("SEED") || !Caps["SEED"].TryGetValue(regionSeedUuid, out del))
                {
                    httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                    return;
                }
                else
                {
                    del(httpreq);
                }
            }

            string[] parts = httpreq.RawUrl.Substring(1).Split('/');

            if(parts.Length < 3)
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            UUID capsUUID;
            if(!UUID.TryParse(parts[2], out capsUUID))
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            RwLockedDictionary<UUID, Action<HttpRequest>> dict;
            if (!Caps.TryGetValue(parts[1], out dict) || !dict.TryGetValue(capsUUID, out del))
            {
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
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

        public string Scheme
        {
            get
            {
                return m_HttpServer.Scheme;
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
            m_HttpServer.StartsWithUriHandlers["/CAPS/"] = RequestHandler;
        }

        public void Shutdown()
        {
            m_Log.Info("Deinitializing Caps Http Redirector");
            Caps.Clear();
        }
    }
}
