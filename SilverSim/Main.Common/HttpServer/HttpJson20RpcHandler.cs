// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Types.StructuredData.Json;
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
    public class HttpJson20RpcHandler : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("JSON2.0RPC SERVER");

        internal RwLockedDictionary<string, Func<string, IValue, IValue>> Json20RpcMethods = new RwLockedDictionary<string, Func<string, IValue, IValue>>();

        [Serializable]
        public class JSON20RpcException : Exception
        {
            public int StatusCode;

            public JSON20RpcException()
            {

            }

            public JSON20RpcException(int statusCode, string message)
                : base(message)
            {
                StatusCode = statusCode;
            }
        }

        void RequestHandler(HttpRequest httpreq)
        {
            IValue req;
            if (httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }
            
            IValue res;
            Map reqmap;
            AnArray reqarr;
            
            try
            {
                req = Json.Deserialize(httpreq.Body);
            }
            catch
            {
                req = null;
            }
            if(req == null)
            {
                res = FaultResponse(-32700, "Invalid JSON20 RPC Request", string.Empty);
            }
            else if(null != (reqmap = (req as Map)))
            {
                res = ProcessJsonRequest(reqmap);
            }
            else if(null != (reqarr = (req as AnArray)))
            {
                AnArray o = new AnArray();
                foreach (IValue v in reqarr)
                {
                    reqmap = v as Map;
                    if (null != reqmap)
                    {
                        o.Add(ProcessJsonRequest(reqmap));
                    }
                }
                res = o;
            }
            else
            {
                res = FaultResponse(-32700, "Invalid JSON20 RPC Request", string.Empty);
            }
            using (HttpResponse httpres = httpreq.BeginResponse("application/json-rpc"))
            {
                using (Stream o = httpres.GetOutputStream())
                {
                    Json.Serialize(res, o);
                }
            }
        }

        Map ProcessJsonRequest(Map req)
        {
            Func<string, IValue, IValue> del;
            string method = req["method"].ToString();
            if (Json20RpcMethods.TryGetValue(method, out del))
            {
                try
                {
                    Map res = new Map();
                    res.Add("jsonrpc", "2.0");
                    try
                    {
                        res.Add("result", del(method, req["params"]));
                    }
                    catch (JSON20RpcException je)
                    {
                        return FaultResponse(je.StatusCode, je.Message, req["id"].ToString());
                    }
                    res.Add("id", req["id"]);
                    return res;
                }
                catch (Exception e)
                {
                    m_Log.WarnFormat("Unexpected exception at XMRPC method {0}: {1}\n{2}", req["method"], e.GetType().Name, e.StackTrace);
                    return FaultResponse(-32700, "Internal service error", req["id"].ToString());
                }
            }
            else
            {
                return FaultResponse(-32601, "Unknown Method", req["id"].ToString());
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

        private Map FaultResponse(int statusCode, string statusMessage, string id)
        {
            Map error = new Map();
            error.Add("code", statusCode);
            error.Add("message", statusMessage);
            Map res = new Map();
            res.Add("jsonrpc", "2.0");
            res.Add("error", error);
            res.Add("id", id);
            return res;
        }
    }
}
