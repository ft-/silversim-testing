// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

namespace SilverSim.ServiceInterfaces.Groups
{
    public abstract class GroupsServiceInterface
    {
        public interface IGroupsInterface
        {
            GroupInfo Create(UUI requestingAgent, GroupInfo group);
            GroupInfo Update(UUI requestingAgent, GroupInfo group);
            void Delete(UUI requestingAgent, GroupInfo group);

            UGI this[UUI requestingAgent, UUID groupID]
            {
                get;
            }

            GroupInfo this[UUI requestingAgent, UGI group]
            {
                get;
            }

            GroupInfo this[UUI requestingAgent, string groupName]
            {
                get;
            }

            List<DirGroupInfo> GetGroupsByName(UUI requestingAgent, string query);
        }

        public interface IGroupMembershipsInterface
        {
            GroupMembership this[UUI requestingAgent, UGI group, UUI principal]
            {
                get;
            }

            List<GroupMembership> this[UUI requestingAgent, UUI principal]
            {
                get;
            }
        }

        public interface IActiveGroupMembershipInterface
        {
            GroupActiveMembership this[UUI requestingAgent, UUI principal]
            {
                get;
            }
        }

        public interface IGroupMembersInterface
        {
            GroupMember this[UUI requestingAgent, UGI group, UUI principal]
            {
                get;
            }

            List<GroupMember> this[UUI requestingAgent, UGI group]
            {
                get;
            }

            List<GroupMember> this[UUI requestingAgent, UUI principal]
            {
                get;
            }

            GroupMember Add(UUI requestingAgent, UGI group, UUI principal, UUID roleID, string accessToken);
            void SetContribution(UUI requestingagent, UGI group, UUI principal, int contribution);
            void Update(UUI requestingagent, UGI group, UUI principal, bool acceptNotices, bool listInProfile);
            void Delete(UUI requestingAgent, UGI group, UUI principal);
        }

        public interface IGroupRolesInterface
        {
            GroupRole this[UUI requestingAgent, UGI group, UUID roleID]
            {
                get;
            }
            List<GroupRole> this[UUI requestingAgent, UGI group]
            {
                get;
            }
            List<GroupRole> this[UUI requestingAgent, UGI group, UUI principal]
            {
                get;
            }

            void Add(UUI requestingAgent, GroupRole role);
            void Update(UUI requestingAgent, GroupRole role);
            void Delete(UUI requestingAgent, UGI group, UUID roleID);
        }

        public interface IGroupRolemembersInterface
        {
            GroupRolemember this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
            {
                get;
            }

            List<GroupRolemember> this[UUI requestingAgent, UGI group, UUID roleID]
            {
                get;
            }

            List<GroupRolemembership> this[UUI requestingAgent, UUI principal]
            {
                get;
            }

            List<GroupRolemember> this[UUI requestingAgent, UGI group]
            {
                get;
            }

            void Add(UUI requestingAgent, GroupRolemember rolemember);
            void Delete(UUI requestingAgent, UGI group, UUID roleID, UUI principal);
        }

        public interface IGroupSelectInterface
        {
            UGI this[UUI requestingAgent, UUI princialID]
            {
                get;
                set;
            }

            /* get/set active role id */
            UUID this[UUI requestingAgent, UGI group, UUI principal]
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

            List<GroupInvite> this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
            {
                get;
            }

            List<GroupInvite> this[UUI requestingAgent, UUI principal]
            {
                get;
            }

            List<GroupInvite> GetByGroup(UUI requestingAgent, UGI group);

            void Add(UUI requestingAgent, GroupInvite invite);
            void Delete(UUI requestingAgent, UUID inviteID);

        }

        public interface IGroupNoticesInterface
        {
            List<GroupNotice> GetNotices(UUI requestingAgent, UGI group);

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

        public abstract IGroupMembershipsInterface Memberships
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

        public abstract IActiveGroupMembershipInterface ActiveMembership
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

        [Serializable]
        public class AccessFailedException : Exception
        {
            public AccessFailedException()
            {

            }

            public AccessFailedException(string message)
                : base(message)
            {

            }
        }
    }
}
