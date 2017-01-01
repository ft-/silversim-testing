// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;

namespace SilverSim.Groups.Common
{
    public partial class DefaultPermissionsGroupsService : GroupsServiceInterface, IPlugin
    {
        GroupsServiceInterface m_InnerService;
        readonly string m_GroupsServiceName;

        public DefaultPermissionsGroupsService(IConfig ownSection)
        {
            m_GroupsServiceName = ownSection.GetString("GroupsStorage", "GroupsStorage");
        }

        public override IGroupSelectInterface ActiveGroup
        {
            get
            {
                return m_InnerService.ActiveGroup;
            }
        }

        public override IActiveGroupMembershipInterface ActiveMembership
        {
            get
            {
                return m_InnerService.ActiveMembership;
            }
        }

        public override IGroupsInterface Groups
        {
            get
            {
                return this;
            }
        }

        public override IGroupInvitesInterface Invites
        {
            get
            {
                return this;
            }
        }

        public override IGroupMembersInterface Members
        {
            get
            {
                return this;
            }
        }

        public override IGroupMembershipsInterface Memberships
        {
            get
            {
                return m_InnerService.Memberships;
            }
        }

        public override IGroupNoticesInterface Notices
        {
            get
            {
                return this;
            }
        }

        public override IGroupRolemembersInterface Rolemembers
        {
            get
            {
                return this;
            }
        }

        public override IGroupRolesInterface Roles
        {
            get
            {
                return this;
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_InnerService = loader.GetService<GroupsServiceInterface>(m_GroupsServiceName);
        }

        public override GroupPowers GetAgentPowers(UGI group, UUI agent)
        {
            return m_InnerService.GetAgentPowers(group, agent);
        }

        bool IsGroupOwner(UGI group, UUI agent)
        {
            GroupInfo groupInfo;
            try
            {
                if(!Groups.TryGetValue(agent, group, out groupInfo))
                {
                    return false;
                }
                return Rolemembers.ContainsKey(agent, group, groupInfo.OwnerRoleID, agent);
            }
            catch
            {
                return false;
            }
        }
    }

    [PluginName("DefaultPermissions")]
    public class DefaultPermissionsGroupServiceFactory : IPluginFactory
    {
        public DefaultPermissionsGroupServiceFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new DefaultPermissionsGroupsService(ownSection);
        }
    }
}
