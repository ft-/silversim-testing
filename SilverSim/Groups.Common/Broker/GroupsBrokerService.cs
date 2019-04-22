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
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Agent;
using SilverSim.ServiceInterfaces;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Threading;
using SilverSim.Types;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;

namespace SilverSim.Groups.Common.Broker
{
    [Description("Groups brokering service")]
    [PluginName("GroupsBroker")]
    public sealed partial class GroupsBrokerService : GroupsServiceInterface, IPlugin, IPluginShutdown
    {
        private readonly Timer m_Timer = new Timer(60000);

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
        private IGroupsChatServiceInterface m_GroupIMRouter;
        private readonly string m_GroupIMRouterName;
        private string m_GroupsHomeURI;
        private SceneList m_Scenes;

        private List<IGroupsServicePlugin> m_GroupsServicePlugins;
        private List<IUserAgentServicePlugin> m_UserAgentServicePlugins;
        private List<IGroupsChatServicePlugin> m_GroupsChatServicePlugins;

        private readonly RwLockedDictionary<UUID, GroupsBrokerEntry> m_NameCache = new RwLockedDictionary<UUID, GroupsBrokerEntry>();

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
            GroupsBrokerEntry groupsService;
            if (!TryGetGroupsService(group, out groupsService))
            {
                throw new GroupsServiceNotFoundException();
            }
            return groupsService;
        }

        private IGroupsChatServiceInterface GetGroupsChatService(UGI group)
        {
            GroupsBrokerEntry groupsService;
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
            groupsService = null;
            IAgent agent;
            UUID regionID;
            if(m_Scenes.TryFindRootAgent(principal.ID, out agent, out regionID))
            {
                groupsService = agent.GroupsService;
            }
            return groupsService != null;
        }

        private bool TryGetGroupsService(UUID groupID, out GroupsServiceInterface groupsService)
        {
            UGI group;
            GroupsBrokerEntry entry;
            if (m_NameCache.TryGetValue(groupID, out entry) && m_ClockSource.TicksElapsed(m_ClockSource.TickCount, entry.ExpiryTickCount) < m_ClockSource.SecsToTicks(120))
            {
                groupsService = entry;
                return true;
            }
            if (!m_GroupsNameService.TryGetValue(groupID, out group))
            {
                groupsService = default(GroupsServiceInterface);
                return false;
            }
            bool result = TryGetGroupsService(group, out entry);
            groupsService = entry;
            return result;
        }

        private bool TryGetGroupsService(UGI ugi, out GroupsBrokerEntry groupsService)
        {
            groupsService = default(GroupsBrokerEntry);
            Dictionary<string, string> cachedheaders;
            GroupsBrokerEntry entry;
            if(ugi.HomeURI == null)
            {
                return false;
            }

            if(ugi.AuthorizationToken == null)
            {
                /* if unset, try fetching the authorization token */
                UGI ugi2;
                if(m_GroupsNameService.TryGetValue(ugi.ID, out ugi2))
                {
                    ugi = ugi2;
                    if (ugi.HomeURI == null)
                    {
                        return false;
                    }
                }
            }

            string groupsServerURI = ugi.HomeURI.ToString();

            if (m_GroupsService != null && m_GroupsHomeURI == groupsServerURI)
            {
                entry = new GroupsBrokerEntry(m_GroupsService, m_GroupIMRouter, m_ClockSource.TickCount);
                m_NameCache[ugi.ID] = entry;
                groupsService = entry;
                return true;
            }

            cachedheaders = ServicePluginHelo.HeloRequest(groupsServerURI);

            foreach (IGroupsServicePlugin plugin in m_GroupsServicePlugins)
            {
                if(plugin.IsProtocolSupported(groupsServerURI, cachedheaders))
                {
                    GroupsServiceInterface service = plugin.Instantiate(ugi);

                    IGroupsChatServiceInterface chatService = service as IGroupsChatServiceInterface;
                    if (chatService == null)
                    {
                        /* do not search for it if the groups connector has it integrated */
                        foreach (IGroupsChatServicePlugin chatplugin in m_GroupsChatServicePlugins)
                        {
                            if (chatplugin.IsProtocolSupported(groupsServerURI, cachedheaders))
                            {
                                chatService = chatplugin.Instantiate(ugi);
                                break;
                            }
                        }
                    }

                    entry = new GroupsBrokerEntry(service, chatService, m_ClockSource.TickCount);
                    m_NameCache[ugi.ID] = entry;
                    groupsService = entry;
                    return true;
                }
            }
            return false;
        }

        public override bool TryRequestAuthorization(UGUI requestingAgent, UGI ugi)
        {
            GroupsServiceInterface groupsService;
            if(TryGetGroupsService(requestingAgent, out groupsService) && groupsService.TryRequestAuthorization(requestingAgent, ugi))
            {
                m_GroupsNameService.Store(ugi);
                return true;
            }
            return false;
        }

        public GroupsBrokerService(IConfig config)
        {
            m_GroupsServiceName = config.GetString("GroupsService", string.Empty);
            m_ClockSource = TimeProvider.StopWatch;
            m_GroupsNameServiceName = config.GetString("GroupsNameService", "GroupsNameStorage");
            m_GroupIMRouterName = config.GetString("GroupIMRouter", string.Empty);
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_Scenes = loader.Scenes;
            m_GroupsServicePlugins = loader.GetServicesByValue<IGroupsServicePlugin>();
            m_UserAgentServicePlugins = loader.GetServicesByValue<IUserAgentServicePlugin>();
            m_GroupsChatServicePlugins = loader.GetServicesByValue<IGroupsChatServicePlugin>();
            loader.GetService(m_GroupsNameServiceName, out m_GroupsNameService);
            if(!string.IsNullOrEmpty(m_GroupsServiceName))
            {
                loader.GetService(m_GroupsServiceName, out m_GroupsService);
            }
            if(!string.IsNullOrEmpty(m_GroupIMRouterName))
            {
                loader.GetService(m_GroupIMRouterName, out m_GroupIMRouter);
            }
            m_GroupsHomeURI = loader.HomeURI;
            m_Timer.Elapsed += ExpireHandler;
            m_Timer.Start();
        }

        public void Shutdown()
        {
            m_Timer.Stop();
            m_Timer.Elapsed -= ExpireHandler;
        }

        public ShutdownOrder ShutdownOrder => ShutdownOrder.Any;

        private void ExpireHandler(object o, ElapsedEventArgs args)
        {
            try
            {
                foreach (UUID id in m_NameCache.Keys)
                {
                    GroupsBrokerEntry entry;
                    if (m_NameCache.TryGetValue(id, out entry))
                    {
                        if (m_ClockSource.TicksElapsed(m_ClockSource.TickCount, entry.ExpiryTickCount) > m_ClockSource.SecsToTicks(120))
                        {
                            m_NameCache.Remove(id);
                        }
                        else
                        {
                            entry.ExpireHandler();
                        }
                    }
                }
            }
            catch
            {
                /* do not pass these to timer handler */
            }
        }
    }
}
