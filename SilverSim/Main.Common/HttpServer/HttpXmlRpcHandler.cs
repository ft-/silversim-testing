// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using SilverSim.Types.StructuredData.XMLRPC;
using System;
using System.Net;
using ThreadedClasses;

namespace SilverSim.Main.Common.HttpServer
{
    public class HttpXmlRpcHandler : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("XMLRPC SERVER");

        public delegate XMLRPC.XmlRpcResponse XmlRpcDelegate(XMLRPC.XmlRpcRequest req);

        public RwLockedDictionary<string, XmlRpcDelegate> XmlRpcMethods = new RwLockedDictionary<string,XmlRpcDelegate>();

        void RequestHandler(HttpRequest httpreq)
        {
            XMLRPC.XmlRpcRequest req;
            if(httpreq.Method != "POST")
            {
                HttpResponse httpres = httpreq.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                httpres.Close();
                return;
            }
            try
            {
                req = XMLRPC.DeserializeRequest(httpreq.Body);
            }
            catch
#if DEBUG
                (Exception e)
#endif
            {
                FaultResponse(httpreq.BeginResponse(), -32700, "Invalid XML RPC Request");
                return;
            }

            XmlRpcDelegate del;
            XMLRPC.XmlRpcResponse res;
            if(XmlRpcMethods.TryGetValue(req.MethodName, out del))
            {
                try
                {
                    res = del(req);
                }
                catch(XMLRPC.XmlRpcFaultException e)
                {
                    FaultResponse(httpreq.BeginResponse(), e.FaultCode, e.Message);
                    return;
                }
                catch(Exception e)
                {
                    m_Log.WarnFormat("Unexpected exception at XMRPC method {0}: {1}\n{2}", req.MethodName, e.GetType().Name, e.StackTrace.ToString());
                    FaultResponse(httpreq.BeginResponse(), -32700, "Internal service error");
                    return;
                }

                HttpResponse response = httpreq.BeginResponse();
                response.ContentType = "text/xml";
                res.Serialize(response.GetOutputStream());
                response.Close();
            }
            else
            {
                FaultResponse(httpreq.BeginResponse(), -32601, "Unknown Method");
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

        private void FaultResponse(HttpResponse response, int statusCode, string statusMessage)
        {
            XMLRPC.XmlRpcFaultResponse res = new XMLRPC.XmlRpcFaultResponse();
            res.FaultCode = statusCode;
            res.FaultString = statusMessage;

            byte[] buffer = res.Serialize();
            response.ContentType = "text/xml";
            response.GetOutputStream(buffer.LongLength).Write(buffer, 0, buffer.Length);
            response.Close();
        }
    }
}
