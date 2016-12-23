// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Threading;
using SilverSim.Types.StructuredData.XmlRpc;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;

namespace SilverSim.Main.Common.HttpServer
{
    [Description("HTTP XMLRPC Handler")]
    public class HttpXmlRpcHandler : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("XMLRPC SERVER");

        public RwLockedDictionary<string, Func<XmlRpc.XmlRpcRequest, XmlRpc.XmlRpcResponse>> XmlRpcMethods = new RwLockedDictionary<string, Func<XmlRpc.XmlRpcRequest, XmlRpc.XmlRpcResponse>>();

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void RequestHandler(HttpRequest httpreq)
        {
            XmlRpc.XmlRpcRequest req;
            if(httpreq.Method != "POST")
            {
                httpreq.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }
            try
            {
                req = XmlRpc.DeserializeRequest(httpreq.Body);
            }
            catch
#if DEBUG
                (Exception e)
#endif
            {
                FaultResponse(httpreq, -32700, "Invalid XML RPC Request");
                return;
            }
            req.CallerIP = httpreq.CallerIP;
            req.IsSsl = httpreq.IsSsl;

            Func<XmlRpc.XmlRpcRequest, XmlRpc.XmlRpcResponse> del;
            XmlRpc.XmlRpcResponse res;
            if(XmlRpcMethods.TryGetValue(req.MethodName, out del))
            {
                try
                {
                    res = del(req);
                }
                catch(XmlRpc.XmlRpcFaultException e)
                {
                    FaultResponse(httpreq, e.FaultCode, e.Message);
                    return;
                }
                catch(Exception e)
                {
                    m_Log.WarnFormat("Unexpected exception at XMRPC method {0}: {1}\n{2}", req.MethodName, e.GetType().Name, e.StackTrace);
                    FaultResponse(httpreq, -32700, "Internal service error");
                    return;
                }

                using (HttpResponse response = httpreq.BeginResponse())
                {
                    response.ContentType = "text/xml";
                    using (Stream o = response.GetOutputStream())
                    {
                        res.Serialize(o);
                    }
                }
            }
            else
            {
                FaultResponse(httpreq, -32601, "Unknown Method");
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
            m_Log.Info("Initializing XMLRPC Handler");
            BaseHttpServer server = loader.GetService<BaseHttpServer>("HttpServer");
            server.RootUriContentTypeHandlers["text/xml"] = RequestHandler;
            server.RootUriContentTypeHandlers["application/xml"] = RequestHandler;
        }

        public void Shutdown()
        {
            m_Log.Info("Deinitializing XMLRPC Handler");
            XmlRpcMethods.Clear();
        }

        private void FaultResponse(HttpRequest req, int statusCode, string statusMessage)
        {
            using (HttpResponse response = req.BeginResponse("text/xml"))
            {
                XmlRpc.XmlRpcFaultResponse res = new XmlRpc.XmlRpcFaultResponse();
                res.FaultCode = statusCode;
                res.FaultString = statusMessage;

                byte[] buffer = res.Serialize();
                response.GetOutputStream(buffer.LongLength).Write(buffer, 0, buffer.Length);
                response.Close();
            }
        }
    }
}
