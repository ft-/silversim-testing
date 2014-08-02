﻿/*

ArribaSim is distributed under the terms of the
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
using Nwc.XmlRpc;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;
using ThreadedClasses;

namespace ArribaSim.Main.Common.HttpServer
{
    public class HttpXmlRpcHandler : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public delegate XmlRpcResponse XmlRpcDelegate(XmlRpcRequest req);

        public RwLockedDictionary<string, XmlRpcDelegate> XmlRpcMethods = new RwLockedDictionary<string,XmlRpcDelegate>();
        XmlRpcDeserializer m_XmlRpcDeserializer = new XmlRpcDeserializer();

        void RequestHandler(HttpRequest httpreq)
        {
            object o;
            XmlRpcRequest req;
            if(httpreq.Method != "POST")
            {
                HttpResponse httpres = httpreq.BeginResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                httpres.Close();
                return;
            }
            try
            {
                using (StreamReader s = new StreamReader(httpreq.Body))
                {
                    o = m_XmlRpcDeserializer.Deserialize(s);
                }
            }
            catch
            {
                FaultResponse(httpreq.BeginResponse(), -32700, "Invalid XML RPC Request");
                return;
            }

            if(!(o is XmlRpcRequest))
            {
                FaultResponse(httpreq.BeginResponse(), -32700, "Invalid XML RPC Request");
                return;
            }

            req = (XmlRpcRequest) o;
            XmlRpcDelegate del;
            XmlRpcResponse res;
            if(XmlRpcMethods.TryGetValue(req.MethodName, out del))
            {
                try
                {
                    res = del(req);
                }
                catch(Exception e)
                {
                    m_Log.WarnFormat("[XMLRPC SERVER]: Unexpected exception at XMRPC method {0}: {1}\n{2}", req.MethodName, e.GetType().Name, e.StackTrace.ToString());
                    FaultResponse(httpreq.BeginResponse(), -32700, "Internal service error");
                    return;
                }

                byte[] buffer = Encoding.UTF8.GetBytes(res.ToString());

                HttpResponse response = httpreq.BeginResponse();
                response.ContentType = "text/xml";
                response.GetOutputStream(buffer.LongLength).Write(buffer, 0, buffer.Length);
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
            m_Log.Info("[XMLRPC SERVER]: Initializing XMLRPC Handler");
            BaseHttpServer server = loader.GetService<BaseHttpServer>("HttpServer");
            server.RootUriContentTypeHandlers["text/xml"] = RequestHandler;
            server.RootUriContentTypeHandlers["application/xml"] = RequestHandler;
        }

        public void Shutdown()
        {
            m_Log.Info("[XMLRPC SERVER]: Deinitializing XMLRPC Handler");
            XmlRpcMethods.Clear();
        }

        private void FaultResponse(HttpResponse response, int statusCode, string statusMessage)
        {
            string s = String.Format("<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<methodResponse>" +
                        "<fault>" +
                            "<value>" +
                                "<struct>" +
                                    "<member>" +
                                        "<name>faultCode</name>" +
                                        "<value><int>{0}</int></value>" +
                                    "</member>" +
                                    "<member>" +
                                        "<name>faultString</name>" +
                                        "<value><string>{1}</string></value>" +
                                    "</member>" +
                                "</struct>" +
                            "</value>" +
                        "</fault>" +
                    "</methodResponse>",
                    statusCode,
                    XmlConvert.EncodeName(statusMessage));

            byte[] buffer = Encoding.UTF8.GetBytes(s);
            response.ContentType = "text/xml";
            response.GetOutputStream(buffer.LongLength).Write(buffer, 0, buffer.Length);
            response.Close();
        }
    }
}
