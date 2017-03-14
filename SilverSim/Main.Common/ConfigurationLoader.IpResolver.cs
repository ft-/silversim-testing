// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

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
