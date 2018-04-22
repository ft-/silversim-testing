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
using Nini.Config;
using SilverSim.Http;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.PortControl;
using SilverSim.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
    public class BaseHttpServer : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("HTTP SERVER");

        public RwLockedDictionary<string, Action<HttpRequest>> StartsWithUriHandlers = new RwLockedDictionary<string, Action<HttpRequest>>();
        public RwLockedDictionary<string, Action<HttpRequest>> UriHandlers = new RwLockedDictionary<string, Action<HttpRequest>>();
        public RwLockedDictionary<string, Action<HttpRequest>> RootUriContentTypeHandlers = new RwLockedDictionary<string, Action<HttpRequest>>();

        public uint Port { get; }
        private ExternalHostNameServiceInterface m_ExternalHostNameService;
        public string ExternalHostName => m_ExternalHostNameService.ExternalHostName;

        public string ServerURI => $"{Scheme}://{ExternalHostName}:{Port}/";
        public string Scheme { get; }

        private readonly bool m_IsBehindProxy;
        private bool m_StoppingListeners;

        private X509Certificate2 m_ServerCertificate;
        private readonly SslProtocols m_SslProtocols = SslProtocols.Tls12;
        private readonly string m_CertificateFileName;
        readonly internal Type m_SslStreamPreload;
        private readonly Socket m_ListenerSocket;
        private readonly Thread m_ListenerThread;
        private readonly List<IPortControlServiceInterface> m_PortControlServices = new List<IPortControlServiceInterface>();

        private int m_AcceptedConnectionsCount;
        public int AcceptedConnectionsCount => m_AcceptedConnectionsCount;

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

            var ep = new IPEndPoint(IPAddress.Any, (int)Port);
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

        public void ReloadCertificate()
        {
            if(Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException();
            }

            try
            {
                m_ServerCertificate = new X509Certificate2(m_CertificateFileName);
            }
            catch (Exception e)
            {
                m_Log.Error("Reloading certificate failed", e);
                throw;
            }
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.Any;

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

        private int m_ActiveThreadCount;

        private readonly AutoResetEvent m_AsyncListenerEvent = new AutoResetEvent(false);

        private void AcceptThread()
        {
            Thread.CurrentThread.Name = Scheme.ToUpper() + " Server at " + Port.ToString();
            try
            {
                Interlocked.Increment(ref m_ActiveThreadCount);
                var args = new SocketAsyncEventArgs();
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

        private static string AddressToString(IPAddress ipAddr)
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

        private void AcceptedConnectionPlain(object socko)
        {
            try
            {
                var socket = (Socket)socko;
                try
                {
                    var ep = (IPEndPoint)socket.RemoteEndPoint;
                    foreach(IPortControlServiceInterface service in m_PortControlServices)
                    {
                        EndPoint newEp;
                        if(service.TryMapAddressToPublic(ep, out newEp))
                        {
                            ep = (IPEndPoint)newEp;
                        }
                    }
                    string remoteAddr = AddressToString(ep.Address);
                    Thread.CurrentThread.Name = Scheme.ToUpper() + " Server for " + remoteAddr + " at " + Port.ToString();

                    AcceptedConnection_Internal(new HttpStream(socket), remoteAddr, false, null);
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
                    socket?.Close();
                    Interlocked.Decrement(ref m_ActiveThreadCount);
                }
            }
            catch (Exception e)
            {
                m_Log.DebugFormat("Exception: {0}: {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace);
            }
        }

        private void AcceptedConnectionSsl(object socko)
        {
            try
            {
                var socket = (Socket)socko;
                try
                {
                    var ep = (IPEndPoint)socket.RemoteEndPoint;
                    foreach (IPortControlServiceInterface service in m_PortControlServices)
                    {
                        EndPoint newEp;
                        if (service.TryMapAddressToPublic(ep, out newEp))
                        {
                            ep = (IPEndPoint)newEp;
                        }
                    }
                    string remoteAddr = AddressToString(ep.Address);
                    Thread.CurrentThread.Name = Scheme.ToUpper() + " Server for " + remoteAddr + " at " + Port.ToString();

                    /* Start SSL handshake */
                    var sslstream = new SslStream(new NetworkStream(socket, true));
                    try
                    {
                        sslstream.AuthenticateAsServer(m_ServerCertificate, false, m_SslProtocols, false);
                    }
                    catch
                    {
                        /* not correctly authenticated */
                        if (m_Log.IsDebugEnabled)
                        {
                            m_Log.DebugFormat("SSL AuthenticateAsServer failed for client {0}", remoteAddr);
                        }
                        return;
                    }

                    AcceptedConnection_Internal(sslstream, remoteAddr, true, sslstream.RemoteCertificate);
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
                    socket?.Close();
                    Interlocked.Decrement(ref m_ActiveThreadCount);
                }
            }
            catch (Exception e)
            {
                m_Log.DebugFormat("Exception: {0}: {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace);
            }
        }

        private void AcceptedConnection_Internal(Stream httpstream, string remoteAddr, bool isSsl, X509Certificate remoteCertificate)
        {
            Interlocked.Increment(ref m_AcceptedConnectionsCount);
            try
            {
                while (true)
                {
                    HttpRequest req;
                    try
                    {
                        req = new Http1Request(httpstream, remoteAddr, m_IsBehindProxy, isSsl, remoteCertificate);
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

                    /* Recognition for HTTP/2.0 connection */
                    if(req.Method == "PRI")
                    {
                        if (req.MajorVersion != 2 || req.MinorVersion != 0)
                        {
                            req.SetConnectionClose();
                            req.ErrorResponse(HttpStatusCode.BadRequest, "Bad Request");
                        }
                        else
                        {
                            X509Certificate cert = req.RemoteCertificate;
                            req = null; /* no need for the initial request data */
                            HandleHttp2(new Http2Connection(httpstream, true), remoteAddr, isSsl, cert);
                        }
                        return;
                    }
                    else if(req.IsH2CUpgradable || req.IsH2CUpgradableAfterReadingBody)
                    {
                        Stream upgradeStream = null;
                        if(req.IsH2CUpgradableAfterReadingBody)
                        {
                            upgradeStream = new MemoryStream();
                            req.Body.CopyTo(upgradeStream);
                            upgradeStream.Seek(0, SeekOrigin.Begin);
                        }
                        httpstream.Write(m_H2cUpgrade, 0, m_H2cUpgrade.Length);
                        byte[] settingsdata = FromUriBase64(req["http2-settings"]);
                        var preface_receive = new byte[24];
                        if (24 != httpstream.Read(preface_receive, 0, 24))
                        {
                            httpstream.Write(m_H2cGoAwayProtocolError, 0, m_H2cGoAwayProtocolError.Length);
                            httpstream.Close();
                            upgradeStream?.Dispose();
                            return;
                        }
                        if(!preface_receive.SequenceEqual(m_H2cClientPreface))
                        {
                            httpstream.Write(m_H2cGoAwayProtocolError, 0, m_H2cGoAwayProtocolError.Length);
                            httpstream.Close();
                            upgradeStream?.Dispose();
                            return;
                        }
                        var h2con = new Http2Connection(httpstream, true);
                        Http2Connection.Http2Stream h2stream = h2con.UpgradeStream(settingsdata, !req.IsH2CUpgradableAfterReadingBody && req.Expect100Continue);
                        req = new Http2Request(h2stream, req.CallerIP, m_IsBehindProxy, isSsl, req.RemoteCertificate, req, upgradeStream);
                        ThreadPool.UnsafeQueueUserWorkItem(HandleHttp2WorkItem, req);
                        HandleHttp2(h2con, remoteAddr, isSsl, req.RemoteCertificate);
                        return;
                    }

                    ProcessHttpRequest(req);
                }
            }
            catch (HttpStream.TimeoutException)
            {
                /* ignore */
            }
            catch (Http2Connection.ConnectionClosedException)
            {
                /* HTTP/2 connection closed */
            }
            catch(Http2Connection.ProtocolErrorException
#if DEBUG
                    e
#endif
                )
            {
                /* HTTP/2 protocol errors */
#if DEBUG
                m_Log.Debug("HTTP/2 Protocol Exception: ", e);
#endif
            }
            catch (HttpResponse.DisconnectFromThreadException)
            {
                /* we simply disconnected that HttpRequest from HttpServer */
                throw;
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

        private static readonly byte[] m_H2cGoAwayProtocolError = new byte[] { 0x00, 0x00, 0x08, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

        private static readonly byte[] m_H2cClientPreface = new byte[] { 0x50, 0x52, 0x49, 0x20, 0x2a, 0x20, 0x48, 0x54, 0x54, 0x50, 0x2f, 0x32, 0x2e, 0x30, 0x0d, 0x0a, 0x0d, 0x0a, 0x53, 0x4d, 0x0d, 0x0a, 0x0d, 0x0a };

        private static readonly byte[] m_H2cUpgrade = Encoding.ASCII.GetBytes("HTTP/1.1 101 Switching Protocols\r\n\r\n");

        private byte[] FromUriBase64(string val) =>
            Convert.FromBase64String(val.Replace('_', '+').Replace('_', '/').PadRight(val.Length % 4 == 0 ? val.Length : val.Length + 4 - (val.Length % 4), '='));

        private void HandleHttp2(Http2Connection http2Connection, string remoteAddr, bool isSsl, X509Certificate remoteCertificate)
        {
            http2Connection.Run((ht2stream) =>
            {
                HttpRequest req;
                try
                {
                    req = new Http2Request(ht2stream, remoteAddr, m_IsBehindProxy, isSsl, remoteCertificate);
                }
                catch (Http2Connection.StreamErrorException)
                {
                    /* ignore */
                    return;
                }
                catch (HttpResponse.ConnectionCloseException)
                {
                    /* ignore */
                    return;
                }
                catch(HttpStream.TimeoutException)
                {
                    /* ignore */
                    return;
                }
                ThreadPool.UnsafeQueueUserWorkItem(HandleHttp2WorkItem, req);
            });
        }

        private void HandleHttp2WorkItem(object o)
        {
            var req = (HttpRequest)o;
            try
            {
                ProcessHttpRequest(req);
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

        public void ProcessHttpRequest(HttpRequest req)
        {
            if ((req.Method == "POST" || req.Method == "PUT") && !req.HasRequestBody)
            {
                req.SetConnectionClose();
                req.ErrorResponse(HttpStatusCode.BadRequest, "Bad request");
                throw new HttpResponse.ConnectionCloseException();
            }

            int qpos = req.RawUrl.IndexOf('?');
            string acturl = qpos >= 0 ? req.RawUrl.Substring(0, qpos) : req.RawUrl;

            Action<HttpRequest> del;
            if(req.RawUrl == "*")
            {
                req.EmptyResponse();
            }
            else if (acturl == "/" && RootUriContentTypeHandlers.TryGetValue(req.ContentType, out del))
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
                    throw;
                }
                catch (HttpResponse.DisconnectFromThreadException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    m_Log.WarnFormat("(Content Handler): Unexpected exception at {0} {1}: {2}: {3}\n{4}", req.Method, acturl, e.GetType().Name, e.Message, e.StackTrace);
                }
                req.Close();
            }
            else if (UriHandlers.TryGetValue(acturl, out del))
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
                    m_Log.WarnFormat("(Uri Handler): Unexpected exception at {0} {1}: {2}: {3}\n{4}", req.Method, acturl, e.GetType().Name, e.Message, e.StackTrace);
                }
                req.Close();
            }
            else
            {
                foreach (KeyValuePair<string, Action<HttpRequest>> kvp in StartsWithUriHandlers)
                {
                    if (acturl.StartsWith(kvp.Key))
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
                            throw;
                        }
                        catch (HttpResponse.DisconnectFromThreadException)
                        {
                            throw;
                        }
                        catch (Exception e)
                        {
                            m_Log.WarnFormat("(StartUriHandler): Unexpected exception at {0} {1}: {2}: {3}\n{4}", req.Method, acturl, e.GetType().Name, e.Message, e.StackTrace);
                        }
                        req.Close();
                        return;
                    }
                }

                using (var res = req.BeginResponse(HttpStatusCode.NotFound, "Not found"))
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(ErrorString);
                    using (Stream s = res.GetOutputStream(buffer.LongLength))
                    {
                        s.Write(buffer, 0, buffer.Length);
                    }
                }
            }
        }

        private const string ErrorString = "<HTML><BODY>No knomes here for you to find.</BODY></HTML>";
    }
}
