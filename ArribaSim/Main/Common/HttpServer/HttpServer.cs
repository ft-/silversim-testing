/*

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
using Nini.Config;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using ThreadedClasses;

namespace ArribaSim.Main.Common.HttpServer
{
    public class BaseHttpServer : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public delegate void HttpRequestDelegate(HttpRequest context);

        public RwLockedDictionary<string, HttpRequestDelegate> StartsWithUriHandlers = new RwLockedDictionary<string, HttpRequestDelegate>();
        public RwLockedDictionary<string, HttpRequestDelegate> UriHandlers = new RwLockedDictionary<string, HttpRequestDelegate>();
        public RwLockedDictionary<string, HttpRequestDelegate> RootUriContentTypeHandlers = new RwLockedDictionary<string, HttpRequestDelegate>();

        private TcpListener m_Listener;
        private uint m_Port;

        public BaseHttpServer(IConfig httpConfig)
        {
            if (!HttpListener.IsSupported)
            {
                m_Log.Fatal("HttpListener is not supported on this platform.");
                return;
            }
            m_Port = (uint)httpConfig.GetInt("HttpListenerPort", 9000);
            m_Listener = new TcpListener(new IPAddress(0), (int)m_Port);
            m_Log.InfoFormat("[HTTP SERVER]: Adding HTTP Server at port {0}", m_Port);
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Log.InfoFormat("[HTTP SERVER]: Starting HTTP Server");
            m_Listener.Start();
            m_Listener.BeginAcceptTcpClient(AcceptConnectionCallback, null);
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
            m_Log.InfoFormat("[HTTP SERVER]: Stopping HTTP Server");
            m_Listener.Stop();
            StartsWithUriHandlers.Clear();
            UriHandlers.Clear();
            RootUriContentTypeHandlers.Clear();
        }

        private void AcceptConnectionCallback(IAsyncResult ar)
        {
            TcpClient client = m_Listener.EndAcceptTcpClient(ar);
            m_Listener.BeginAcceptTcpClient(AcceptConnectionCallback, null);

            try
            {
                while (true)
                {
                    HttpRequest req;
                    try
                    {
                        req = new HttpRequest(client.GetStream());
                    }
                    catch (HttpResponse.ConnectionCloseException)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        m_Log.WarnFormat("[HTTP SERVER]: Unexpected exception: {0}\n{1}", e.GetType().Name, e.StackTrace.ToString());
                        return;
                    }

                    if ((req.Method == "POST" || req.Method == "PUT") && req.Body == null)
                    {
                        HttpResponse res = req.BeginResponse(HttpStatusCode.LengthRequired, "Length Required");
                        res.Close();
                    }

                    HttpRequestDelegate del;
                    if (req.RawUrl == "/" && RootUriContentTypeHandlers.TryGetValue(req.ContentType, out del))
                    {
                        try
                        {
                            del(req);
                        }
                        catch (HttpResponse.ConnectionCloseException)
                        {
                            return;
                        }
                        catch (Exception e)
                        {
                            m_Log.WarnFormat("[HTTP SERVER]: Unexpected exception at {0} {1}: {1}\n{2}", req.Method, req.RawUrl, e.GetType().Name, e.StackTrace.ToString());
                        }
                        req.Close();
                    }
                    else if (UriHandlers.TryGetValue(req.RawUrl, out del))
                    {
                        try
                        {
                            del(req);
                        }
                        catch (HttpResponse.ConnectionCloseException)
                        {
                            return;
                        }
                        catch (Exception e)
                        {
                            m_Log.WarnFormat("[HTTP SERVER]: Unexpected exception at {0} {1}: {1}\n{2}", req.Method, req.RawUrl, e.GetType().Name, e.StackTrace.ToString());
                        }
                        req.Close();
                    }
                    else
                    {
                        foreach (KeyValuePair<string, HttpRequestDelegate> kvp in StartsWithUriHandlers)
                        {
                            if (req.RawUrl.StartsWith(kvp.Key))
                            {
                                try
                                {
                                    del(req);
                                }
                                catch (HttpResponse.ConnectionCloseException)
                                {
                                    return;
                                }
                                catch (Exception e)
                                {
                                    m_Log.WarnFormat("[HTTP SERVER]: Unexpected exception at {0} {1}: {1}\n{2}", req.Method, req.RawUrl, e.GetType().Name, e.StackTrace.ToString());
                                }
                                req.Close();
                                return;
                            }
                        }

                        HttpResponse res = req.BeginResponse(HttpStatusCode.NotFound, "Not found");
                        byte[] buffer = Encoding.UTF8.GetBytes(ErrorString);
                        res.GetOutputStream(buffer.LongLength).Write(buffer, 0, buffer.Length);
                        res.Close();
                    }
                }
            }
            catch (HttpResponse.ConnectionCloseException)
            {
            }
            catch (Exception e)
            {
                m_Log.DebugFormat("[HTTP SERVER]: Exception: {0}\n{1}", e.GetType().Name, e.StackTrace.ToString());
            }
            finally
            {
                client.Close();
            }
        }

        private const string ErrorString = "<HTML><BODY>No knomes here for you to find.</BODY></HTML>";
    }
}
