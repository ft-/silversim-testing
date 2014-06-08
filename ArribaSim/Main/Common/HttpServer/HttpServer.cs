/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using log4net;
using Nini.Config;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using ThreadedClasses;

namespace ArribaSim.Main.Common.HttpServer
{
    public class BaseHttpServer : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public delegate void HttpRequestDelegate(HttpListenerContext context);

        public RwLockedDictionary<string, HttpRequestDelegate> StartsWithUriHandlers = new RwLockedDictionary<string, HttpRequestDelegate>();
        public RwLockedDictionary<string, HttpRequestDelegate> UriHandlers = new RwLockedDictionary<string, HttpRequestDelegate>();
        public RwLockedDictionary<string, HttpRequestDelegate> RootUriContentTypeHandlers = new RwLockedDictionary<string, HttpRequestDelegate>();

        private HttpListener m_Listener;
        private uint m_Port;

        public BaseHttpServer(IConfig httpConfig)
        {
            if (!HttpListener.IsSupported)
            {
                m_Log.Fatal("HttpListener is not supported on this platform.");
                return;
            }
            m_Listener = new HttpListener();
            m_Port = (uint)httpConfig.GetInt("HttpListenerPort", 9000);
            m_Log.InfoFormat("Adding HTTP Server at port {0}", m_Port);
            m_Listener.Prefixes.Add(String.Format("http://+:{0}/", m_Port));
        }

        public void Startup(ConfigurationLoader loader)
        {
            //  netsh http add urlacl url=http://+:8008/ user=Everyone listen=yes
            m_Log.InfoFormat("Starting HTTP Server");
            m_Listener.Start();
            m_Listener.BeginGetContext(GetContextCallback, null);
        }

        public ShutdownOrder ShutdownOrder 
        { 
            get
            {
                return ShutdownOrder.Any;
            }
        }

        public void Shutdown()
        {
            m_Log.InfoFormat("Stopping HTTP Server");
            m_Listener.Stop();
            StartsWithUriHandlers.Clear();
            UriHandlers.Clear();
            RootUriContentTypeHandlers.Clear();
        }

        private void GetContextCallback(IAsyncResult ar)
        {
            HttpListenerContext context = m_Listener.EndGetContext(ar);
            m_Listener.BeginGetContext(GetContextCallback, null);
            HttpListenerRequest request = context.Request;

            HttpRequestDelegate del;
            HttpListenerResponse response = context.Response;
            if (request.RawUrl == "/" && RootUriContentTypeHandlers.TryGetValue(request.ContentType, out del))
            {
                try
                {
                    del(context);
                }
                catch (Exception e)
                {
                    m_Log.WarnFormat("[HTTP SERVER]: Unexpected exception at {0} {1}: {1}\n{2}", request.HttpMethod, request.RawUrl, e.GetType().Name, e.StackTrace.ToString());
                }
                response.OutputStream.Close();
            }
            else if (UriHandlers.TryGetValue(request.RawUrl, out del))
            {
                try
                {
                    del(context);
                }
                catch (Exception e)
                {
                    m_Log.WarnFormat("[HTTP SERVER]: Unexpected exception at {0} {1}: {1}\n{2}", request.HttpMethod, request.RawUrl, e.GetType().Name, e.StackTrace.ToString());
                }
                response.OutputStream.Close();
            }
            else
            {
                foreach(KeyValuePair<string, HttpRequestDelegate> kvp in StartsWithUriHandlers)
                {
                    if(request.RawUrl.StartsWith(kvp.Key))
                    {
                        try
                        {
                            del(context);
                        }
                        catch(Exception e)
                        {
                            m_Log.WarnFormat("[HTTP SERVER]: Unexpected exception at {0} {1}: {1}\n{2}", request.HttpMethod, request.RawUrl, e.GetType().Name, e.StackTrace.ToString());
                        }
                        response.OutputStream.Close();
                        return;
                    }
                }
                byte[] buffer = Encoding.UTF8.GetBytes(ErrorString);
                response.ContentLength64 = buffer.Length;
                response.StatusCode = 404;
                response.StatusDescription = "Not found";
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
        }

        private const string ErrorString = "<HTML><BODY>No knomes here for you to find.</BODY></HTML>";
    }
}
