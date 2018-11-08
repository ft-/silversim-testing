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
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.ServiceInterfaces.UserSession;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Friends;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace SilverSim.Main.Friends
{
    [PluginName("FriendsStatusNotifier")]
    [Description("Friends status notifier")]
    public sealed class FriendsStatusNotifier : IPlugin, IPluginShutdown, IUserSessionStatusHandler
    {
        private static readonly ILog m_Log = LogManager.GetLogger("FRIENDS STATUS NOTIFIER");
        private BlockingQueue<KeyValuePair<UGUI, bool>> m_StatusQueue = new BlockingQueue<KeyValuePair<UGUI, bool>>();
        private bool m_ShutdownThreads;
        private int m_ActiveThreads;
        private UserSessionServiceInterface m_UserSessionService;
        private readonly string m_UserSessionServiceName;
        private FriendsServiceInterface m_FriendsService;
        private readonly string m_FriendsServiceName;
        private IFriendsSimStatusNotifyService m_FriendsSimStatusNotifierService;
        private readonly string m_FriendsSimStatusNotifierServiceName;
        private string m_GatekeeperURI;
        private List<IUserAgentServicePlugin> m_UserAgentServicePlugins;

        public ShutdownOrder ShutdownOrder => ShutdownOrder.LogoutRegion;

        public FriendsStatusNotifier(IConfig config)
        {
            m_UserSessionServiceName = config.GetString("UserSessionService", "UserSessionService");
            m_FriendsServiceName = config.GetString("FriendsService", "FriendsService");
            m_FriendsSimStatusNotifierServiceName = config.GetString("FriendsSimStatusNotifier", string.Empty);
        }

        public void Shutdown()
        {
            m_Log.Info("Stopping friends status handler");
            m_ShutdownThreads = true;
            while (m_ActiveThreads != 0)
            {
                Thread.Sleep(1);
            }
            m_Log.Info("Stopped friends status handler");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_GatekeeperURI = loader.GatekeeperURI;
            loader.GetService(m_UserSessionServiceName, out m_UserSessionService);
            loader.GetService(m_FriendsServiceName, out m_FriendsService);
            if (!string.IsNullOrEmpty(m_FriendsSimStatusNotifierServiceName))
            {
                loader.GetService(m_FriendsSimStatusNotifierServiceName, out m_FriendsSimStatusNotifierService);
            }
            m_UserAgentServicePlugins = loader.GetServicesByValue<IUserAgentServicePlugin>();
        }

        public void UserSessionLogin(UUID sessionID, UGUI user)
        {
            m_StatusQueue.Enqueue(new KeyValuePair<UGUI, bool>(user, true));
            if (!m_ShutdownThreads && m_ActiveThreads < 50)
            {
                Interlocked.Increment(ref m_ActiveThreads);
                ThreadManager.CreateThread(Run).Start();
            }
        }

        public void UserSessionLogout(UUID sessionID, UGUI user)
        {
            m_StatusQueue.Enqueue(new KeyValuePair<UGUI, bool>(user, false));
            if (!m_ShutdownThreads && m_ActiveThreads < 50)
            {
                Interlocked.Increment(ref m_ActiveThreads);
                ThreadManager.CreateThread(Run).Start();
            }
        }

        private void Run()
        {
            while (!m_ShutdownThreads)
            {
                KeyValuePair<UGUI, bool> statusMsg;
                try
                {
                    statusMsg = m_StatusQueue.Dequeue(1000);
                }
                catch (TimeoutException)
                {
                    break;
                }

                try
                {
                    HandleStatusMsg(statusMsg);
                }
                catch (Exception e)
                {
                    m_Log.Debug($"Exception when signaling {statusMsg.Key}", e);
                }
            }
            Interlocked.Decrement(ref m_ActiveThreads);
        }

        private void HandleStatusMsg(KeyValuePair<UGUI, bool> statusMsg)
        {
            bool isOnline = statusMsg.Value;
            UGUI user = statusMsg.Key;

            var signalingTo = new Dictionary<string, List<FriendInfo>>();

            if (isOnline)
            {
                foreach (FriendInfo fi in m_FriendsService[user])
                {
                    string homeURI = fi.User.HomeURI?.ToString() ?? m_GatekeeperURI;
                    List<FriendInfo> friendsPerHomeURI;
                    if ((fi.FriendGivenFlags & FriendRightFlags.SeeOnline) != 0)
                    {
                        if (!signalingTo.TryGetValue(homeURI, out friendsPerHomeURI))
                        {
                            friendsPerHomeURI = new List<FriendInfo>();
                            signalingTo.Add(homeURI, friendsPerHomeURI);
                        }
                        friendsPerHomeURI.Add(fi);
                    }
                }
            }
            else if (!m_UserSessionService.ContainsKey(user))
            {
                foreach (FriendInfo fi in m_FriendsService[user])
                {
                    string homeURI = fi.User.HomeURI?.ToString() ?? m_GatekeeperURI;
                    List<FriendInfo> friendsPerHomeURI;
                    if ((fi.FriendGivenFlags & FriendRightFlags.SeeOnline) != 0)
                    {
                        if (!signalingTo.TryGetValue(homeURI, out friendsPerHomeURI))
                        {
                            friendsPerHomeURI = new List<FriendInfo>();
                            signalingTo.Add(homeURI, friendsPerHomeURI);
                        }
                        friendsPerHomeURI.Add(fi);
                    }
                }
            }

            if (signalingTo.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, List<FriendInfo>> kvp in signalingTo)
            {
                Dictionary<string, string> heloheaders;
                try
                {
                    heloheaders = ServicePluginHelo.HeloRequest(kvp.Key);
                }
                catch (Exception e)
                {
                    m_Log.Debug("Failed to retrieve user agent HELO", e);
                    continue;
                }

                if (kvp.Key == m_GatekeeperURI)
                {
                    /* local stuff */
                    if(m_FriendsSimStatusNotifierService != null)
                    {
                        try
                        {
                            m_FriendsSimStatusNotifierService.NotifyStatus(user, new List<UGUI>(from x in kvp.Value select x.Friend), isOnline);
                        }
                        catch(Exception e)
                        {
                            m_Log.Debug($"Failed to send status to {kvp.Key}", e);
                        }
                    }
                }
                else
                {
                    UserAgentServiceInterface userAgentService = null;
                    foreach (IUserAgentServicePlugin userAgentPlugin in m_UserAgentServicePlugins)
                    {
                        if (userAgentPlugin.IsProtocolSupported(kvp.Key))
                        {
                            userAgentService = userAgentPlugin.Instantiate(kvp.Key);
                            break;
                        }
                    }

                    if (userAgentService == null)
                    {
                        continue;
                    }

                    List<KeyValuePair<UGUI, string>> notifiedFriends = new List<KeyValuePair<UGUI, string>>();
                    foreach (FriendInfo fi in kvp.Value)
                    {
                        notifiedFriends.Add(new KeyValuePair<UGUI, string>(fi.Friend, fi.Secret));
                    }

                    try
                    {
                        userAgentService.NotifyStatus(notifiedFriends, user, isOnline);
                    }
                    catch (Exception e)
                    {
                        m_Log.Debug($"Failed to send status to {kvp.Key}", e);
                    }
                }
            }
        }
    }
}
