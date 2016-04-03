﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

//#define SUPPORT_PIPELINING

using SilverSim.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Timers;

namespace SilverSim.Http.Client
{
    public static partial class HttpRequestHandler
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

            try
            {
                List<string> removeList = new List<string>();
                foreach(KeyValuePair<string, KeyValuePair<IPAddress[], int>> kvp in m_DnsCache)
                {
                    int diffTime = kvp.Value.Value - Environment.TickCount;
                    if(diffTime < 0)
                    {
                        removeList.Add(kvp.Key);
                    }
                }
                foreach(string remove in removeList)
                {
                    m_DnsCache.Remove(remove);
                }
            }
            catch
            {
                /* just ensure that the caller does not get exceptioned */
            }
        }

        static HttpRequestHandler()
        {
            m_Timer = new Timer(1000);
            m_Timer.Elapsed += CleanUpTimer;
            m_Timer.Start();
        }

        #region Connect Handling
        /* yes, we need our own DNS cache. Mono bypasses anything that caches on Linux */
        static RwLockedDictionary<string, KeyValuePair<IPAddress[], int>> m_DnsCache = new RwLockedDictionary<string, KeyValuePair<IPAddress[], int>>();
        const int MAX_DNS_CACHE_TIME_IN_MILLISECONDS = 60 * 1000;

        static Socket ConnectToTcp(string host, int port)
        {
            KeyValuePair<IPAddress[], int> kvp;
            IPAddress[] addresses;
            if (!m_DnsCache.TryGetValue(host, out kvp) || 0 > (kvp.Value - Environment.TickCount))
            {
                addresses = Dns.GetHostAddresses(host);
                m_DnsCache[host] = new KeyValuePair<IPAddress[], int>(addresses, Environment.TickCount + MAX_DNS_CACHE_TIME_IN_MILLISECONDS);
            }
            else
            {
                addresses = kvp.Key;
            }

            if (addresses.Length == 0)
            {
                throw new SocketException((int)SocketError.HostNotFound);
            }
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(addresses, port);
            return socket;
        }

        public static ICollection<string> GetCachedDnsEntries()
        {
            return m_DnsCache.Keys;
        }

        public static bool RemoveCachedDnsEntry(string hostname)
        {
            return m_DnsCache.Remove(hostname);
        }
        #endregion

        #region Stream pipeling handling
        static AbstractHttpStream OpenStream(string scheme, string host, int port)
        {
            return OpenStream(scheme, host, port,
                null,
                SslProtocols.Default,
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
                SslStream sslstream = new SslStream(new NetworkStream(ConnectToTcp(host, port)));
                sslstream.AuthenticateAsClient(host, clientCertificates, enabledSslProtocols, checkCertificateRevocation);
                if(!sslstream.IsEncrypted)
                {
                    throw new AuthenticationException("Encryption not available");
                }
                return new HttpsStream(sslstream);
            }
            else
            {
                throw new NotSupportedException();
            }
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
