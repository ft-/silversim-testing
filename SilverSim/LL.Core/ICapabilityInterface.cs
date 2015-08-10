// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;

namespace SilverSim.LL.Core
{
    public interface ICapabilityInterface
    {
        string CapabilityName { get; }
        void HttpRequestHandler(HttpRequest httpreq);
    }
}
