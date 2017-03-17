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

using Nini.Config;
using SilverSim.ServiceInterfaces;
using System;
using System.IO;
using System.Net.NetworkInformation;

namespace SilverSim.Main.Common
{
    partial class ConfigurationLoader
    {
        sealed class SystemIPv4Service : ExternalHostNameServiceInterface
        {
            string m_IPv4Cached = string.Empty;
            int m_IPv4LastCached;

            public SystemIPv4Service()
            {

            }

            public override string ExternalHostName
            {
                get
                {
                    if (Environment.TickCount - m_IPv4LastCached < 60000 && m_IPv4Cached.Length != 0)
                    {
                        return m_IPv4Cached;
                    }

                    foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                        {
                            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                            {
                                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                {
                                    m_IPv4Cached = ip.Address.ToString();
                                    m_IPv4LastCached = Environment.TickCount;
                                    return m_IPv4Cached;
                                }
                            }
                        }
                    }
                    throw new InvalidDataException("No IPv4 address found");
                }
            }
        }

        sealed class DomainNameResolveService : ExternalHostNameServiceInterface
        {
            readonly string m_HostName;
            public DomainNameResolveService(string hostname)
            {
                m_HostName = hostname;
            }

            public DomainNameResolveService()
            {

            }

            public override string ExternalHostName
            {
                get
                {
                    return m_HostName;
                }
            }
        }

        static readonly SystemIPv4Service m_SystemIPv4 = new SystemIPv4Service();
        DomainNameResolveService m_DomainResolver;

        public ExternalHostNameServiceInterface ExternalHostNameService
        {
            get
            {
                string result = "SYSTEMIP";
                IConfig config = Config.Configs["Network"];
                if (config != null)
                {
                    result = config.GetString("ExternalHostName", "SYSTEMIP");
                }

                if (result == "SYSTEMIP")
                {
                    return m_SystemIPv4;
                }
                else if (result.StartsWith("resolver:"))
                {
                    return GetService<ExternalHostNameServiceInterface>(result.Substring(9));
                }
                else
                {
                    if (null == m_DomainResolver)
                    {
                        m_DomainResolver = new DomainNameResolveService(result);
                    }
                    return m_DomainResolver;
                }
            }
        }
    }
}
