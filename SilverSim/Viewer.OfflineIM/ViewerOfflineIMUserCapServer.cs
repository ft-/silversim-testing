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
using SilverSim.Main.Common.HttpServer;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Traveling;
using SilverSim.Types;
using SilverSim.Types.TravelingData;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

namespace SilverSim.Viewer.OfflineIM
{
    [Description("Viewer Offline IM User Server Cap Handler")]
    [PluginName("ViewerOfflineIMUserCapServer")]
    public sealed class ViewerOfflineIMUserCapServer : ViewerOfflineIMServerBase, IPlugin, ILoginUserCapsGetInterface
    {
        private PresenceServiceInterface m_PresenceService;
        private TravelingDataServiceInterface m_TravelingDataService;
        private OfflineIMServiceInterface m_OfflineIMService;
        private UserAccountServiceInterface m_UserAccountService;
        private readonly string m_PresenceServiceName;
        private readonly string m_TravelingDataServiceName;
        private readonly string m_OfflineIMServiceName;
        private readonly string m_UserAccountServiceName;

        private BaseHttpServer m_HttpServer;
        private BaseHttpServer m_HttpsServer;

        /* prefix url is /CAPS/ReadOfflineMsgs/<sessionid>/ */
        public ViewerOfflineIMUserCapServer(IConfig ownSection)
        {
            m_PresenceServiceName = ownSection.GetString("PresenceService", "PresenceService");
            m_TravelingDataServiceName = ownSection.GetString("TravelingDataService", "TravelingDataService");
            m_OfflineIMServiceName = ownSection.GetString("OfflineIMService", "OfflineIMService");
            m_UserAccountServiceName = ownSection.GetString("UserAccountService", "UserAccountService");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_PresenceService = loader.GetService<PresenceServiceInterface>(m_PresenceServiceName);
            m_TravelingDataService = loader.GetService<TravelingDataServiceInterface>(m_TravelingDataServiceName);

            m_OfflineIMService = loader.GetService<OfflineIMServiceInterface>(m_OfflineIMServiceName);
            m_UserAccountService = loader.GetService<UserAccountServiceInterface>(m_UserAccountServiceName);
            m_HttpServer = loader.HttpServer;
            m_HttpServer.StartsWithUriHandlers.Add("/UserCAPS/ReadOfflineMsgs/", CapsHandler);
            if (loader.TryGetHttpsServer(out m_HttpsServer))
            {
                m_HttpsServer.StartsWithUriHandlers.Add("/UserCAPS/ReadOfflineMsgs/", CapsHandler);
            }
        }

        private void CapsHandler(HttpRequest req)
        {
            string[] splitquery = req.RawUrl.Split('?');
            string[] elements = splitquery[0].Substring(1).Split('/');

            if (elements.Length < 3)
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not found");
                return;
            }

            UUID sessionid;
            if (!UUID.TryParse(elements[2], out sessionid))
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not found");
                return;
            }

            bool foundIP = false;
            UUID agent = UUID.Zero;
            try
            {
                TravelingDataInfo trv = m_TravelingDataService.GetTravelingData(sessionid);
                if (trv.ClientIPAddress == req.CallerIP)
                {
                    agent = trv.UserID;
                    foundIP = true;
                }
            }
            catch
            {
                /* entry not found */
            }

            if (!foundIP || !m_UserAccountService.ContainsKey(UUID.Zero, agent))
            {
                req.ErrorResponse(HttpStatusCode.NotFound, "Not found");
                return;
            }

            ProcessReadOfflineMsgs(agent, req, m_OfflineIMService);
        }

        void ILoginUserCapsGetInterface.GetCaps(UUID agentid, UUID sessionid, Dictionary<string, string> userCapList)
        {
            string serverURI = m_HttpsServer != null ? m_HttpsServer.ServerURI : m_HttpServer.ServerURI;
            userCapList.Add("ReadOfflineMsgs", $"{serverURI}UserCAPS/ReadOfflineMsgs/{sessionid}");
        }
    }
}
