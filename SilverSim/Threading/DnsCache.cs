// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Timers;

namespace SilverSim.Threading
{
    public static class DnsNameCache
    {
        static RwLockedDictionary<string, KeyValuePair<IPAddress[], int>> m_DnsCache = new RwLockedDictionary<string, KeyValuePair<IPAddress[], int>>();
        const int MAX_DNS_CACHE_TIME_IN_MILLISECONDS = 60 * 1000;

        static Timer m_Timer;

        static DnsNameCache()
        {
            m_Timer = new Timer(1000);
            m_Timer.Elapsed += CleanUpTimer;
            m_Timer.Start();
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        static void CleanUpTimer(object sender, ElapsedEventArgs e)
        {
            try
            {
                List<string> removeList = new List<string>();
                foreach (KeyValuePair<string, KeyValuePair<IPAddress[], int>> kvp in m_DnsCache)
                {
                    int diffTime = kvp.Value.Value - Environment.TickCount;
                    if (diffTime < 0)
                    {
                        removeList.Add(kvp.Key);
                    }
                }
                foreach (string remove in removeList)
                {
                    m_DnsCache.Remove(remove);
                }
            }
            catch
            {
                /* just ensure that the caller does not get exceptioned */
            }
        }

        public static IPAddress[] GetHostAddresses(string host, bool ipv4only = false)
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

            if (ipv4only)
            {
                IEnumerable<IPAddress> filtered_addrs = from addr in addresses where addr.AddressFamily == AddressFamily.InterNetwork select addr;
                addresses = filtered_addrs.ToArray();
            }
            return addresses;
        }

        public static ICollection<string> GetCachedDnsEntries()
        {
            return m_DnsCache.Keys;
        }

        public static bool RemoveCachedDnsEntry(string hostname)
        {
            return m_DnsCache.Remove(hostname);
        }
    }
}
