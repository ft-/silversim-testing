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

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.UserSession;
using SilverSim.Types;
using SilverSim.Types.Friends;
using SilverSim.Types.UserSession;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Main.Friends
{
    [PluginName("LocalFriendsStatusNotifier")]
    [Description("Local friends status notifier")]
    public sealed class LocalFriendsStatusNotifyService : IPlugin, ILocalFriendsStatusNotifyService
    {
        private static readonly ILog m_Log = LogManager.GetLogger("FRIENDS STATUS NOTIFIER");
        private FriendsServiceInterface m_FriendsService;
        private UserSessionServiceInterface m_UserSessionService;
        private IFriendsSimStatusNotifyService m_FriendsStatusNotifier;

        private readonly string m_FriendsServiceName;
        private readonly string m_UserSessionServiceName;
        private readonly string m_FriendsStatusNotifierName;

        public LocalFriendsStatusNotifyService(IConfig config)
        {
            m_FriendsServiceName = config.GetString("FriendsService", "FriendsService");
            m_UserSessionServiceName = config.GetString("UserSessionService", "UserSessionService");
            m_FriendsStatusNotifierName = config.GetString("FriendsSimStatusNotifier", string.Empty);
        }

        public void Startup(ConfigurationLoader loader)
        {
            loader.GetService(m_FriendsServiceName, out m_FriendsService);
            loader.GetService(m_UserSessionServiceName, out m_UserSessionService);
            if (!string.IsNullOrEmpty(m_FriendsStatusNotifierName))
            {
                loader.GetService(m_FriendsStatusNotifierName, out m_FriendsStatusNotifier);
            }
        }

        public List<UGUI> NotifyStatus(UGUI from, List<KeyValuePair<UGUI, string>> tolist, bool isOnline)
        {
            var onlineFriends = new List<UGUI>();
            var distribution = new Dictionary<UUID, Dictionary<UUID, List<UGUI>>>();
            foreach (KeyValuePair<UGUI, string> to in tolist)
            {
                FriendInfo fi;
                if (!m_FriendsService.TryGetValue(from, to.Key, out fi) || fi.Secret != to.Value)
                {
                    continue;
                }

                List<UserSessionInfo> pi = m_UserSessionService[to.Key];
                if (pi.Count != 0 && (fi.UserGivenFlags & FriendRightFlags.SeeOnline) != 0)
                {
                    onlineFriends.Add(to.Key);
                }

#warning TODO: implement distribution

            }

            return onlineFriends;
        }
    }
}
