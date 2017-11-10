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
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Types;
using SilverSim.Types.Presence;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Main.Friends
{
    [PluginName("LocalFriendsNotifier")]
    [Description("Local friends status notification")]
    public sealed class LocalFriendsStatusNotifyService : IFriendsStatusNotifyServiceInterface, IPlugin
    {
        private PresenceServiceInterface m_PresenceStorage;
        private readonly string m_PresenceStorageName;
        private IFriendsSimStatusNotifyService m_FriendsSimStatusNotifyService;
        private readonly string m_FriendsSimStatusNotifyServiceName;

        public LocalFriendsStatusNotifyService(IConfig section)
        {
            m_PresenceStorageName = section.GetString("PresenceStorage", "PresenceStorage");
            m_FriendsSimStatusNotifyServiceName = section.GetString("FriendsSimStatusNotifyService", "FriendsSimStatusNotifyService");
        }

        public void NotifyAsOffline(UUI notifier, List<KeyValuePair<UUI, string>> list) => InnerNotify(notifier, list, false);

        public void NotifyAsOnline(UUI notifier, List<KeyValuePair<UUI, string>> list) => InnerNotify(notifier, list, false);

        private void InnerNotify(UUI notifier, List<KeyValuePair<UUI, string>> list, bool isOnline)
        {
            var notified = new Dictionary<UUID, List<UUI>>();
            Action<UUID, UUI, List<UUI>> notifyFunc;

            if (isOnline)
            {
                notifyFunc = m_FriendsSimStatusNotifyService.NotifyAsOnline;
            }
            else
            {
                notifyFunc = m_FriendsSimStatusNotifyService.NotifyAsOffline;
            }

            foreach(KeyValuePair<UUI, string> kvp in list)
            {
                List<PresenceInfo> presences = m_PresenceStorage[kvp.Key.ID];

                List<UUI> notifiedInRegion;
                foreach (PresenceInfo pinfo in presences)
                {
                    if (!notified.TryGetValue(pinfo.RegionID, out notifiedInRegion))
                    {
                        notifiedInRegion = new List<UUI>();
                        notified.Add(pinfo.RegionID, notifiedInRegion);
                    }
                    notifiedInRegion.Add(pinfo.UserID);
                }
            }

            foreach(KeyValuePair<UUID, List<UUI>> kvp in notified)
            {
                notifyFunc(kvp.Key, notifier, kvp.Value);
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_PresenceStorage = loader.GetService<PresenceServiceInterface>(m_PresenceStorageName);
            m_FriendsSimStatusNotifyService = loader.GetService<IFriendsSimStatusNotifyService>(m_FriendsSimStatusNotifyServiceName);
        }
    }
}
