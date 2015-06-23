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
using SilverSim.StructuredData.JSON;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ThreadedClasses;

namespace SilverSim.Main.Common.HttpServer
{
    class HttpJson20RpcHandler : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("JSON2.0RPC SERVER");

        public delegate Map Json20RpcDelegate(Map req);

        public RwLockedDictionary<string, Json20RpcDelegate> Json20RpcMethods = new RwLockedDictionary<string, Json20RpcDelegate>();

        private static Encoding UTF8NoBOM = new System.Text.UTF8Encoding(false);

        void RequestHandler(HttpRequest httpreq)
        {
            object o;
            Map req;
            if (httpreq.Method != "POST")
            {
                HttpResponse httpres = httpreq.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                httpres.Close();
                return;
            }
            try
            {
                req = (Map)JSON.Deserialize(httpreq.Body);
            }
            catch
            {
                FaultResponse(httpreq.BeginResponse(), -32700, "Invalid JSON20 RPC Request", "");
                return;
            }

            Json20RpcDelegate del;
            Map res;
            if (Json20RpcMethods.TryGetValue(req["method"].ToString(), out del))
            {
                try
                {
                    res = del(req);
                }
                catch (Exception e)
                {
                    m_Log.WarnFormat("Unexpected exception at XMRPC method {0}: {1}\n{2}", req["method"], e.GetType().Name, e.StackTrace.ToString());
                    FaultResponse(httpreq.BeginResponse(), -32700, "Internal service error", req["id"].ToString());
                    return;
                }

                HttpResponse response = httpreq.BeginResponse();
                response.ContentType = "application/json-rpc";
                using (TextWriter tw = new StreamWriter(response.GetOutputStream(), UTF8NoBOM))
                {
                    tw.Write(res.ToString());
                    tw.Flush();
                }
                response.Close();
            }
            else
            {
                FaultResponse(httpreq.BeginResponse(), -32601, "Unknown Method", req["id"].ToString());
            }
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.Any;
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Log.Info("Initializing JSON2.0RPC Handler");
            BaseHttpServer server = loader.GetService<BaseHttpServer>("HttpServer");
            server.RootUriContentTypeHandlers["application/json-rpc"] = RequestHandler;
        }

        public void Shutdown()
        {
            m_Log.Info("Deinitializing JSON2.0RPC Handler");
            Json20RpcMethods.Clear();
        }

        private void FaultResponse(HttpResponse response, int statusCode, string statusMessage, string id)
        {
            string s = String.Format("{{\"error\":{{" +
                    "\"jsonrpc\":\"2.0\"," +
                    "\"error:\":{{," +
                        "\"code\":{0}," +
                        "\"message\":{1}" +
                    "}}," +
                    "\"id\":\"{2}\"" +
                    "}}",
                    statusCode,
                    statusMessage,
                    id);

            byte[] buffer = Encoding.UTF8.GetBytes(s);
            response.ContentType = "application/json-rpc";
            response.GetOutputStream(buffer.LongLength).Write(buffer, 0, buffer.Length);
            response.Close();
        }
    }
}
