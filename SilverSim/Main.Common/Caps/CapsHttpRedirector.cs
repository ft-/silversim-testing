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

using log4net;
using SilverSim.Main.Common.HttpServer;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.ComponentModel;
using System.Net;

namespace SilverSim.Main.Common.Caps
{
    [Description("HTTP Capability Handler")]
    public class CapsHttpRedirector : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("CAPS HTTP REDIRECTOR");
        private BaseHttpServer m_HttpServer;

        public readonly RwLockedDictionaryAutoAdd<string, RwLockedDictionary<UUID, Action<HttpRequest>>> Caps = new RwLockedDictionaryAutoAdd<string, RwLockedDictionary<UUID, Action<HttpRequest>>>(() => new RwLockedDictionary<UUID, Action<HttpRequest>>());

        private void RequestHandler(HttpRequest httpreq)
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
                    return;
                }
            }

            /* prevent ? being taken as uuid part */
            string[] outerparts = httpreq.RawUrl.Split('?');
            string[] parts = outerparts[0].Substring(1).Split('/');

            if(parts.Length < 3)
            {
#if DEBUG
                m_Log.DebugFormat("Capability not found: Url invalid: {0}", httpreq.RawUrl);
#endif
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            UUID capsUUID;
            if(!UUID.TryParse(parts[2], out capsUUID))
            {
#if DEBUG
                m_Log.DebugFormat("Capability not found: UUID invalid: {0} in {1}", parts[2], httpreq.RawUrl);
#endif
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }

            RwLockedDictionary<UUID, Action<HttpRequest>> dict;
            if (!Caps.TryGetValue(parts[1], out dict) || !dict.TryGetValue(capsUUID, out del))
            {
#if DEBUG
                m_Log.DebugFormat("Capability not found: {0} for {1} in {2}", capsUUID, parts[1], httpreq.RawUrl);
#endif
                httpreq.ErrorResponse(HttpStatusCode.NotFound, "Not Found");
                return;
            }
            else
            {
                del(httpreq);
            }
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.Any;

        public string Scheme => m_HttpServer.Scheme;

        public string ExternalHostName => m_HttpServer.ExternalHostName;

        public uint Port => m_HttpServer.Port;

        public string ServerURI => m_HttpServer.ServerURI;

        public void Startup(ConfigurationLoader loader)
        {
            m_Log.Info("Initializing Caps Http Redirector");
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers["/CAPS/"] = RequestHandler;
            BaseHttpServer server;
            if(loader.TryGetHttpsServer(out server))
            {
                server.StartsWithUriHandlers["/CAPS/"] = RequestHandler;
            }
        }

        public void Shutdown()
        {
            m_Log.Info("Deinitializing Caps Http Redirector");
            Caps.Clear();
        }

        public string NewCapsURL(UUID uuid) => ServerURI + "CAPS/" + uuid.ToString() + "0000/";
        public static string NewCapsURL(string serverURI, UUID uuid) => serverURI + "CAPS/" + uuid.ToString() + "0000/";
    }
}
