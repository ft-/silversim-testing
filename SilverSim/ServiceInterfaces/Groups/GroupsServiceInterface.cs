// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.Groups
{
    public abstract class GroupsServiceInterface
    {
        public interface IGroupsInterface
        {
            GroupInfo Create(UUI requestingAgent, GroupInfo group);
            GroupInfo Update(UUI requestingAgent, GroupInfo group);
            void Delete(UUI requestingAgent, GroupInfo group);

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            UGI this[UUI requestingAgent, UUID groupID]
            {
                get;
            }

            bool TryGetValue(UUI requestingAgent, UUID groupID, out UGI ugi);
            bool ContainsKey(UUI requestingAgent, UUID groupID);

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            GroupInfo this[UUI requestingAgent, UGI group]
            {
                get;
            }

            bool TryGetValue(UUI requestingAgent, UGI groupID, out GroupInfo groupInfo);
            bool ContainsKey(UUI requestingAgent, UGI groupID);

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            GroupInfo this[UUI requestingAgent, string groupName]
            {
                get;
            }

            bool TryGetValue(UUI requestingAgent, string groupName, out GroupInfo groupInfo);
            bool ContainsKey(UUI requestingAgent, string groupName);

            List<DirGroupInfo> GetGroupsByName(UUI requestingAgent, string query);
        }

        public interface IGroupMembershipsInterface
        {
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            GroupMembership this[UUI requestingAgent, UGI group, UUI principal]
            {
                get;
            }

            bool TryGetValue(UUI requestingAgent, UGI group, UUI principal, out GroupMembership gmem);
            bool ContainsKey(UUI requestingAgent, UGI group, UUI principal);

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            List<GroupMembership> this[UUI requestingAgent, UUI principal]
            {
                get;
            }
        }

        public interface IActiveGroupMembershipInterface
        {
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            GroupActiveMembership this[UUI requestingAgent, UUI principal]
            {
                get;
            }

            bool TryGetValue(UUI requestingAgent, UUI principal, out GroupActiveMembership gam);
            bool ContainsKey(UUI requestingAgent, UUI principal);
        }

        public interface IGroupMembersInterface
        {
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            GroupMember this[UUI requestingAgent, UGI group, UUI principal]
            {
                get;
            }

            bool TryGetValue(UUI requestingAgent, UGI group, UUI principal, GroupMember gmem);
            bool ContainsKey(UUI requestingAgent, UGI group, UUI principal);

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            List<GroupMember> this[UUI requestingAgent, UGI group]
            {
                get;
            }

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
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
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            GroupRole this[UUI requestingAgent, UGI group, UUID roleID]
            {
                get;
            }

            bool TryGetValue(UUI requestingAgent, UGI group, UUID roleID, out GroupRole groupRole);
            bool ContainsKey(UUI requestingAgent, UGI group, UUID roleID);

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            List<GroupRole> this[UUI requestingAgent, UGI group]
            {
                get;
            }
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
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
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            GroupRolemember this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
            {
                get;
            }

            bool TryGetValue(UUI requestingAgent, UGI group, UUID roleID, out GroupRolemember grolemem);
            bool ContainsKey(UUI requestingAgent, UGI group, UUID roleID);

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            List<GroupRolemember> this[UUI requestingAgent, UGI group, UUID roleID]
            {
                get;
            }

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            List<GroupRolemembership> this[UUI requestingAgent, UUI principal]
            {
                get;
            }

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            List<GroupRolemember> this[UUI requestingAgent, UGI group]
            {
                get;
            }

            void Add(UUI requestingAgent, GroupRolemember rolemember);
            void Delete(UUI requestingAgent, UGI group, UUID roleID, UUI principal);
        }

        public interface IGroupSelectInterface
        {
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            UGI this[UUI requestingAgent, UUI princialID]
            {
                get;
                set;
            }

            bool TryGetValue(UUI requestingAgent, UUI principalID, out UGI ugi);

            /* get/set active role id */
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            UUID this[UUI requestingAgent, UGI group, UUI principal]
            {
                get;
                set;
            }

            bool TryGetValue(UUI requestingAgent, UGI group, UUI principal, out UUID id);
        }

        public interface IGroupInvitesInterface
        {
            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            GroupInvite this[UUI requestingAgent, UUID groupInviteID]
            {
                get;
            }

            bool TryGetValue(UUI requestingAgent, UUID groupInviteID, out GroupInvite ginvite);

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            List<GroupInvite> this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
            {
                get;
            }

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
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

            [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
            GroupNotice this[UUI requestingAgent, UUID groupNoticeID]
            {
                get;
            }

            bool TryGetValue(UUI requestingAgent, UUID groupNoticeID, out GroupNotice groupNotice);

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

            protected AccessFailedException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

            public AccessFailedException(string message, Exception innerException)
                : base(message, innerException)
            {

            }
        }
    }
}
