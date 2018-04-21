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

using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SilverSim.ServiceInterfaces.Groups
{
    public abstract class GroupsServiceInterface
    {
        public interface IGroupsInterface
        {
            GroupInfo Create(UGUI requestingAgent, GroupInfo group);
            GroupInfo Update(UGUI requestingAgent, GroupInfo group);
            void Delete(UGUI requestingAgent, UGI group);

            UGI this[UGUI requestingAgent, UUID groupID] { get; }

            bool TryGetValue(UGUI requestingAgent, UUID groupID, out UGI ugi);
            bool ContainsKey(UGUI requestingAgent, UUID groupID);

            GroupInfo this[UGUI requestingAgent, UGI group] { get; }

            bool TryGetValue(UGUI requestingAgent, UGI groupID, out GroupInfo groupInfo);
            bool ContainsKey(UGUI requestingAgent, UGI groupID);

            GroupInfo this[UGUI requestingAgent, string groupName] { get; }

            bool TryGetValue(UGUI requestingAgent, string groupName, out GroupInfo groupInfo);
            bool ContainsKey(UGUI requestingAgent, string groupName);

            List<DirGroupInfo> GetGroupsByName(UGUI requestingAgent, string query);
        }

        public interface IGroupMembershipsInterface
        {
            GroupMembership this[UGUI requestingAgent, UGI group, UGUI principal] { get; }

            bool TryGetValue(UGUI requestingAgent, UGI group, UGUI principal, out GroupMembership gmem);
            bool ContainsKey(UGUI requestingAgent, UGI group, UGUI principal);

            List<GroupMembership> this[UGUI requestingAgent, UGUI principal] { get; }
        }

        public interface IActiveGroupMembershipInterface
        {
            GroupActiveMembership this[UGUI requestingAgent, UGUI principal] { get; }

            bool TryGetValue(UGUI requestingAgent, UGUI principal, out GroupActiveMembership gam);
            bool ContainsKey(UGUI requestingAgent, UGUI principal);
        }

        public interface IGroupMembersInterface
        {
            GroupMember this[UGUI requestingAgent, UGI group, UGUI principal] { get; }

            bool TryGetValue(UGUI requestingAgent, UGI group, UGUI principal, out GroupMember gmem);
            bool ContainsKey(UGUI requestingAgent, UGI group, UGUI principal);

            List<GroupMember> this[UGUI requestingAgent, UGI group] { get; }

            List<GroupMember> this[UGUI requestingAgent, UGUI principal] { get; }

            GroupMember Add(UGUI requestingAgent, UGI group, UGUI principal, UUID roleID, string accessToken);
            void SetContribution(UGUI requestingagent, UGI group, UGUI principal, int contribution);
            void Update(UGUI requestingagent, UGI group, UGUI principal, bool acceptNotices, bool listInProfile);
            void Delete(UGUI requestingAgent, UGI group, UGUI principal);
        }

        public interface IGroupRolesInterface
        {
            GroupRole this[UGUI requestingAgent, UGI group, UUID roleID] { get; }

            bool TryGetValue(UGUI requestingAgent, UGI group, UUID roleID, out GroupRole groupRole);
            bool ContainsKey(UGUI requestingAgent, UGI group, UUID roleID);

            List<GroupRole> this[UGUI requestingAgent, UGI group] { get; }
            List<GroupRole> this[UGUI requestingAgent, UGI group, UGUI principal] { get; }

            void Add(UGUI requestingAgent, GroupRole role);
            void Update(UGUI requestingAgent, GroupRole role);
            void Delete(UGUI requestingAgent, UGI group, UUID roleID);
        }

        public interface IGroupRolemembersInterface
        {
            GroupRolemember this[UGUI requestingAgent, UGI group, UUID roleID, UGUI principal] { get; }

            bool TryGetValue(UGUI requestingAgent, UGI group, UUID roleID, UGUI principal, out GroupRolemember grolemem);
            bool ContainsKey(UGUI requestingAgent, UGI group, UUID roleID, UGUI principal);

            List<GroupRolemember> this[UGUI requestingAgent, UGI group, UUID roleID] { get; }

            List<GroupRolemembership> this[UGUI requestingAgent, UGUI principal] { get; }

            List<GroupRolemember> this[UGUI requestingAgent, UGI group] { get; }

            void Add(UGUI requestingAgent, GroupRolemember rolemember);
            void Delete(UGUI requestingAgent, UGI group, UUID roleID, UGUI principal);
        }

        public interface IGroupSelectInterface
        {
            UGI this[UGUI requestingAgent, UGUI princialID] { get; set; }

            bool TryGetValue(UGUI requestingAgent, UGUI principalID, out UGI ugi);

            /* get/set active role id */
            UUID this[UGUI requestingAgent, UGI group, UGUI principal] { get; set; }

            bool TryGetValue(UGUI requestingAgent, UGI group, UGUI principal, out UUID id);
        }

        public interface IGroupInvitesInterface
        {
            GroupInvite this[UGUI requestingAgent, UUID groupInviteID] { get; }

            bool TryGetValue(UGUI requestingAgent, UUID groupInviteID, out GroupInvite ginvite);
            bool ContainsKey(UGUI requestingAgent, UUID groupInviteID);

            bool DoesSupportListGetters { get; }

            /** <summary>Only for use of Permission modules</summary> */
            List<GroupInvite> this[UGUI requestingAgent, UGI group, UUID roleID, UGUI principal] { get; }

            /** <summary>Only for use of Permission modules</summary> */
            List<GroupInvite> this[UGUI requestingAgent, UGUI principal] { get; }

            /** <summary>Only for use of Permission modules</summary> */
            List<GroupInvite> GetByGroup(UGUI requestingAgent, UGI group);

            void Add(UGUI requestingAgent, GroupInvite invite);
            void Delete(UGUI requestingAgent, UUID inviteID);
        }

        public interface IGroupNoticesInterface
        {
            List<GroupNotice> GetNotices(UGUI requestingAgent, UGI group);

            GroupNotice this[UGUI requestingAgent, UUID groupNoticeID] { get; }

            bool TryGetValue(UGUI requestingAgent, UUID groupNoticeID, out GroupNotice groupNotice);
            bool ContainsKey(UGUI requestingAgent, UUID groupNoticeID);

            void Add(UGUI requestingAgent, GroupNotice notice);

            void Delete(UGUI requestingAgent, UUID groupNoticeID);
        }

        public abstract IGroupsInterface Groups { get; }

        public abstract IGroupRolesInterface Roles { get; }

        public abstract IGroupMembersInterface Members { get; }

        public abstract IGroupMembershipsInterface Memberships { get; }

        public abstract IGroupRolemembersInterface Rolemembers { get; }

        public abstract IGroupSelectInterface ActiveGroup { get; }

        public abstract IActiveGroupMembershipInterface ActiveMembership { get; }

        public abstract IGroupInvitesInterface Invites { get; }

        public abstract IGroupNoticesInterface Notices { get; }

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

        public virtual GroupInfo CreateGroup(UGUI requestingAgent, GroupInfo ginfo, GroupPowers everyonePowers, GroupPowers ownerPowers)
        {
            var role_everyone = new GroupRole
            {
                ID = UUID.Zero,
                Group = ginfo.ID,
                Name = "Everyone",
                Description = "Everyone in the group",
                Title = "Member of " + ginfo.ID.GroupName,
                Powers = everyonePowers
            };
            var role_owner = new GroupRole
            {
                ID = UUID.Random,
                Group = ginfo.ID,
                Name = "Owners",
                Description = "Owners of the group",
                Title = "Owner of " + ginfo.ID.GroupName,
                Powers = ownerPowers
            };
            ginfo.OwnerRoleID = role_owner.ID;

            var gmemrole_owner = new GroupRolemember
            {
                Group = ginfo.ID,
                RoleID = role_owner.ID,
                Principal = ginfo.Founder
            };
            var gmemrole_everyone = new GroupRolemember
            {
                Group = ginfo.ID,
                RoleID = role_everyone.ID,
                Principal = ginfo.Founder
            };
            Groups.Create(requestingAgent, ginfo);

            try
            {
                Roles.Add(requestingAgent, role_everyone);
                Roles.Add(requestingAgent, role_owner);
                Members.Add(requestingAgent, ginfo.ID, ginfo.Founder, role_owner.ID, UUID.Random.ToString());
                Rolemembers.Add(requestingAgent, gmemrole_owner);
                Rolemembers.Add(requestingAgent, gmemrole_everyone);
                ginfo.RoleCount = 2;
                ginfo.MemberCount = 1;
            }
            catch
            {
                Groups.Delete(requestingAgent, ginfo.ID);
                throw;
            }
            return ginfo;
        }

        public GroupMember AddAgentToGroup(UGUI requestingAgent, UGI group, UUID roleid, UGUI agent, string accessToken)
        {
            bool alreadyInGroup;

            GroupMember gmem;
            alreadyInGroup = Members.TryGetValue(requestingAgent, group, agent, out gmem);
            if(!alreadyInGroup)
            {
                gmem = Members.Add(requestingAgent, group, agent, roleid, accessToken);
            }

            try
            {
                if (!Rolemembers.ContainsKey(requestingAgent, group, UUID.Zero, agent))
                {
                    var rolemember = new GroupRolemember
                    {
                        Group = group,
                        Principal = agent,
                        RoleID = UUID.Zero
                    };
                    Rolemembers.Add(requestingAgent, rolemember);
                }

                if(UUID.Zero != roleid)
                {
                    var rolemember = new GroupRolemember
                    {
                        Group = group,
                        Principal = agent,
                        RoleID = roleid
                    };
                    Rolemembers.Add(requestingAgent, rolemember);
                }

                try
                {
                    var invites = Invites[requestingAgent, group, roleid, agent];
                    foreach(var invite in invites)
                    {
                        invites.Remove(invite);
                    }
                }
                catch
                {
                    /* intentionally ignored */
                }
            }
            catch
            {
                if(!alreadyInGroup)
                {
                    Members.Delete(requestingAgent, group, agent);
                }
            }

            return gmem;
        }

        public virtual GroupPowers GetAgentPowers(UGI group, UGUI agent)
        {
            var rolemembers = Rolemembers[agent, group];
            var powers = GroupPowers.None;
            foreach(var rolemember in rolemembers)
            {
                GroupRole role;
                if(Roles.TryGetValue(agent, group, rolemember.RoleID, out role))
                {
                    powers |= role.Powers;
                }
            }
            return powers;
        }

        public void VerifyAgentPowers(UGI group, UGUI agent, GroupPowers powers)
        {
            VerifyAgentPowers(group, agent, new GroupPowers[] { powers });
        }

        public void VerifyAgentPowers(UGI group, UGUI agent, GroupPowers[] powers)
        {
            var agentPowers = GetAgentPowers(group, agent);

            foreach(var power in powers)
            {
                if((agentPowers & power) == 0)
                {
                    throw new GroupInsufficientPowersException(power);
                }
            }
        }

        [Serializable]
        public class GroupInsufficientPowersException : Exception
        {
            public GroupInsufficientPowersException(GroupPowers power)
                : base(string.Format("Missing group permission {0}", power.ToString()))
            {
            }
        }
    }
}
