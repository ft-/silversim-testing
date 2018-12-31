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
using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.ComponentModel;

namespace SilverSim.Database.Memory.Groups
{
    [Description("Memory Groups Backend")]
    [PluginName("Groups")]
    public sealed partial class MemoryGroupsService : GroupsServiceInterface, IPlugin
    {
        public override IGroupsInterface Groups => this;
        public override IGroupRolesInterface Roles => this;
        public override IGroupMembersInterface Members => this;
        public override IGroupMembershipsInterface Memberships => this;
        public override IGroupRolemembersInterface Rolemembers => this;
        public override IGroupSelectInterface ActiveGroup => this;
        public override IActiveGroupMembershipInterface ActiveMembership => this;
        public override IGroupInvitesInterface Invites => this;
        public override IGroupNoticesInterface Notices => this;

        public void Startup(ConfigurationLoader loader)
        {
            /* intentionally left empty */
        }

        private class MemoryGroupMember : GroupMember
        {
            public UUID ActiveRoleID;

            public MemoryGroupMember()
            {

            }

            public MemoryGroupMember(GroupMember src)
                : base(src)
            {
            }

            public MemoryGroupMember(MemoryGroupMember src)
                : base(src)
            {
                ActiveRoleID = src.ActiveRoleID;
            }
        }

        private class MemoryGroupRole : GroupRole
        {
            public readonly RwLockedDictionary<UGUI, bool> Rolemembers = new RwLockedDictionary<UGUI, bool>();

            public MemoryGroupRole()
            {
            }

            public MemoryGroupRole(GroupRole src)
                : base(src)
            {
            }
        }

        private class MemoryGroupInfo : GroupInfo
        {
            public readonly RwLockedDictionary<UGUI, MemoryGroupMember> Members = new RwLockedDictionary<UGUI, MemoryGroupMember>();
            public readonly RwLockedDictionary<UUID, MemoryGroupRole> Roles = new RwLockedDictionary<UUID, MemoryGroupRole>();
            public readonly RwLockedDictionary<UUID, GroupNotice> Notices = new RwLockedDictionary<UUID, GroupNotice>();
            public readonly RwLockedDictionary<UUID, GroupInvite> Invites = new RwLockedDictionary<UUID, GroupInvite>();

            public MemoryGroupInfo(GroupInfo src)
                : base(src)
            {
            }
        }

        private readonly RwLockedDictionary<UUID, MemoryGroupInfo> m_Groups = new RwLockedDictionary<UUID, MemoryGroupInfo>();
        private readonly RwLockedDictionary<UUID, UUID> m_GroupNoticeToGroup = new RwLockedDictionary<UUID, UUID>();
        private readonly RwLockedDictionary<UUID, UUID> m_GroupInvitesToGroup = new RwLockedDictionary<UUID, UUID>();
        private readonly RwLockedDictionary<UGUI, UUID> m_ActiveGroups = new RwLockedDictionary<UGUI, UUID>();
    }
}
