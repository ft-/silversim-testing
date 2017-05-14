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
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;

namespace SilverSim.Groups.Common
{
    public sealed partial class DefaultPermissionsGroupsService : GroupsServiceInterface, IPlugin
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
            if(!m_InnerService.Invites.DoesSupportListGetters)
            {
                throw new ConfigurationLoader.ConfigurationErrorException("Inner service must support list getters");
            }
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
