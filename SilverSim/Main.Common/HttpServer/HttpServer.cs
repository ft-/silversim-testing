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
using Nini.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ThreadedClasses;

namespace SilverSim.Main.Common.HttpServer
{
    public class BaseHttpServer : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("HTTP SERVER");

        public delegate void HttpRequestDelegate(HttpRequest context);

        public RwLockedDictionary<string, HttpRequestDelegate> StartsWithUriHandlers = new RwLockedDictionary<string, HttpRequestDelegate>();
        public RwLockedDictionary<string, HttpRequestDelegate> UriHandlers = new RwLockedDictionary<string, HttpRequestDelegate>();
        public RwLockedDictionary<string, HttpRequestDelegate> RootUriContentTypeHandlers = new RwLockedDictionary<string, HttpRequestDelegate>();

        private TcpListener m_Listener;
        public uint Port { get; private set; }
        public string ExternalHostName { get; private set; }
        public string Scheme { get; private set; }

        private bool m_IsBehindProxy = false;

        X509Certificate m_ServerCertificate = null;

        public BaseHttpServer(IConfig httpConfig)
        {
            Port = (uint)httpConfig.GetInt("HttpListenerPort", 9000);
            m_IsBehindProxy = httpConfig.GetBoolean("HasProxy", false);
            ExternalHostName = httpConfig.GetString("ExternalHostName", "SYSTEMIP");

            if(httpConfig.Contains("ServerCertificate"))
            {
                string filename = httpConfig.GetString("ServerCertificate");
                m_ServerCertificate = X509Certificate.CreateFromCertFile(filename);
                Scheme = Uri.UriSchemeHttps;
            }
            else
            {
                Scheme = Uri.UriSchemeHttp;
            }

            m_Listener = new TcpListener(new IPAddress(0), (int)Port);
            m_Listener.Server.Ttl = 128;

            m_Log.InfoFormat("Adding HTTP Server at port {0}", Port);
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Log.InfoFormat("Starting HTTP Server");
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
            m_Log.InfoFormat("Stopping HTTP Server");
            m_Listener.Stop();
            StartsWithUriHandlers.Clear();
            UriHandlers.Clear();
            RootUriContentTypeHandlers.Clear();
        }

        private void AcceptConnectionCallback(IAsyncResult ar)
        {
            TcpClient client;
            try
            {
                client = m_Listener.EndAcceptTcpClient(ar);
            }
            catch(ObjectDisposedException)
            {
                return;
            }
            m_Listener.BeginAcceptTcpClient(AcceptConnectionCallback, null);
            try
            {
                Stream httpstream = client.GetStream();
                if (m_ServerCertificate != null)
                {
                    /* Start SSL handshake */
                    SslStream sslstream = new SslStream(httpstream);
                    sslstream.AuthenticateAsServer(m_ServerCertificate);
                    httpstream = sslstream;
                }

                while (true)
                {
                    HttpRequest req;
                    try
                    {
                        string remoteAddr = IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString()).ToString();
                        req = new HttpRequest(httpstream, remoteAddr, m_IsBehindProxy);
                    }
                    catch (HttpResponse.ConnectionCloseException)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        m_Log.WarnFormat("Unexpected exception: {0}\n{1}", e.GetType().Name, e.StackTrace.ToString());
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
                        catch(HttpResponse.DisconnectFromThreadException)
                        {
                            client = null;
                            return;
                        }
                        catch (Exception e)
                        {
                            m_Log.WarnFormat("Unexpected exception at {0} {1}: {1}\n{2}", req.Method, req.RawUrl, e.GetType().Name, e.StackTrace.ToString());
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
                        catch (HttpResponse.DisconnectFromThreadException)
                        {
                            client = null;
                            return;
                        }
                        catch (Exception e)
                        {
                            m_Log.WarnFormat("Unexpected exception at {0} {1}: {1}\n{2}", req.Method, req.RawUrl, e.GetType().Name, e.StackTrace.ToString());
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
                                    kvp.Value(req);
                                }
                                catch (HttpResponse.ConnectionCloseException)
                                {
                                    return;
                                }
                                catch (HttpResponse.DisconnectFromThreadException)
                                {
                                    client = null;
                                    return;
                                }
                                catch (Exception e)
                                {
                                    m_Log.WarnFormat("Unexpected exception at {0} {1}: {2}\n{3}", req.Method, req.RawUrl, e.GetType().Name, e.StackTrace.ToString());
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
            catch (IOException)
            {
                /* commonly a broken pipe */
            }
            catch (Exception e)
            {
                m_Log.DebugFormat("Exception: {0}\n{1}", e.GetType().Name, e.StackTrace.ToString());
            }
            finally
            {
                if (null != client)
                {
                    client.Close();
                }
            }
        }

        private const string ErrorString = "<HTML><BODY>No knomes here for you to find.</BODY></HTML>";
    }
}
