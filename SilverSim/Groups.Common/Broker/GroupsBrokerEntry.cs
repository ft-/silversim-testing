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

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Threading;

namespace SilverSim.Groups.Common.Broker
{
    internal sealed partial class GroupsBrokerEntry : GroupsServiceInterface
    { 
        private static TimeProvider m_ClockSource = TimeProvider.StopWatch;

        private GroupsServiceInterface InnerGroupsService { get; }

        private IGroupsChatServiceInterface InnerGroupsChatService { get; }

        public override IGroupsInterface Groups => InnerGroupsService.Groups;

        public override IGroupRolesInterface Roles => this;

        public override IGroupMembersInterface Members => this;

        public override IGroupMembershipsInterface Memberships => InnerGroupsService.Memberships;

        public override IGroupRolemembersInterface Rolemembers => InnerGroupsService.Rolemembers;

        public override IGroupSelectInterface ActiveGroup => InnerGroupsService.ActiveGroup;

        public override IActiveGroupMembershipInterface ActiveMembership => InnerGroupsService.ActiveMembership;

        public override IGroupInvitesInterface Invites => InnerGroupsService.Invites;

        public override IGroupNoticesInterface Notices => InnerGroupsService.Notices;

        public long ExpiryTickCount;

        public GroupsBrokerEntry(GroupsServiceInterface innerGroupsService, IGroupsChatServiceInterface innerGroupsChatService, long expiryTickCount)
        {
            InnerGroupsService = innerGroupsService;
            InnerGroupsChatService = innerGroupsChatService;
            ExpiryTickCount = expiryTickCount;
        }

        internal void ExpireHandler()
        {
            foreach(UGUI_UGUI id in m_PrincipalGroupRoleCache.Keys)
            {
                m_PrincipalGroupRoleCache.RemoveIf(id, (entry) => m_ClockSource.TicksElapsed(m_ClockSource.TickCount, entry.ExpiryTickCount) > m_ClockSource.SecsToTicks(RoleCacheTimeout));
            }
            foreach (UGUI_UGI id in m_GroupMemberCache.Keys)
            {
                m_GroupMemberCache.RemoveIf(id, (entry) => m_ClockSource.TicksElapsed(m_ClockSource.TickCount, entry.ExpiryTickCount) > m_ClockSource.SecsToTicks(RoleCacheTimeout));
            }
            foreach (UGUI_UGUI id in m_PrincipalGroupMemberCache.Keys)
            {
                m_PrincipalGroupMemberCache.RemoveIf(id, (entry) => m_ClockSource.TicksElapsed(m_ClockSource.TickCount, entry.ExpiryTickCount) > m_ClockSource.SecsToTicks(RoleCacheTimeout));
            }
        }
    }
}
