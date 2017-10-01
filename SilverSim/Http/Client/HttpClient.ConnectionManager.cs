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

//#define SUPPORT_REUSE

using SilverSim.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Timers;

namespace SilverSim.Http.Client
{
    public static partial class HttpClient
    {
        public enum ConnectionReuseMode
        {
            SingleRequest,
            Close,
            Keepalive,
            UpgradeHttp2,
            Http2PriorKnowledge,
            Http2PriorKnowledgeSingleRequest
        }
#if SUPPORT_REUSE
        public static ConnectionReuseMode ConnectionReuse = ConnectionReuseMode.Keepalive;
#else
        public static ConnectionReuseMode ConnectionReuse /*= ConnectionReuseMode.ConnectionReuseMode */;
#endif

        private struct StreamInfo
        {
            public int ValidUntil;
            public AbstractHttpStream Stream;
            public string Scheme;
            public string Host;
            public int Port;

            public StreamInfo(AbstractHttpStream stream, string scheme, string host, int port)
            {
                Stream = stream;
                Scheme = scheme;
                Host = host;
                Port = port;
                ValidUntil = Environment.TickCount + 5000;
            }
        }

        private struct H2StreamInfo
        {
            public int ValidUntil;
            public Http2Connection Connection;
            public string Scheme;
            public string Host;
            public int Port;

            public H2StreamInfo(Http2Connection connection, string scheme, string host, int port)
            {
                Connection = connection;
                Scheme = scheme;
                Host = host;
                Port = port;
                ValidUntil = Environment.TickCount + 5000;
            }
        }

        private static readonly RwLockedDictionaryAutoAdd<string, RwLockedList<H2StreamInfo>> m_H2StreamList = new RwLockedDictionaryAutoAdd<string, RwLockedList<H2StreamInfo>>(() => new RwLockedList<H2StreamInfo>());
        private static readonly RwLockedDictionaryAutoAdd<string, RwLockedList<StreamInfo>> m_StreamList = new RwLockedDictionaryAutoAdd<string, RwLockedList<StreamInfo>>(() => new RwLockedList<StreamInfo>());
        private static readonly Timer m_Timer;

        private static void CleanUpTimer(object sender, ElapsedEventArgs e)
        {
            try
            {
                foreach(RwLockedList<H2StreamInfo> infolist in m_H2StreamList.Values)
                {
                    var removelist = new List<H2StreamInfo>();
                    foreach(H2StreamInfo si in infolist)
                    {
                        if(si.ValidUntil - Environment.TickCount < 0)
                        {
                            removelist.Add(si);
                        }
                    }
                    foreach(H2StreamInfo si in removelist)
                    {
                        infolist.Remove(si);
                    }
                }

                /* cleanup empty entries */
                foreach (string key in m_StreamList.Keys)
                {
                    m_StreamList.RemoveIf(key, (RwLockedList<StreamInfo> val) => val.Count == 0);
                }
            }
            catch
            {
                /* just ensure that the caller does not get exceptioned */
            }
            try
            {
                foreach (RwLockedList<StreamInfo> infolist in m_StreamList.Values)
                {
                    while ((infolist[0].ValidUntil - Environment.TickCount) < 0)
                    {
                        infolist.RemoveAt(0);
                    }
                }

                /* cleanup empty entries */
                foreach(string key in m_StreamList.Keys)
                {
                    m_StreamList.RemoveIf(key, (RwLockedList<StreamInfo> val) => val.Count == 0);
                }
            }
            catch
            {
                /* just ensure that the caller does not get exceptioned */
            }
        }

        static HttpClient()
        {
            m_Timer = new Timer(1000);
            m_Timer.Elapsed += CleanUpTimer;
            m_Timer.Start();
        }

        #region Connect Handling
        /* yes, we need our own DNS cache. Mono bypasses anything that caches on Linux */

        private static Socket ConnectToTcp(string host, int port)
        {
            IPAddress[] addresses = DnsNameCache.GetHostAddresses(host);

            if (addresses.Length == 0)
            {
                throw new SocketException((int)SocketError.HostNotFound);
            }
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(addresses, port);
            return socket;
        }
        #endregion

        #region Stream pipeling handling
        private static AbstractHttpStream OpenStream(string scheme, string host, int port, ConnectionReuseMode reuseMode) =>
            OpenStream(scheme, host, port,
                null,
                SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
                false,
                reuseMode);

        private static AbstractHttpStream OpenStream(
            string scheme, string host, int port,
            X509CertificateCollection clientCertificates,
            SslProtocols enabledSslProtocols,
            bool checkCertificateRevocation,
            ConnectionReuseMode reuseMode)
        {
            if(reuseMode == ConnectionReuseMode.UpgradeHttp2 && scheme != Uri.UriSchemeHttp)
            {
                throw new ArgumentException(nameof(reuseMode));
            }

            if (reuseMode != ConnectionReuseMode.SingleRequest)
            {
                string key = scheme + "://" + host + ":" + port.ToString();
                RwLockedList<StreamInfo> streaminfo;
                if (m_StreamList.TryGetValue(key, out streaminfo))
                {
                    AbstractHttpStream stream = null;
                    lock (streaminfo)
                    {
                        if (streaminfo.Count > 0)
                        {
                            if ((streaminfo[0].ValidUntil - Environment.TickCount) > 0)
                            {
                                stream = streaminfo[0].Stream;
                            }
                            streaminfo.RemoveAt(0);
                        }
                    }
                    m_StreamList.RemoveIf(key, (RwLockedList<StreamInfo> info) => info.Count == 0);

                    if (stream != null)
                    {
                        if (reuseMode == ConnectionReuseMode.Close)
                        {
                            stream.IsReusable = false;
                        }

                        return stream;
                    }
                }
            }

            if (scheme == Uri.UriSchemeHttp)
            {
                return new HttpStream(ConnectToTcp(host, port)) { IsReusable = reuseMode == ConnectionReuseMode.Keepalive };
            }
            else if (scheme == Uri.UriSchemeHttps)
            {
                return ConnectToSslServer(host, port, clientCertificates, enabledSslProtocols, checkCertificateRevocation, reuseMode == ConnectionReuseMode.Keepalive);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static HttpsStream ConnectToSslServer(
            string host,
            int port,
            X509CertificateCollection clientCertificates,
            SslProtocols enabledSslProtocols,
            bool checkCertificateRevocation,
            bool enableReuseConnection)
        {
            var sslstream = new SslStream(new NetworkStream(ConnectToTcp(host, port)));
            sslstream.AuthenticateAsClient(host, clientCertificates, enabledSslProtocols, checkCertificateRevocation);
            if (!sslstream.IsEncrypted)
            {
                throw new AuthenticationException("Encryption not available");
            }
            return new HttpsStream(sslstream) { IsReusable = enableReuseConnection };
        }

        private static void AddH2Connection(Http2Connection conn, string scheme, string host, int port)
        {
            string key = scheme + "://" + host + ":" + port.ToString();
            var h2info = new H2StreamInfo(conn, scheme, host, port);
            m_H2StreamList[key].Add(h2info);
            ThreadManager.CreateThread((o) =>
            {
                var info = (H2StreamInfo)o;
                System.Threading.Thread.CurrentThread.Name = "HTTP/2 client connection for " + key;
                try
                {
                    info.Connection.Run();
                }
                catch
                {
                    m_H2StreamList[key].Remove(info);
                }
            }).Start(h2info);
        }

        private static void AddStreamForNextRequest(AbstractHttpStream st, string scheme, string host, int port)
        {
            if (st.IsReusable)
            {
                string key = scheme + "://" + host + ":" + port.ToString();
                m_StreamList[key].Add(new StreamInfo(st, scheme, host, port));
            }
            else
            {
                st.Close();
            }
        }
        #endregion

        #region HTTP/2 stream handling
        private static Http2Connection.Http2Stream OpenHttp2Stream(string scheme, string host, int port, ConnectionReuseMode reuseMode) =>
            OpenHttp2Stream(scheme, host, port,
                null,
                SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
                false,
                reuseMode);

        private static Http2Connection.Http2Stream TryReuseStream(string scheme, string host, int port)
        {
            string key = scheme + "://" + host + ":" + port.ToString();
            Http2Connection.Http2Stream h2stream = null;

            RwLockedList<H2StreamInfo> streaminfo;
            if (m_H2StreamList.TryGetValue(key, out streaminfo))
            {
                lock (streaminfo)
                {
                    for (int i = 0; i < streaminfo.Count; ++i)
                    {
                        H2StreamInfo h2info = streaminfo[i];
                        if ((h2info.ValidUntil - Environment.TickCount) > 0 &&
                            h2info.Connection.AvailableStreams > 0)
                        {
                            h2info.ValidUntil = Environment.TickCount + 5000;
                            h2stream = h2info.Connection.OpenClientStream();
                        }
                    }
                }
            }
            return h2stream;
        }

        private static Http2Connection.Http2Stream OpenHttp2Stream(
            string scheme, string host, int port,
            X509CertificateCollection clientCertificates,
            SslProtocols enabledSslProtocols,
            bool checkCertificateRevocation,
            ConnectionReuseMode reuseMode)
        {
            string key = scheme + "://" + host + ":" + port.ToString();
            Http2Connection.Http2Stream h2stream = null;

            if (reuseMode != ConnectionReuseMode.Http2PriorKnowledgeSingleRequest)
            {
                h2stream = TryReuseStream(scheme, host, port);
            }

            Stream s;
            if (scheme == Uri.UriSchemeHttp)
            {
                s = new HttpStream(ConnectToTcp(host, port)) { IsReusable = false };
            }
            else if (scheme == Uri.UriSchemeHttps)
            {
                s = ConnectToSslServer(host, port, clientCertificates, enabledSslProtocols, checkCertificateRevocation, false);
            }
            else
            {
                throw new NotSupportedException();
            }

            var h2con = new Http2Connection(s, false);
            h2stream = h2con.OpenClientStream();
            AddH2Connection(h2con, scheme, host, port);
            return h2stream;
        }
        #endregion
    }
}
