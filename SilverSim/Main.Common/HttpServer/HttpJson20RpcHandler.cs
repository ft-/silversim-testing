// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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

        public delegate IValue Json20RpcDelegate(string method, IValue req);

        public RwLockedDictionary<string, Json20RpcDelegate> Json20RpcMethods = new RwLockedDictionary<string, Json20RpcDelegate>();

        public class JSON20RpcException : Exception
        {
            public int StatusCode;

            public JSON20RpcException(int statusCode, string message)
                : base(message)
            {
                StatusCode = statusCode;
            }
        }

        void RequestHandler(HttpRequest httpreq)
        {
            IValue req;
            HttpResponse httpres;
            if (httpreq.Method != "POST")
            {
                httpres = httpreq.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                httpres.Close();
                return;
            }
            
            IValue res; 
            
            try
            {
                req = JSON.Deserialize(httpreq.Body);
            }
            catch
            {
                req = null;
            }
            if(req == null)
            {
                res = FaultResponse(-32700, "Invalid JSON20 RPC Request", "");
            }
            else if(req is Map)
            {
                res = ProcessJsonRequest((Map)req);
            }
            else if(req is AnArray)
            {
                AnArray o = new AnArray();
                foreach(IValue v in (AnArray)req)
                {
                    if (v is Map)
                    {
                        o.Add(ProcessJsonRequest((Map)v));
                    }
                }
                res = o;
            }
            else
            {
                res = FaultResponse(-32700, "Invalid JSON20 RPC Request", "");
            }
            httpres = httpreq.BeginResponse("application/json-rpc");
            JSON.Serialize(res, httpres.GetOutputStream());
            httpres.Close();
        }

        Map ProcessJsonRequest(Map req)
        {
            Json20RpcDelegate del;
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
                    m_Log.WarnFormat("Unexpected exception at XMRPC method {0}: {1}\n{2}", req["method"], e.GetType().Name, e.StackTrace.ToString());
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
