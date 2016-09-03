// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3


using SilverSim.Threading;
using System.IO;
using System.Net;

namespace SilverSim.ServiceInterfaces
{
    public abstract class ExternalHostNameServiceInterface
    {
        public ExternalHostNameServiceInterface()
        {

        }

        public abstract string ExternalHostName { get; }

        public string ResolvedIP
        {
            get
            {
                IPAddress[] addresses = DnsNameCache.GetHostAddresses(ExternalHostName);
                return addresses[0].ToString();
            }
        }
    }
}
