// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Http;
using SilverSim.ServiceInterfaces;
using SilverSim.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace SilverSim.Main.Common.HttpServer
{
    [Description("HTTP Server")]
    public class BaseHttpServer : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("HTTP SERVER");

        public RwLockedDictionary<string, Action<HttpRequest>> StartsWithUriHandlers = new RwLockedDictionary<string, Action<HttpRequest>>();
        public RwLockedDictionary<string, Action<HttpRequest>> UriHandlers = new RwLockedDictionary<string, Action<HttpRequest>>();
        public RwLockedDictionary<string, Action<HttpRequest>> RootUriContentTypeHandlers = new RwLockedDictionary<string, Action<HttpRequest>>();

        public uint Port { get; private set; }
        ExternalHostNameServiceInterface m_ExternalHostNameService;
        public string ExternalHostName
        {
            get
            {
                return m_ExternalHostNameService.ExternalHostName;
            }
        }

        public string ServerURI
        {
            get
            {
                return string.Format("{0}://{1}:{2}/", Scheme, ExternalHostName, Port);
            }
        }
        public string Scheme { get; private set; }

        readonly bool m_IsBehindProxy;
        bool m_StoppingListeners;

        X509Certificate2 m_ServerCertificate;
        string m_CertificateFileName;
        Type m_SslStreamPreload;
        Socket m_ListenerSocket;
        Thread m_ListenerThread;

        public BaseHttpServer(IConfig httpConfig, bool useSsl = false)
        {
            Port = (uint)httpConfig.GetInt("Port", useSsl ? 9001 : 9000);
            m_IsBehindProxy = httpConfig.GetBoolean("HasProxy", false);
            /* prevent Mono from lazy loading SslStream at shutdown */
            m_SslStreamPreload = typeof(SslStream);

            if(httpConfig.Contains("ServerCertificate"))
            {
                m_CertificateFileName = httpConfig.GetString("ServerCertificate");
                Scheme = Uri.UriSchemeHttps;
            }
            else if(useSsl)
            {
                m_CertificateFileName = "../data/server-cert.p12";
                Scheme = Uri.UriSchemeHttps;
            }
            else
            {
                Scheme = Uri.UriSchemeHttp;
            }

            m_ListenerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            try
            {
                if (m_ListenerSocket.Ttl < 128)
                {
                    m_ListenerSocket.Ttl = 128;
                }
            }
            catch (SocketException)
            {
                m_Log.Debug("Failed to increase default TTL");
            }

            /* since Win 2000, there is a WSAECONNRESET, we do not want that in our code */
            try
            {
                const int SIO_UDP_CONNRESET = -1744830452;

                m_ListenerSocket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null);
            }
            catch (SocketException)
            {
                /* however, mono does not have an idea about what this is all about, so we catch that here */
            }

            IPEndPoint ep = new IPEndPoint(IPAddress.Any, (int)Port);
            m_ListenerSocket.Bind(ep);
            m_ListenerSocket.Listen(128);

            m_ListenerThread = new Thread(AcceptThread);
            m_ListenerThread.IsBackground = true;
            m_ListenerThread.Start();

            if (Scheme == Uri.UriSchemeHttps)
            {
                m_Log.InfoFormat("Adding HTTPS Server at port {0}", Port);
            }
            else
            {
                m_Log.InfoFormat("Adding HTTP Server at port {0}", Port);
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_ExternalHostNameService = loader.ExternalHostNameService;
            if(!File.Exists(m_CertificateFileName) && Scheme == Uri.UriSchemeHttps)
            {
                SslSelfSignCertUtil.GenerateSelfSignedServiceCertificate(m_CertificateFileName, m_ExternalHostNameService.ExternalHostName);
            }
            if (Scheme == Uri.UriSchemeHttps)
            {
                m_ServerCertificate = new X509Certificate2(m_CertificateFileName);
                m_Log.InfoFormat("Starting HTTPS Server");
            }
            else
            {
                m_Log.InfoFormat("Starting HTTP Server");
            }
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
            m_StoppingListeners = true;
            while(m_ActiveThreadCount > 0)
            {
                Thread.Sleep(1);
            }
            StartsWithUriHandlers.Clear();
            UriHandlers.Clear();
            RootUriContentTypeHandlers.Clear();
        }

        int m_ActiveThreadCount;
        AutoResetEvent m_AsyncListenerEvent = new AutoResetEvent(false);

        private void AcceptThread()
        {
            Thread.CurrentThread.Name = Scheme.ToUpper() + " Server at " + Port.ToString();
            Interlocked.Increment(ref m_ActiveThreadCount);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += AcceptHandler;
            while (!m_StoppingListeners)
            {
                args.AcceptSocket = null;
                if (m_ListenerSocket.AcceptAsync(args))
                {
                    while (!m_AsyncListenerEvent.WaitOne(1000))
                    {
                        if(m_StoppingListeners)
                        {
                            break;
                        }
                    }
                }

                Interlocked.Increment(ref m_ActiveThreadCount);
                Thread t = new Thread(AcceptedConnection);
                t.IsBackground = true;
                t.Start(args.AcceptSocket);
                while(m_ActiveThreadCount > 200)
                {
                    Thread.Sleep(1);
                }
            }
            Interlocked.Decrement(ref m_ActiveThreadCount);
        }

        private void AcceptHandler(object o, SocketAsyncEventArgs args)
        {
            m_AsyncListenerEvent.Set();
        }

        static string AddressToString(IPAddress ipAddr)
        {
            if(ipAddr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                byte[] b = ipAddr.GetAddressBytes();
                if(b[0] == 0 &&
                    b[1] == 0 &&
                    b[2] == 0 &&
                    b[3] == 0 &&
                    b[4] == 0 &&
                    b[5] == 0 &&
                    b[6] == 0 &&
                    b[7] == 0 &&
                    b[8] == 0 &&
                    b[9] == 0 &&
                    b[10] == 0xFF &&
                    b[11] == 0xFF)
                {
                    return string.Format("{0}.{1}.{2}.{3}", b[12], b[13], b[14], b[15]);
                }
            }
            return ipAddr.ToString();
        }

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        private void AcceptedConnection(object socko)
        {
            Socket socket = (Socket)socko;
            try
            {
                IPEndPoint ep = (IPEndPoint)socket.RemoteEndPoint;
                string remoteAddr = AddressToString(ep.Address);
                Thread.CurrentThread.Name = Scheme.ToUpper() + " Server for " + remoteAddr + " at " + Port.ToString();

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
                        req = new HttpRequest(httpstream, remoteAddr, m_IsBehindProxy);
                    }
                    catch (HttpResponse.ConnectionCloseException)
                    {
                        return;
                    }
                    catch(HttpStream.TimeoutException)
                    {
                        return;
                    }
                    catch(TimeoutException)
                    {
                        return;
                    }
                    catch(IOException)
                    {
                        return;
                    }
                    catch(InvalidDataException)
                    {
                        return;
                    }
                    catch(ObjectDisposedException)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        m_Log.WarnFormat("Unexpected exception: {0}: {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                        return;
                    }

                    if ((req.Method == "POST" || req.Method == "PUT") && req.Body == null)
                    {
                        HttpResponse res = req.BeginResponse(HttpStatusCode.LengthRequired, "Length Required");
                        res.Close();
                    }

                    Action<HttpRequest> del;
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
                            m_Log.WarnFormat("(Content Handler): Unexpected exception at {0} {1}: {1}\n{2}", req.Method, req.RawUrl, e.GetType().Name, e.StackTrace);
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
                            m_Log.WarnFormat("(Uri Handler): Unexpected exception at {0} {1}: {1}\n{2}", req.Method, req.RawUrl, e.GetType().Name, e.StackTrace);
                        }
                        req.Close();
                    }
                    else
                    {
                        foreach (KeyValuePair<string, Action<HttpRequest>> kvp in StartsWithUriHandlers)
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
                                    m_Log.WarnFormat("(StartUriHandler): Unexpected exception at {0} {1}: {2}\n{3}", req.Method, req.RawUrl, e.GetType().Name, e.StackTrace);
                                }
                                req.Close();
                                return;
                            }
                        }

                        using (HttpResponse res = req.BeginResponse(HttpStatusCode.NotFound, "Not found"))
                        {
                            byte[] buffer = Encoding.UTF8.GetBytes(ErrorString);
                            res.GetOutputStream(buffer.LongLength).Write(buffer, 0, buffer.Length);
                        }
                    }
                    httpstream.ReadTimeout = 10000;
                }
            }
            catch (HttpResponse.ConnectionCloseException)
            {
                /* simply a closed connection */
            }
            catch (IOException)
            {
                /* commonly a broken pipe */
            }
            catch(SocketException)
            {
                /* commonly a broken pipe */
            }
            catch(ObjectDisposedException)
            {
                /* commonly a broken pipe */
            }
            catch (Exception e)
            {
                m_Log.DebugFormat("Exception: {0}: {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace);
            }
            finally
            {
                if (null != socket)
                {
                    socket.Close();
                }
                Interlocked.Decrement(ref m_ActiveThreadCount);
            }
        }

        private const string ErrorString = "<HTML><BODY>No knomes here for you to find.</BODY></HTML>";
    }
}
