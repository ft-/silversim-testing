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
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Friends;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace SilverSim.Main.Friends
{
    [PluginName("FriendsNotifier")]
    [Description("Friends status notification service")]
    public class FriendsStatusNotifier : IFriendsStatusNotifer, IPlugin, IPluginShutdown
    {
        private static readonly ILog m_Log = LogManager.GetLogger("FRIENDS STATUS NOTIFIER");
        private List<IFriendsStatusNotifyServicePlugin> m_Plugins;
        private FriendsServiceInterface m_FriendsService;
        private readonly string m_FriendsServiceName;
        private IFriendsStatusNotifyServiceInterface m_LocalFriendsStatusNotifyService;
        private readonly string m_LocalFriendsStatusNotifyServiceName;
        private string m_HomeURI;

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutDatabase;

        private class FriendsStatusRequest
        {
            public readonly UUI Notifier;
            public readonly bool IsOnline;
            public readonly FriendsServiceInterface FriendsService;

            public FriendsStatusRequest(UUI notifier, bool isOnline, FriendsServiceInterface friendsService)
            {
                Notifier = notifier;
                IsOnline = isOnline;
                FriendsService = friendsService;
            }
        }

        public FriendsStatusNotifier(IConfig config)
        {
            m_FriendsServiceName = config.GetString("FriendsService", string.Empty);
            m_LocalFriendsStatusNotifyServiceName = config.GetString("LocalFriendsNotifyService", string.Empty);
        }

        public void NotifyAsOffline(UUI notifier, FriendsServiceInterface friendsService = null) =>
            m_NotificationQueue.Enqueue(new FriendsStatusRequest(notifier, false, friendsService ?? m_FriendsService));

        public void NotifyAsOnline(UUI notifier, FriendsServiceInterface friendsService = null) =>
            m_NotificationQueue.Enqueue(new FriendsStatusRequest(notifier, true, friendsService ?? m_FriendsService));

        private readonly BlockingQueue<FriendsStatusRequest> m_NotificationQueue = new BlockingQueue<FriendsStatusRequest>();
        private bool m_ShutdownThread;

        private void NotifyThread()
        {
            Thread.CurrentThread.Name = "Friend status notifier";
            FriendsStatusRequest notifyReq;

            while(!m_ShutdownThread)
            {
                try
                {
                    notifyReq = m_NotificationQueue.Dequeue(1000);
                }
                catch(TimeoutException)
                {
                    continue;
                }

                try
                {
                    Notify(notifyReq.Notifier, notifyReq.IsOnline, notifyReq.FriendsService);
                }
                catch(Exception e)
                {
                    m_Log.Debug("Exception at friends status", e);
                }
            }
        }

        private void Notify(UUI notifier, bool isOnline, FriendsServiceInterface friendsService)
        {
            var friendsPerHomeUri = new Dictionary<Uri, List<KeyValuePair<UUI, string>>>();

            if(friendsService != null)
            {
#if DEBUG
                m_Log.DebugFormat("Signaling {0} to friends for {1}", isOnline ? "online" : "offline", notifier.FullName);
#endif
                foreach(FriendInfo fi in friendsService[notifier])
                {
                    if((fi.FriendGivenFlags & FriendRightFlags.SeeOnline) != 0)
                    {
                        Uri homeURI = fi.Friend.HomeURI;
                        List <KeyValuePair<UUI, string>> list;
                        if(!friendsPerHomeUri.TryGetValue(homeURI, out list))
                        {
                            list = new List<KeyValuePair<UUI, string>>();
                            friendsPerHomeUri.Add(homeURI, list);
                        }

                        list.Add(new KeyValuePair<UUI, string>(fi.Friend, fi.Secret));
                    }
                }

                foreach(KeyValuePair<Uri, List<KeyValuePair<UUI, string>>> kvp in friendsPerHomeUri)
                {
                    Uri uri = kvp.Key ?? new Uri(m_HomeURI);
                    try
                    {
                        InnerNotify(notifier, uri, kvp.Value, isOnline);
                    }
                    catch(Exception e)
                    {
                        m_Log.Warn($"Could not connect to {uri}", e);
                    }
                }
            }
        }

        private void InnerNotify(UUI notifier, Uri uri, List<KeyValuePair<UUI, string>> list, bool isOnline)
        {
            string url = uri?.ToString() ?? m_HomeURI;
            if (url.Equals(m_HomeURI, StringComparison.InvariantCultureIgnoreCase))
            {
                if (isOnline)
                {
                    m_LocalFriendsStatusNotifyService?.NotifyAsOnline(notifier, list);
                }
                else
                {
                    m_LocalFriendsStatusNotifyService?.NotifyAsOffline(notifier, list);
                }
            }
            else
            {
                Dictionary<string, string> cachedheaders = ServicePluginHelo.HeloRequest(url);
                foreach (IFriendsStatusNotifyServicePlugin plugin in m_Plugins)
                {
                    if (plugin.IsProtocolSupported(url, cachedheaders))
                    {
                        IFriendsStatusNotifyServiceInterface service = plugin.Instantiate(url);
                        if (isOnline)
                        {
                            service.NotifyAsOnline(notifier, list);
                        }
                        else
                        {
                            service.NotifyAsOffline(notifier, list);
                        }
                    }
                }
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Plugins = loader.GetServicesByValue<IFriendsStatusNotifyServicePlugin>();
            m_HomeURI = loader.HomeURI;
            if(!string.IsNullOrEmpty(m_FriendsServiceName))
            {
                m_FriendsService = loader.GetService<FriendsServiceInterface>(m_FriendsServiceName);
            }
            if(!string.IsNullOrEmpty(m_LocalFriendsStatusNotifyServiceName))
            {
                m_LocalFriendsStatusNotifyService = loader.GetService<IFriendsStatusNotifyServiceInterface>(m_LocalFriendsStatusNotifyServiceName);
            }
            ThreadManager.CreateThread(NotifyThread).Start();
        }

        public void Shutdown()
        {
            m_ShutdownThread = true;
        }
    }
}
