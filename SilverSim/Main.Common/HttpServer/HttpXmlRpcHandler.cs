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
using SilverSim.Types.StructuredData.XmlRpc;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;

namespace SilverSim.Main.Common.HttpServer
{
    [Description("HTTP XMLRPC Handler")]
    public class HttpXmlRpcHandler : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("XMLRPC SERVER");

        public RwLockedDictionary<string, Func<XmlRpc.XmlRpcRequest, XmlRpc.XmlRpcResponse>> XmlRpcMethods = new RwLockedDictionary<string, Func<XmlRpc.XmlRpcRequest, XmlRpc.XmlRpcResponse>>();

        private void RequestHandler(HttpRequest httpreq)
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

                if(res == null)
                {
                    /* disconnect thread support for HTTP XmlRPC handling */
                    throw new HttpResponse.DisconnectFromThreadException();
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

        public ShutdownOrder ShutdownOrder => ShutdownOrder.Any;

        public void Startup(ConfigurationLoader loader)
        {
            m_Log.Info("Initializing XMLRPC Handler");
            BaseHttpServer server = loader.HttpServer;
            server.RootUriContentTypeHandlers["text/xml"] = RequestHandler;
            server.RootUriContentTypeHandlers["application/xml"] = RequestHandler;
            if(loader.TryGetHttpsServer(out server))
            {
                server.RootUriContentTypeHandlers["text/xml"] = RequestHandler;
                server.RootUriContentTypeHandlers["application/xml"] = RequestHandler;
            }
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
                var res = new XmlRpc.XmlRpcFaultResponse
                {
                    FaultCode = statusCode,
                    FaultString = statusMessage
                };
                byte[] buffer = res.Serialize();
                response.GetOutputStream(buffer.LongLength).Write(buffer, 0, buffer.Length);
            }
        }
    }
}
