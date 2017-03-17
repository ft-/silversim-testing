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

//#define SUPPORT_PIPELINING

using SilverSim.Threading;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Timers;

namespace SilverSim.Http.Client
{
    public static partial class HttpClient
    {
#if SUPPORT_PIPELINING
        public static readonly bool SupportsPipelining = true;
#else
        public static readonly bool SupportsPipelining /*= false */;
#endif

        struct StreamInfo
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

        static readonly RwLockedDictionaryAutoAdd<string, RwLockedList<StreamInfo>> m_StreamList = new RwLockedDictionaryAutoAdd<string, RwLockedList<StreamInfo>>(delegate() { return new RwLockedList<StreamInfo>(); });
        static Timer m_Timer;

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        static void CleanUpTimer(object sender, ElapsedEventArgs e)
        {
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
                    m_StreamList.RemoveIf(key, delegate(RwLockedList<StreamInfo> val) { return val.Count == 0; });
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

        static Socket ConnectToTcp(string host, int port)
        {
            IPAddress[] addresses;
            addresses = DnsNameCache.GetHostAddresses(host);

            if (addresses.Length == 0)
            {
                throw new SocketException((int)SocketError.HostNotFound);
            }
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(addresses, port);
            return socket;
        }
        #endregion

        #region Stream pipeling handling
        static AbstractHttpStream OpenStream(string scheme, string host, int port)
        {
            return OpenStream(scheme, host, port,
                null,
                SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
                false);
        }

        static AbstractHttpStream OpenStream(
            string scheme, string host, int port,
            X509CertificateCollection clientCertificates, 
            SslProtocols enabledSslProtocols, 
            bool checkCertificateRevocation)
        {
#if SUPPORT_PIPELINING
            string key = scheme + "://" + host + ":" + port.ToString();
            RwLockedList<StreamInfo> streaminfo;
            if(m_StreamList.TryGetValue(key, out streaminfo))
            {
                Stream stream = null;
                lock (streaminfo)
                {
                    if(streaminfo.Count > 0)
                    {
                        if ((streaminfo[0].ValidUntil - Environment.TickCount) > 0)
                        {
                            stream = streaminfo[0].Stream;
                        }
                        streaminfo.RemoveAt(0);
                    }
                }
                m_StreamList.RemoveIf(key, delegate(RwLockedList<StreamInfo> info) { return info.Count == 0; });
                
                if(stream != null)
                {
                    return stream;
                }
            }
#endif

            if (scheme == Uri.UriSchemeHttp)
            {
                return new HttpStream(ConnectToTcp(host, port));
            }
            else if (scheme == Uri.UriSchemeHttps)
            {
                return ConnectToSslServer(host, port, clientCertificates, enabledSslProtocols, checkCertificateRevocation);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        static HttpsStream ConnectToSslServer(
            string host,
            int port,
            X509CertificateCollection clientCertificates,
            SslProtocols enabledSslProtocols,
            bool checkCertificateRevocation)
        {
            SslStream sslstream = new SslStream(new NetworkStream(ConnectToTcp(host, port)));
            sslstream.AuthenticateAsClient(host, clientCertificates, enabledSslProtocols, checkCertificateRevocation);
            if (!sslstream.IsEncrypted)
            {
                throw new AuthenticationException("Encryption not available");
            }
            return new HttpsStream(sslstream);
        }

        static void AddStreamForNextRequest(AbstractHttpStream st, string scheme, string host, int port)
        {
#if SUPPORT_PIPELINING
            string key = scheme + "://" + host + ":" + port.ToString();
            m_StreamList[key].Add(new StreamInfo(st, scheme, host, port));
#else
            st.Close();
#endif
        }
        #endregion
    }
}
