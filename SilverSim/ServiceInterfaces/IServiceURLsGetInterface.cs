// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces
{
    public interface IServiceURLsGetInterface
    {
        void GetServiceURLs(Dictionary<string, string> dict);
    }
}
