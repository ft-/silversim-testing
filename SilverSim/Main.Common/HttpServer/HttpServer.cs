// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Http;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.PortControl;
using SilverSim.ServiceInterfaces.ServerParam;
using SilverSim.Threading;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace SilverSim.Main.Common.HttpServer
{
    [Description("HTTP Server")]
    [ServerParam("HTTP.MaxActiveConnectionsPerPort", Type = ServerParamType.GlobalOnly, ParameterType = typeof(uint))]
    public class BaseHttpServer : IPlugin, IPluginShutdown, IServerParamListener
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
        readonly SslProtocols m_SslProtocols = SslProtocols.Tls12;
        readonly string m_CertificateFileName;
        readonly Type m_SslStreamPreload;
        readonly Socket m_ListenerSocket;
        readonly Thread m_ListenerThread;
        readonly List<IPortControlServiceInterface> m_PortControlServices = new List<IPortControlServiceInterface>();

        public BaseHttpServer(IConfig httpConfig, ConfigurationLoader loader, bool useSsl = false)
        {
            m_PortControlServices = loader.GetServicesByValue<IPortControlServiceInterface>();
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

            m_ListenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

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

            foreach(IPortControlServiceInterface iface in m_PortControlServices)
            {
                iface.EnablePort(new AddressFamily[] { AddressFamily.InterNetwork }, ProtocolType.Tcp, (int)Port);
            }

            m_ListenerThread = ThreadManager.CreateThread(AcceptThread);
            m_ListenerThread.Start();

            if (Scheme == Uri.UriSchemeHttps)
            {
                if(httpConfig.GetBoolean("EnableTls1.0", false))
                {
                    m_SslProtocols |= SslProtocols.Tls;
                    loader.KnownConfigurationIssues.Add("Please set EnableTls1.0 in [HTTPS] to false. TLS V1.0 is susceptible to POODLE attack. Only enable if explicitly needed for certain old applications.");
                }
                if (httpConfig.GetBoolean("EnableTls1.1", false))
                {
                    m_SslProtocols |= SslProtocols.Tls11;
                    loader.KnownConfigurationIssues.Add("Please set EnableTls1.0 in [HTTPS] to false. TLS V1.1 is susceptible to POODLE attack. Only enable if explicitly needed for certain old applications.");
                }
                m_Log.InfoFormat("Adding HTTPS Server at port {0}", Port);
            }
            else
            {
                m_Log.InfoFormat("Adding HTTP Server at port {0}", Port);
            }
        }

        ~BaseHttpServer()
        {
            m_AsyncListenerEvent.Dispose();
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_ExternalHostNameService = loader.ExternalHostNameService;
            if(!File.Exists(m_CertificateFileName) && Scheme == Uri.UriSchemeHttps)
            {
                m_Log.Warn("Generating self-signed cert");
                try
                {
                    SslSelfSignCertUtil.GenerateSelfSignedServiceCertificate(m_CertificateFileName, m_ExternalHostNameService.ExternalHostName);
                }
                catch(Exception e)
                {
                    m_Log.Error("Creating self-signed cert failed", e);
                    throw new ConfigurationLoader.ConfigurationErrorException("Creating self-signed cert failed");
                }
            }
            if (Scheme == Uri.UriSchemeHttps)
            {
                try
                {
                    m_ServerCertificate = new X509Certificate2(m_CertificateFileName);
                }
                catch(Exception e)
                {
                    m_Log.Error("Loading certificate failed", e);
                    throw new ConfigurationLoader.ConfigurationErrorException("Loading certificate failed");
                }
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
            m_ListenerSocket.Close();
            while (m_ActiveThreadCount > 0)
            {
                Thread.Sleep(1);
            }
            StartsWithUriHandlers.Clear();
            UriHandlers.Clear();
            RootUriContentTypeHandlers.Clear();
            foreach (IPortControlServiceInterface iface in m_PortControlServices)
            {
                try
                {
                    iface.DisablePort(new AddressFamily[] { AddressFamily.InterNetwork }, ProtocolType.Tcp, (int)Port);
                }
                catch (Exception e)
                {
                    m_Log.DebugFormat("Failed to disable port {0}: {1}: {2}", Port, e.GetType().FullName, e.Message);
                }
            }
        }

        int m_ActiveThreadCount;
        int m_MaxActiveHttpConnections = 500;

        [ServerParam("HTTP.MaxActiveConnectionsPerPort")]
        public void MaxActiveHttpConnectionsPerPortUpdate(UUID regionId, string value)
        {
            int val;
            if(UUID.Zero == regionId && int.TryParse(value, out val) && val > 0)
            {
                m_MaxActiveHttpConnections = val;
            }
        }

        readonly AutoResetEvent m_AsyncListenerEvent = new AutoResetEvent(false);

        private void AcceptThread()
        {
            Thread.CurrentThread.Name = Scheme.ToUpper() + " Server at " + Port.ToString();
            try
            {
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
                            if (m_StoppingListeners)
                            {

                                break;
                            }
                        }
                    }

                    if(args.AcceptSocket == null)
                    {
                        /* we cannot do anything with it when the AcceptSocket is not set */
                    }
                    else if (args.AcceptSocket.Connected && !m_StoppingListeners)
                    {
                        Interlocked.Increment(ref m_ActiveThreadCount);
                        Thread t = m_ServerCertificate != null ?
                            ThreadManager.CreateThread(AcceptedConnectionSsl) :
                            ThreadManager.CreateThread(AcceptedConnectionPlain);
                        t.Start(args.AcceptSocket);
                        args.AcceptSocket = null;
                        while (m_ActiveThreadCount > 200 && !m_StoppingListeners)
                        {
                            Thread.Sleep(1);
                        }
                    }
                    else
                    {
                        /* no connection get rid of the socket */
                        args.AcceptSocket.Dispose();
                    }
                }
            }
            catch (NullReferenceException)
            {
                /* intentionally ignored */
            }
            finally
            {
                Interlocked.Decrement(ref m_ActiveThreadCount);
            }
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
        private void AcceptedConnectionPlain(object socko)
        {
            try
            {
                Socket socket = (Socket)socko;
                try
                {
                    IPEndPoint ep = (IPEndPoint)socket.RemoteEndPoint;
                    string remoteAddr = AddressToString(ep.Address);
                    Thread.CurrentThread.Name = Scheme.ToUpper() + " Server for " + remoteAddr + " at " + Port.ToString();

                    Stream httpstream;
                    httpstream = new HttpStream(socket);

                    AcceptedConnection_Internal(httpstream, remoteAddr, false);
                }
                catch (HttpResponse.DisconnectFromThreadException)
                {
                    socket = null;
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
            catch (Exception e)
            {
                m_Log.DebugFormat("Exception: {0}: {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace);
            }
        }

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        private void AcceptedConnectionSsl(object socko)
        {
            try
            {
                Socket socket = (Socket)socko;
                try
                {
                    IPEndPoint ep = (IPEndPoint)socket.RemoteEndPoint;
                    string remoteAddr = AddressToString(ep.Address);
                    Thread.CurrentThread.Name = Scheme.ToUpper() + " Server for " + remoteAddr + " at " + Port.ToString();

                    /* Start SSL handshake */
                    SslStream sslstream = new SslStream(new NetworkStream(socket));
                    try
                    {
                        sslstream.AuthenticateAsServer(m_ServerCertificate, false, m_SslProtocols, false);
                    }
                    catch
                    {
                        /* not correctly authenticated */
                        if (m_Log.IsDebugEnabled)
                        {
                            m_Log.DebugFormat("SSL AuthenticateAsServer failed for client {0}", remoteAddr.ToString());
                        }
                        return;
                    }

                    AcceptedConnection_Internal(sslstream, remoteAddr, true);
                }
                catch (HttpResponse.DisconnectFromThreadException)
                {
                    socket = null;
                }
                catch (HttpResponse.ConnectionCloseException)
                {
                    /* simply a closed connection */
                }
                catch (IOException)
                {
                    /* commonly a broken pipe */
                }
                catch (SocketException)
                {
                    /* commonly a broken pipe */
                }
                catch (ObjectDisposedException)
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
            catch (Exception e)
            {
                m_Log.DebugFormat("Exception: {0}: {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace);
            }
        }

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
        private void AcceptedConnection_Internal(Stream httpstream, string remoteAddr, bool isSsl)
        {
            try
            {
                while (true)
                {
                    HttpRequest req;
                    try
                    {
                        req = new Http1Request(httpstream, remoteAddr, m_IsBehindProxy, isSsl);
                    }
                    catch (HttpResponse.ConnectionCloseException)
                    {
                        return;
                    }
                    catch (HttpStream.TimeoutException)
                    {
                        return;
                    }
                    catch (TimeoutException)
                    {
                        return;
                    }
                    catch (IOException)
                    {
                        return;
                    }
                    catch (InvalidDataException)
                    {
                        return;
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (SocketException)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        m_Log.WarnFormat("Unexpected exception: {0}: {1}\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                        return;
                    }

                    /* Recognition for plaintext HTTP/2.0 connection */
                    if(req.Method == "PRI")
                    {
                        if (req.MajorVersion != 2 || req.MinorVersion != 0 || req.IsSsl)
                        {
                            req.SetConnectionClose();
                            req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                        }
                        else
                        {
                            req.SetConnectionClose();
                            req.ErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed");
                        }
                        return;
                    }

                    if ((req.Method == "POST" || req.Method == "PUT") && req.Body == null)
                    {
                        req.SetConnectionClose();
                        req.ErrorResponse(HttpStatusCode.LengthRequired, "Length Required");
                        return;
                    }

                    Action<HttpRequest> del;
                    if (req.RawUrl == "/" && RootUriContentTypeHandlers.TryGetValue(req.ContentType, out del))
                    {
                        try
                        {
                            del(req);
                        }
                        catch (HttpServerException e)
                        {
                            e.Serialize(req);
                        }
                        catch (HttpResponse.ConnectionCloseException)
                        {
                            return;
                        }
                        catch (HttpResponse.DisconnectFromThreadException)
                        {
                            throw;
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
                        catch (HttpServerException e)
                        {
                            e.Serialize(req);
                        }
                        catch (HttpResponse.ConnectionCloseException)
                        {
                            return;
                        }
                        catch (HttpResponse.DisconnectFromThreadException)
                        {
                            throw;
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
                                catch (HttpServerException e)
                                {
                                    e.Serialize(req);
                                }
                                catch (HttpResponse.ConnectionCloseException)
                                {
                                    return;
                                }
                                catch (HttpResponse.DisconnectFromThreadException)
                                {
                                    throw;
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
            catch (HttpResponse.DisconnectFromThreadException)
            {
                /* we simply disconnected that HttpRequest from HttpServer */
            }
            catch (HttpResponse.ConnectionCloseException)
            {
                /* simply a closed connection */
            }
            catch (IOException)
            {
                /* commonly a broken pipe */
            }
            catch (SocketException)
            {
                /* commonly a broken pipe */
            }
            catch (ObjectDisposedException)
            {
                /* commonly a broken pipe */
            }
            catch (Exception e)
            {
                m_Log.DebugFormat("Exception: {0}: {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace);
            }
        }

        private const string ErrorString = "<HTML><BODY>No knomes here for you to find.</BODY></HTML>";
    }
}
