/*

ArribaSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArribaSim.Types;
using ArribaSim.Types.Groups;

namespace ArribaSim.ServiceInterfaces.Groups
{
    public abstract class GroupsServiceInterface
    {
        public interface IGroupsInterface
        {
            void Create(UUI requestingAgent, GroupInfo group, ulong EveryonePowers, ulong ownerPowers);
            void Update(UUI requestingAgent, GroupInfo group);
            void Delete(UUI requestingAgent, GroupInfo group);

            GroupInfo this[UUI requestingAgent, UUID groupID]
            {
                get;
            }
            GroupInfo this[UUI requestingAgent, string groupName]
            {
                get;
            }

            List<GroupInfo> GetGroupsByName(UUI requestingAgent, string groupName, int limit);
        }

        public interface IGroupMembersInterface
        {
            GroupMember this[UUI requestingAgent, UUID groupID, UUI principal]
            {
                get;
            }

            List<GroupMember> this[UUI requestingAgent, UUID groupID]
            {
                get;
            }

            List<GroupMember> this[UUI requestingAgent, UUI principal]
            {
                get;
            }

            void Add(UUI requestingAgent, UUID groupID, UUI principal);
            void Update(UUI requestingAgent, UUID groupID, UUI principal);
            void Delete(UUI requestingAgent, UUID groupID, UUI principal);
        }

        public interface IGroupRolesInterface
        {
            GroupRole this[UUI requestingAgent, UUID groupID, UUID roleID]
            {
                get;
            }
            List<GroupRole> this[UUI requestingAgent, UUID groupID, UUI principal]
            {
                get;
            }

            void Add(UUI requestingAgent, GroupRole role);
            void Update(UUI requestingAgent, GroupRole role);
            void Delete(UUI requestingAgent, UUID groupID, UUID roleID);
        }

        public interface IGroupRolemembersInterface
        {
            GroupRolemember this[UUI requestingAgent, UUID groupID, UUID roleID, UUI principal]
            {
                get;
            }

            List<GroupRolemember> this[UUI requestingAgent, UUID groupID, UUID roleID]
            {
                get;
            }

            void Add(UUI requestingAgent, GroupRolemember rolemember);
            void Delete(UUI requestingAgent, UUID groupID, UUID roleID, UUI principal);
        }

        public interface IGroupSelectInterface
        {
            UUID this[UUI requestingAgent, UUI princialID]
            {
                get;
                set;
            }
        }

        public interface IGroupInvitesInterface
        {
            GroupInvite this[UUI requestingAgent, UUID groupInviteID]
            {
                get;
            }
            
            List<GroupInvite> this[UUI requestingAgent, UUID groupID, UUID roleID, UUI principal]
            {
                get;
            }

            List<GroupInvite> this[UUI requestingAgent, UUI principal]
            {
                get;
            }

            List<GroupInvite> GetByGroup(UUI requestingAgent, UUID groupID);

            void Add(UUI requestingAgent, GroupInvite invite);
            void Update(UUI requestingAgent, GroupInvite invite);
            void Delete(UUI requestingAgent, UUID inviteID);

        }

        public interface IGroupNoticesInterface
        {
            List<GroupNotice> GetNotices(UUI requestingAgent, UUID groupID);

            GroupNotice this[UUI requestingAgent, UUID groupNoticeID]
            {
                get;
            }

            void Add(UUI requestingAgent, GroupNotice notice);

            void Delete(UUI requestingAgent, UUID groupNoticeID);
        }

        #region Constructor
        public GroupsServiceInterface()
        {

        }
        #endregion

        public abstract IGroupsInterface Groups
        {
            get;
        }

        public abstract IGroupRolesInterface Roles
        {
            get;
        }

        public abstract IGroupMembersInterface Members
        {
            get;
        }

        public abstract IGroupRolemembersInterface Rolemembers
        {
            get;
        }

        public abstract IGroupSelectInterface ActiveGroup
        {
            get;
        }

        public abstract IGroupInvitesInterface Invites
        {
            get;
        }

        public abstract IGroupNoticesInterface Notices
        {
            get;
        }
    }
}
