using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.UserSession;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Friends;
using SilverSim.Types.UserSession;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
