// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Main.Common.HttpServer
{
    public class HttpAclHandler
    {
        public bool IsAllowedDefault { get; set; }
        public string Description { get; private set; }

        public HttpAclHandler(string description)
        {
            Description = description;
        }

        public bool CheckIfAllowed(HttpRequest req)
        {
            return IsAllowedDefault;
        }
    }
}
