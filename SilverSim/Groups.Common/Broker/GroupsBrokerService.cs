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
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.ServerURIs;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SilverSim.Groups.Common.Broker
{
    [Description("Groups brokering service")]
    [PluginName("GroupsBroker")]
    public sealed partial class GroupsBrokerService : GroupsServiceInterface, IPlugin
    {
        private TimeProvider m_ClockSource;

        public override IGroupsInterface Groups => this;

        public override IGroupRolesInterface Roles => this;

        public override IGroupMembersInterface Members => this;

        public override IGroupMembershipsInterface Memberships => this;

        public override IGroupRolemembersInterface Rolemembers => this;

        public override IGroupSelectInterface ActiveGroup => this;

        public override IActiveGroupMembershipInterface ActiveMembership => this;

        public override IGroupInvitesInterface Invites => this;

        public override IGroupNoticesInterface Notices => this;

        private GroupsNameServiceInterface m_GroupsNameService;
        private readonly string m_GroupsNameServiceName;

        private GroupsServiceInterface m_GroupsService;
        private readonly string m_GroupsServiceName;
        private string m_GroupsHomeURI;

        private List<IGroupsServicePlugin> m_GroupsServicePlugins;
        private List<IUserAgentServicePlugin> m_UserAgentServicePlugins;

        private readonly RwLockedDictionary<string, GroupsBrokerEntry> m_Cache = new RwLockedDictionary<string, GroupsBrokerEntry>();

        private sealed class NameCacheEntry
        {
            public UGI Group;
            public long ExpiryTickCount;
        }
        private readonly RwLockedDictionary<UUID, NameCacheEntry> m_NameCache = new RwLockedDictionary<UUID, NameCacheEntry>();

        private sealed class PrincipalCacheEntry
        {
            public string GroupsServerURI;
            public long ExpiryTickCount;
        }
        private readonly RwLockedDictionary<UGUI, PrincipalCacheEntry> m_PrincipalCache = new RwLockedDictionary<UGUI, PrincipalCacheEntry>();

        [Serializable]
        public class GroupsServiceNotFoundException : KeyNotFoundException
        {
            public GroupsServiceNotFoundException()
            {
            }

            public GroupsServiceNotFoundException(string message) : base(message)
            {
            }

            public GroupsServiceNotFoundException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }

        private GroupsServiceInterface GetGroupsService(UGUI principal)
        {
            GroupsServiceInterface groupsService;
            if(!TryGetGroupsService(principal, out groupsService))
            {
                throw new GroupsServiceNotFoundException();
            }
            return groupsService;
        }

        private GroupsServiceInterface GetGroupsService(UUID groupid)
        {
            GroupsServiceInterface groupsService;
            if (!TryGetGroupsService(groupid, out groupsService))
            {
                throw new GroupsServiceNotFoundException();
            }
            return groupsService;
        }

        private GroupsServiceInterface GetGroupsService(UGI group)
        {
            GroupsServiceInterface groupsService;
            if (!TryGetGroupsService(group, out groupsService))
            {
                throw new GroupsServiceNotFoundException();
            }
            return groupsService;
        }

        private GroupsServiceInterface GetGroupsService(string groupsServerURI)
        {
            GroupsServiceInterface groupsService;
            if (!TryGetGroupsService(groupsServerURI, out groupsService))
            {
                throw new GroupsServiceNotFoundException();
            }
            return groupsService;
        }

        private bool TryGetGroupsService(UGUI principal, out GroupsServiceInterface groupsService)
        {
            groupsService = default(GroupsServiceInterface);
            if(principal.HomeURI == null)
            {
                return false;
            }

            PrincipalCacheEntry entry;
            if(m_PrincipalCache.TryGetValue(principal, out entry) && m_ClockSource.TicksElapsed(m_ClockSource.TickCount, entry.ExpiryTickCount) > m_ClockSource.SecsToTicks(120))
            {
                if(string.IsNullOrEmpty(entry.GroupsServerURI))
                {
                    return false;
                }
                return TryGetGroupsService(entry.GroupsServerURI, out groupsService);
            }

            string homeURI = principal.HomeURI.ToString();
            Dictionary<string, string> cachedheaders = ServicePluginHelo.HeloRequest(homeURI);
            foreach (IUserAgentServicePlugin plugin in m_UserAgentServicePlugins)
            {
                if (plugin.IsProtocolSupported(homeURI, cachedheaders))
                {
                    UserAgentServiceInterface service = plugin.Instantiate(homeURI);
                    ServerURIs serveruris = service.GetServerURLs(principal);
                    entry = new PrincipalCacheEntry
                    {
                        GroupsServerURI = serveruris.GroupsServerURI,
                        ExpiryTickCount = m_ClockSource.TickCount
                    };
                    m_PrincipalCache[principal] = entry;
                    if (string.IsNullOrEmpty(entry.GroupsServerURI))
                    {
                        return false;
                    }
                    return TryGetGroupsService(serveruris.GroupsServerURI, out groupsService);
                }
            }

            return false;
        }

        private bool TryGetGroupsService(UUID groupID, out GroupsServiceInterface groupsService)
        {
            UGI group;
            NameCacheEntry entry;
            if(m_NameCache.TryGetValue(groupID, out entry) && m_ClockSource.TicksElapsed(m_ClockSource.TickCount, entry.ExpiryTickCount) > m_ClockSource.SecsToTicks(120))
            {
                return TryGetGroupsService(entry.Group, out groupsService);
            }
            if (!m_GroupsNameService.TryGetValue(groupID, out group))
            {
                groupsService = default(GroupsServiceInterface);
                return false;
            }
            entry = new NameCacheEntry { Group = group, ExpiryTickCount = m_ClockSource.TickCount };
            m_NameCache[groupID] = entry;
            return TryGetGroupsService(group, out groupsService);
        }

        private bool TryGetGroupsService(UGI group, out GroupsServiceInterface groupsService)
        {
            groupsService = default(GroupsServiceInterface);
            Uri uri = group.HomeURI;
            if (uri == null)
            {
                return false;
            }
            return TryGetGroupsService(uri.ToString(), out groupsService);
        }

        private bool TryGetGroupsService(string groupsServerURI, out GroupsServiceInterface groupsService)
        {
            groupsService = default(GroupsServiceInterface);
            Dictionary<string, string> cachedheaders;
            GroupsBrokerEntry entry;
            if(m_Cache.TryGetValue(groupsServerURI, out entry) && m_ClockSource.TicksElapsed(m_ClockSource.TickCount, entry.ExpiryTickCount) > m_ClockSource.SecsToTicks(120))
            {
                groupsService = entry;
                return true;
            }

            if (m_GroupsService != null && m_GroupsHomeURI == groupsServerURI)
            {
                entry = new GroupsBrokerEntry(m_GroupsService, m_ClockSource.TickCount);
                m_Cache[groupsServerURI] = entry;
                groupsService = entry;
                return true;
            }

            cachedheaders = ServicePluginHelo.HeloRequest(groupsServerURI);
            foreach(IGroupsServicePlugin plugin in m_GroupsServicePlugins)
            {
                if(plugin.IsProtocolSupported(groupsServerURI, cachedheaders))
                {
                    GroupsServiceInterface service = plugin.Instantiate(groupsServerURI);
                    entry = new GroupsBrokerEntry(service, m_ClockSource.TickCount);
                    m_Cache[groupsServerURI] = entry;
                    groupsService = entry;
                    return true;
                }
            }
            return false;
        }

        public GroupsBrokerService(IConfig config)
        {
            m_GroupsServiceName = config.GetString("GroupsService", string.Empty);
            m_ClockSource = TimeProvider.StopWatch;
            m_GroupsNameServiceName = config.GetString("GroupsNameService", "GroupsNameStorage");
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_GroupsServicePlugins = loader.GetServicesByValue<IGroupsServicePlugin>();
            m_UserAgentServicePlugins = loader.GetServicesByValue<IUserAgentServicePlugin>();
            loader.GetService(m_GroupsNameServiceName, out m_GroupsNameService);
            if(!string.IsNullOrEmpty(m_GroupsServiceName))
            {
                loader.GetService(m_GroupsServiceName, out m_GroupsService);
            }
            m_GroupsHomeURI = loader.HomeURI;
        }
    }
}
