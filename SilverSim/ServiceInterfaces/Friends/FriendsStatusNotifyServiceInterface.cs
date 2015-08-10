// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Friends
{
    public interface IFriendsStatusNotifyServiceInterface
    {
        void NotifyAsOnline(List<KeyValuePair<UUI, string>> kvp);
        void NotifyAsOffline(List<KeyValuePair<UUI, string>> kvp);
    }
}
