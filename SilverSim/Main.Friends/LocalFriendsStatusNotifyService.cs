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
using SilverSim.ServiceInterfaces.UserSession;
using SilverSim.Types;
using SilverSim.Types.UserSession;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Main.Friends
{
    [PluginName("LocalFriendsNotifier")]
    [Description("Local friends status notification")]
    public sealed class LocalFriendsStatusNotifyService : IFriendsStatusNotifyServiceInterface, IPlugin
    {
        private UserSessionServiceInterface m_UserSessionService;
        private readonly string m_UserSessionServiceName;
        private IFriendsSimStatusNotifyService m_FriendsSimStatusNotifyService;
        private readonly string m_FriendsSimStatusNotifyServiceName;

        public LocalFriendsStatusNotifyService(IConfig section)
        {
            m_UserSessionServiceName = section.GetString("UserSessionService", "UserSessionService");
            m_FriendsSimStatusNotifyServiceName = section.GetString("FriendsSimStatusNotifyService", "FriendsSimStatusNotifyService");
        }

        public void NotifyAsOffline(UGUI notifier, List<KeyValuePair<UGUI, string>> list) => InnerNotify(notifier, list, false);

        public void NotifyAsOnline(UGUI notifier, List<KeyValuePair<UGUI, string>> list) => InnerNotify(notifier, list, false);

        private void InnerNotify(UGUI notifier, List<KeyValuePair<UGUI, string>> list, bool isOnline)
        {
            var notified = new Dictionary<UUID, List<UGUI>>();
            Action<UUID, UGUI, List<UGUI>> notifyFunc;

            if (isOnline)
            {
                notifyFunc = m_FriendsSimStatusNotifyService.NotifyAsOnline;
            }
            else
            {
                notifyFunc = m_FriendsSimStatusNotifyService.NotifyAsOffline;
            }

            foreach(KeyValuePair<UGUI, string> kvp in list)
            {
                List<UserSessionInfo> presences = m_UserSessionService[kvp.Key];

                List<UGUI> notifiedInRegion;
                foreach (UserSessionInfo pinfo in presences)
                {
                    UUID regionID;
                    if (pinfo.TryGetValue(KnownUserSessionInfoVariables.LocationRegionID, out regionID))
                    {
                        if (!notified.TryGetValue(regionID, out notifiedInRegion))
                        {
                            notifiedInRegion = new List<UGUI>();
                            notified.Add(regionID, notifiedInRegion);
                        }
                        notifiedInRegion.Add(pinfo.User);
                    }
                }
            }

            foreach(KeyValuePair<UUID, List<UGUI>> kvp in notified)
            {
                notifyFunc(kvp.Key, notifier, kvp.Value);
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_UserSessionService = loader.GetService<UserSessionServiceInterface>(m_UserSessionServiceName);
            m_FriendsSimStatusNotifyService = loader.GetService<IFriendsSimStatusNotifyService>(m_FriendsSimStatusNotifyServiceName);
        }
    }
}
