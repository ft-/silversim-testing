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

#if OLD
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Main.Common.CmdIO;
using SilverSim.ServiceInterfaces.AvatarName;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Traveling;
using SilverSim.Types;
using SilverSim.Types.TravelingData;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Main.Friends
{
    [PluginName("FriendsTravelingStatus")]
    [Description("Friends status intercepting traveling data handler")]
    public sealed class FriendsTravelingDataService : TravelingDataServiceInterface, IPlugin
    {
        private TravelingDataServiceInterface m_TravelingDataService;
        private IFriendsStatusNotifer m_FriendsStatusNotifierService;
        private AvatarNameServiceInterface m_AvatarNameService;
        private readonly string m_TravelingDataServiceName;
        private readonly string m_FriendsStatusNotifierServiceName;
        private readonly string m_AvatarNameServiceName;

        public FriendsTravelingDataService(IConfig config)
        {
            m_FriendsStatusNotifierServiceName = config.GetString("FriendsStatusNotifier", "FriendsStatusNotifier");
            m_TravelingDataServiceName = config.GetString("TravelingDataService", "TravelingDataStorage");
            m_AvatarNameServiceName = config.GetString("AvatarNameService", "UserAccountNameService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_TravelingDataService = loader.GetService<TravelingDataServiceInterface>(m_TravelingDataServiceName);
            m_FriendsStatusNotifierService = loader.GetService<IFriendsStatusNotifer>(m_FriendsStatusNotifierServiceName);
            m_AvatarNameService = loader.GetService<AvatarNameServiceInterface>(m_AvatarNameServiceName);
#if DEBUG
            loader.CommandRegistry.CheckAddCommandType("debug").Add("send-online-status", SendOnlineCmd);
            loader.CommandRegistry.CheckAddCommandType("debug").Add("send-offline-status", SendOfflineCmd);
#endif
        }

#if DEBUG
        private void SendOnlineCmd(List<string> args, TTY io, UUID limitedToScene)
        {
            UGUI id;
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Command not allowed");
            }
            else if (args.Count < 3 || args[0] == "help" || !UGUI.TryParse(args[2], out id))
            {
                io.Write("debug send-online-status <uui>");
            }
            else
            {
                m_FriendsStatusNotifierService.NotifyAsOnline(id);
                io.Write("Notification send");
            }
        }

        private void SendOfflineCmd(List<string> args, TTY io, UUID limitedToScene)
        {
            UGUI id;
            if (limitedToScene != UUID.Zero)
            {
                io.Write("Command not allowed");
            }
            else if (args.Count < 3 || args[0] == "help" || !UGUI.TryParse(args[2], out id))
            {
                io.Write("debug send-offline-status <uui>");
            }
            else
            {
                m_FriendsStatusNotifierService.NotifyAsOffline(id);
                io.Write("Notification send");
            }
        }
#endif

        public override TravelingDataInfo GetTravelingData(UUID sessionID)
        {
            return m_TravelingDataService.GetTravelingData(sessionID);
        }

        public override TravelingDataInfo GetTravelingDataByAgentUUIDAndIPAddress(UUID agentID, string ipAddress)
        {
            return m_TravelingDataService.GetTravelingDataByAgentUUIDAndIPAddress(agentID, ipAddress);
        }

        public override List<TravelingDataInfo> GetTravelingDatasByAgentUUID(UUID agentID)
        {
            return m_TravelingDataService.GetTravelingDatasByAgentUUID(agentID);
        }

        public override TravelingDataInfo GetTravelingDatabyAgentUUIDAndNotHomeURI(UUID agentID, string homeURI)
        {
            return m_TravelingDataService.GetTravelingDatabyAgentUUIDAndNotHomeURI(agentID, homeURI);
        }

        public override void Store(TravelingDataInfo data)
        {
            m_TravelingDataService.Store(data);
            UGUI uui;
            if(m_AvatarNameService.TryGetValue(data.UserID, out uui))
            {
                m_FriendsStatusNotifierService.NotifyAsOnline(uui);
            }
        }

        public override bool Remove(UUID sessionID, out TravelingDataInfo info)
        {
            if(!m_TravelingDataService.Remove(sessionID, out info))
            {
                return false;
            }
            UGUI uui;
            if (m_AvatarNameService.TryGetValue(info.UserID, out uui))
            {
                m_FriendsStatusNotifierService.NotifyAsOffline(uui);
            }
            return true;
        }

        public override bool RemoveByAgentUUID(UUID agentID, out TravelingDataInfo info)
        {
            if(!m_TravelingDataService.RemoveByAgentUUID(agentID, out info))
            {
                return false;
            }
            UGUI uui;
            if (m_AvatarNameService.TryGetValue(info.UserID, out uui))
            {
                m_FriendsStatusNotifierService.NotifyAsOffline(uui);
            }
            return true;
        }
    }
}
#endif
