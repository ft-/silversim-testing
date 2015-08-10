// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common.Http;
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

            m_Listener = new TcpListener(IPAddress.Any, (int)Port);
            m_Listener.Server.Ttl = 128;

            m_Log.InfoFormat("Adding HTTP Server at port {0}", Port);
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Log.InfoFormat("Starting HTTP Server");
            m_Listener.Start();
            m_Listener.BeginAcceptSocket(AcceptConnectionCallback, null);
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
            Socket socket;
            try
            {
                socket = m_Listener.EndAcceptSocket(ar);
            }
            catch(ObjectDisposedException)
            {
                return;
            }
            m_Listener.BeginAcceptSocket(AcceptConnectionCallback, null);
            try
            {
                Stream httpstream;
                if (m_ServerCertificate != null)
                {
                    /* Start SSL handshake */
                    SslStream sslstream = new SslStream(new NetworkStream(socket));
                    sslstream.AuthenticateAsServer(m_ServerCertificate);
                    httpstream = sslstream;
                }
                else
                {
                    httpstream = new HttpStream(socket);
                }

                while (true)
                {
                    HttpRequest req;
                    try
                    {
                        string remoteAddr = IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString()).ToString();
                        req = new HttpRequest(httpstream, remoteAddr, m_IsBehindProxy);
                    }
                    catch (HttpResponse.ConnectionCloseException)
                    {
                        return;
                    }
                    catch(IOException)
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
                            socket = null;
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
                            socket = null;
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
                                    socket = null;
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
                    httpstream.ReadTimeout = 10000;
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
                if (null != socket)
                {
                    socket.Close();
                }
            }
        }

        private const string ErrorString = "<HTML><BODY>No knomes here for you to find.</BODY></HTML>";
    }
}
