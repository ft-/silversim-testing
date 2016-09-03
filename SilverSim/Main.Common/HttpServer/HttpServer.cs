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
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SilverSim.Main.Common.HttpServer
{
    [Description("HTTP Server")]
    public class BaseHttpServer : IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("HTTP SERVER");

        public RwLockedDictionary<string, Action<HttpRequest>> StartsWithUriHandlers = new RwLockedDictionary<string, Action<HttpRequest>>();
        public RwLockedDictionary<string, Action<HttpRequest>> UriHandlers = new RwLockedDictionary<string, Action<HttpRequest>>();
        public RwLockedDictionary<string, Action<HttpRequest>> RootUriContentTypeHandlers = new RwLockedDictionary<string, Action<HttpRequest>>();

        readonly TcpListener m_Listener;
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

        readonly X509Certificate m_ServerCertificate;


        public BaseHttpServer(IConfig httpConfig)
        {
            Port = (uint)httpConfig.GetInt("HttpListenerPort", 9000);
            m_IsBehindProxy = httpConfig.GetBoolean("HasProxy", false);

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
            m_ExternalHostNameService = loader.ExternalHostNameService;
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
            m_StoppingListeners = true;
            m_Listener.Stop();
            StartsWithUriHandlers.Clear();
            UriHandlers.Clear();
            RootUriContentTypeHandlers.Clear();
        }

        [SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferTryParseRule")]
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
            catch(NullReferenceException) /* de-initialization can result into null references */
            {
                return;
            }
            if (!m_StoppingListeners)
            {
                m_Listener.BeginAcceptSocket(AcceptConnectionCallback, null);
            }
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
                        m_Log.WarnFormat("Unexpected exception: {0}\n{1}", e.GetType().FullName, e.StackTrace);
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
            catch (Exception e)
            {
                m_Log.DebugFormat("Exception: {0}\n{1}", e.GetType().Name, e.StackTrace);
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
