// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Net.Sockets;

namespace SilverSim.ServiceInterfaces.PortControl
{
    public interface IPortControlServiceInterface
    {
        void EnablePort(AddressFamily[] family, ProtocolType proto, int port);
        void DisablePort(AddressFamily[] family, ProtocolType proto, int port);
    }
}
