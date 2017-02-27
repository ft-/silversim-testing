// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Friends
{
    public interface IFriendsServicePlugin
    {
        FriendsServiceInterface Instantiate(string url);
        string Name { get; }
        bool IsProtocolSupported(string url);
        bool IsProtocolSupported(string url, Dictionary<string, string> cachedheaders);
    }
}
