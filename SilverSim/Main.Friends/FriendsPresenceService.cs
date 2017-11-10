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

using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Presence;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SilverSim.Types;
using SilverSim.Types.Presence;
using SilverSim.ServiceInterfaces.Friends;
using Nini.Config;

namespace SilverSim.Main.Friends
{
    [PluginName("FriendsPresenceStatus")]
    [Description("Friends presence intermediate service")]
    public sealed class FriendsPresenceService : PresenceServiceInterface, IPlugin
    {
        private PresenceServiceInterface m_PresenceService;
        private IFriendsStatusNotifer m_FriendsStatusNotifierService;
        private readonly string m_PresenceServiceName;
        private readonly string m_FriendsStatusNotifierServiceName;

        public FriendsPresenceService(IConfig config)
        {
            m_FriendsStatusNotifierServiceName = config.GetString("FriendsStatusNotifier", "FriendsStatusNotifier");
            m_PresenceServiceName = config.GetString("PresenceService", "PresenceStorage");
        }

        public override List<PresenceInfo> this[UUID userID] => m_PresenceService[userID];

        public override PresenceInfo this[UUID sessionID, UUID userID] => m_PresenceService[sessionID, userID];

        public override List<PresenceInfo> GetPresencesInRegion(UUID regionId) => m_PresenceService.GetPresencesInRegion(regionId);

        public override void Login(PresenceInfo pInfo)
        {
            m_PresenceService.Login(pInfo);
            m_FriendsStatusNotifierService.NotifyAsOnline(pInfo.UserID);
        }

        public override void Logout(UUID sessionID, UUID userID)
        {
            List<PresenceInfo> presences = this[userID];
            if (presences.Count == 1)
            {
                m_FriendsStatusNotifierService.NotifyAsOffline(presences[0].UserID);
            }
            m_PresenceService.Logout(sessionID, userID);
        }

        public override void LogoutRegion(UUID regionID)
        {
            foreach(PresenceInfo pinfo in GetPresencesInRegion(regionID))
            {
                try
                {
                    Logout(pinfo.SessionID, pinfo.UserID.ID);
                }
                catch
                {
                    /* intentionally ignored */
                }
            }

            m_PresenceService.LogoutRegion(regionID);
        }

        public override void Remove(UUID scopeID, UUID accountID) => m_PresenceService.Remove(scopeID, accountID);

        public override void Report(PresenceInfo pInfo) => m_PresenceService.Report(pInfo);

        public void Startup(ConfigurationLoader loader)
        {
            m_PresenceService = loader.GetService<PresenceServiceInterface>(m_PresenceServiceName);
            m_FriendsStatusNotifierService = loader.GetService<IFriendsStatusNotifer>(m_FriendsStatusNotifierServiceName);
        }
    }
}
