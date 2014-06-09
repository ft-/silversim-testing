/*
 * ArribaSim is distributed under the terms of the
 * GNU General Public License v2 
 * with the following clarification and special exception.
 * 
 * Linking this code statically or dynamically with other modules is
 * making a combined work based on this code. Thus, the terms and
 * conditions of the GNU General Public License cover the whole
 * combination.
 * 
 * As a special exception, the copyright holders of this code give you
 * permission to link this code with independent modules to produce an
 * executable, regardless of the license terms of these independent
 * modules, and to copy and distribute the resulting executable under
 * terms of your choice, provided that you also meet, for each linked
 * independent module, the terms and conditions of the license of that
 * module. An independent module is a module which is not derived from
 * or based on this code. If you modify this code, you may extend
 * this exception to your version of the code, but you are not
 * obligated to do so. If you do not wish to do so, delete this
 * exception statement from your version.
 * 
 * License text is derived from GNU classpath text
 */

using ArribaSim.Main.Common;
using ArribaSim.ServiceInterfaces.Groups;

namespace ArribaSim.Database.Null.Groups
{
    class NullGroupsService : GroupsServiceInterface, IPlugin
    {
        NullGroupsInterface m_GroupsInterface = new NullGroupsInterface();

        NullGroupMembersInterface m_GroupMembersInterface = new NullGroupMembersInterface();

        NullGroupRolesInterface m_GroupRolesInterface = new NullGroupRolesInterface();

        NullGroupRolemembersInterface m_GroupRolemembersInterface = new NullGroupRolemembersInterface();

        NullGroupSelectInterface m_GroupSelectInterface = new NullGroupSelectInterface();

        GroupInvitesInterface m_GroupInvitesInterface = new GroupInvitesInterface();

        GroupNoticesInterface m_GroupNoticesInterface = new GroupNoticesInterface();

        #region Constructor
        public NullGroupsService()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }
        #endregion

        public override IGroupsInterface Groups
        {
            get
            {
                return m_GroupsInterface;
            }
        }

        public override IGroupRolesInterface Roles
        {
            get
            {
                return m_GroupRolesInterface;
            }
        }

        public override IGroupMembersInterface Members
        {
            get
            {
                return m_GroupMembersInterface;
            }
        }

        public override IGroupRolemembersInterface Rolemembers
        {
            get
            {
                return m_GroupRolemembersInterface;
            }
        }

        public override IGroupSelectInterface ActiveGroup
        {
            get
            {
                return m_GroupSelectInterface;
            }
        }

        public override IGroupInvitesInterface Invites
        {
            get
            {
                return m_GroupInvitesInterface;
            }
        }

        public override IGroupNoticesInterface Notices
        {
            get
            {
                return m_GroupNoticesInterface;
            }
        }

    }
}
