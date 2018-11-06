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
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.UserSession;
using SilverSim.Types;
using SilverSim.Types.Grid;
using SilverSim.Types.UserSession;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Main.Friends
{
    [PluginName("GridFriendsSimNotifier")]
    [Description("Local friends on regions notifier")]
    public sealed class GridFriendsStatusNotifyService : IFriendsSimStatusNotifyService, IPlugin
    {
        private GridServiceInterface m_GridService;
        private readonly string m_GridServiceName;

        private UserSessionServiceInterface m_UserSessionService;
        private readonly string m_UserSessionServiceName;

        private IFriendsSimStatusConnector m_FriendsSimStatusConnector;
        private readonly string m_FriendsSimStatusConnectorName;

        public GridFriendsStatusNotifyService(IConfig config)
        {
            m_GridServiceName = config.GetString("GridService", "GridService");
            m_UserSessionServiceName = config.GetString("UserSessionService", "UserSessionService");
            m_FriendsSimStatusConnectorName = config.GetString("FriendsSimStatusConnector", "FriendsSimStatusConnector");
        }

        public void Startup(ConfigurationLoader loader)
        {
            loader.GetService(m_UserSessionServiceName, out m_UserSessionService);
            loader.GetService(m_GridServiceName, out m_GridService);
            loader.GetService(m_FriendsSimStatusConnectorName, out m_FriendsSimStatusConnector);
        }

        public void NotifyStatus(UGUI notifier, List<UGUI> list, bool isOnline)
        {
            var regionURIs = new Dictionary<UUID, string>();
            var perRegionNotify = new Dictionary<UUID, List<UGUI>>();
            var regionInfoCache = new Dictionary<UUID, RegionInfo>();

            foreach(UGUI target in list)
            {
                foreach(UserSessionInfo info in m_UserSessionService[target])
                {
                    string griduri;
                    UUID regionid;
                    RegionInfo ri;
                    if(info.TryGetValue(KnownUserSessionInfoVariables.LocationGridURI, out griduri) &&
                        info.TryGetValue(KnownUserSessionInfoVariables.LocationRegionID, out regionid) &&
                        (regionInfoCache.TryGetValue(regionid, out ri) || m_GridService.TryGetValue(regionid, out ri)) &&
                        (ri.Flags & RegionFlags.RegionOnline) != 0)
                    {
                        regionInfoCache[regionid] = ri;
                        regionURIs[regionid] = ri.ServerURI;
                        List<UGUI> notifyList;
                        if(!perRegionNotify.TryGetValue(regionid, out notifyList))
                        {
                            notifyList = new List<UGUI>();
                            perRegionNotify.Add(regionid, notifyList);
                        }
                        notifyList.Add(target);
                    }
                }
            }

            foreach(KeyValuePair<UUID, List<UGUI>> kvp in perRegionNotify)
            {
                try
                {
                    m_FriendsSimStatusConnector.NotifyStatus(regionURIs[kvp.Key], kvp.Key, notifier, kvp.Value, isOnline);
                }
                catch
                {
                    /* intentionally left empty */
                }
            }
        }
    }
}
