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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Timers;

namespace SilverSim.Threading
{
    public static class DnsNameCache
    {
        private static readonly RwLockedDictionary<string, KeyValuePair<IPAddress[], int>> m_DnsCache = new RwLockedDictionary<string, KeyValuePair<IPAddress[], int>>();
        private const int MAX_DNS_CACHE_TIME_IN_MILLISECONDS = 60 * 1000;

        private static readonly Timer m_Timer;

        static DnsNameCache()
        {
            m_Timer = new Timer(1000);
            AppDomain.CurrentDomain.ProcessExit += TimerStop;
            m_Timer.Elapsed += CleanUpTimer;
            m_Timer.Start();
        }

        private static void TimerStop(object sender, EventArgs e)
        {
            m_Timer.Stop();
        }

        private static void CleanUpTimer(object sender, ElapsedEventArgs e)
        {
            try
            {
                var removeList = new List<string>();
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

        public static ICollection<string> GetCachedDnsEntries() => m_DnsCache.Keys;

        public static bool RemoveCachedDnsEntry(string hostname) => m_DnsCache.Remove(hostname);
    }
}
