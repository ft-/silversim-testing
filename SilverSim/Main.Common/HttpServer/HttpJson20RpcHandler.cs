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
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.StructuredData.Json;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.Serialization;

namespace SilverSim.Main.Common.HttpServer
{
    [Description("HTTP JSON2.0RPC Handler")]
    public class HttpJson20RpcHandler : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("JSON2.0RPC SERVER");

        public RwLockedDictionary<string, Func<string, IValue, IValue>> Json20RpcMethods = new RwLockedDictionary<string, Func<string, IValue, IValue>>();

        [Serializable]
        public class JSON20RpcException : Exception
        {
            public int StatusCode;

            public JSON20RpcException()
            {
            }

            public JSON20RpcException(string message)
                : base(message)
            {
            }

            protected JSON20RpcException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }

            public JSON20RpcException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            public JSON20RpcException(int statusCode, string message)
                : base(message)
            {
                StatusCode = statusCode;
            }
        }

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
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


            if(null != (reqmap = (req as Map)))
            {
                res = ProcessJsonRequest(reqmap);
            }
            else if(null != (reqarr = (req as AnArray)))
            {
                var o = new AnArray();
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

            using (var httpres = httpreq.BeginResponse("application/json-rpc"))
            {
                using (var o = httpres.GetOutputStream())
                {
                    Json.Serialize(res, o);
                }
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        Map ProcessJsonRequest(Map req)
        {
            Func<string, IValue, IValue> del;
            string method = req["method"].ToString();
            if (Json20RpcMethods.TryGetValue(method, out del))
            {
                try
                {
                    var res = new Map();
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

        public ShutdownOrder ShutdownOrder => ShutdownOrder.Any;

        public void Startup(ConfigurationLoader loader)
        {
            m_Log.Info("Initializing JSON2.0RPC Handler");
            BaseHttpServer server = loader.HttpServer;
            server.RootUriContentTypeHandlers["application/json-rpc"] = RequestHandler;
            try
            {
                server = loader.HttpsServer;
                server.RootUriContentTypeHandlers["application/json-rpc"] = RequestHandler;
            }
            catch
            {
                /* intentionally left empty */
            }
        }

        public void Shutdown()
        {
            m_Log.Info("Deinitializing JSON2.0RPC Handler");
            Json20RpcMethods.Clear();
        }

        private Map FaultResponse(int statusCode, string statusMessage, string id) => new Map
        {
            { "jsonrpc", "2.0" },
            { "error", new Map
                {
                    { "code", statusCode },
                    { "message", statusMessage }
                }
            },
            { "id", id }
        };
    }
}
