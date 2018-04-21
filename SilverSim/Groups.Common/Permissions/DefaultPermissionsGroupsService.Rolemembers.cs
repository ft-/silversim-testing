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
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;

namespace SilverSim.Groups.Common.Permissions
{
    partial class DefaultPermissionsGroupsService: GroupsServiceInterface.IGroupRolemembersInterface
    {
        GroupRolemember IGroupRolemembersInterface.this[UGUI requestingAgent, UGI group, UUID roleID, UGUI principal] =>
            m_InnerService.Rolemembers[requestingAgent, group, roleID, principal];

        bool IGroupRolemembersInterface.TryGetValue(UGUI requestingAgent, UGI group, UUID roleID, UGUI principal, out GroupRolemember grolemem) =>
            m_InnerService.Rolemembers.TryGetValue(requestingAgent, group, roleID, principal, out grolemem);

        bool IGroupRolemembersInterface.ContainsKey(UGUI requestingAgent, UGI group, UUID roleID, UGUI principal) =>
            m_InnerService.Rolemembers.ContainsKey(requestingAgent, group, roleID, principal);

        List<GroupRolemember> IGroupRolemembersInterface.this[UGUI requestingAgent, UGI group, UUID roleID] =>
            m_InnerService.Rolemembers[requestingAgent, group, roleID];

        List<GroupRolemembership> IGroupRolemembersInterface.this[UGUI requestingAgent, UGUI principal] =>
            m_InnerService.Rolemembers[requestingAgent, principal];

        List<GroupRolemember> IGroupRolemembersInterface.this[UGUI requestingAgent, UGI group] =>
            m_InnerService.Rolemembers[requestingAgent, group];

        void IGroupRolemembersInterface.Add(UGUI requestingAgent, GroupRolemember rolemember)
        {
            bool isUnlimited = false;
            if(m_InnerService.Invites[requestingAgent, rolemember.Group, rolemember.RoleID, rolemember.Principal].Count != 0)
            {
            }
            else
            {
                if(!IsGroupOwner(rolemember.Group, requestingAgent))
                {
                    try
                    {
                        VerifyAgentPowers(rolemember.Group, requestingAgent, GroupPowers.AssignMember);
                        isUnlimited = true;
                    }
                    catch(GroupInsufficientPowersException)
                    {
                        VerifyAgentPowers(rolemember.Group, requestingAgent, GroupPowers.AssignMemberLimited);
                    }
                }
                else
                {
                    isUnlimited = true;
                }

                if(!isUnlimited && !Rolemembers.ContainsKey(requestingAgent, rolemember.Group, rolemember.RoleID, requestingAgent))
                {
                    throw new GroupInsufficientPowersException(GroupPowers.AssignMemberLimited);
                }
            }

            m_InnerService.Rolemembers.Add(requestingAgent, rolemember);
        }

        void IGroupRolemembersInterface.Delete(UGUI requestingAgent, UGI group, UUID roleID, UGUI principal)
        {
            bool isUnlimited = false;
            if (!IsGroupOwner(group, requestingAgent))
            {
                try
                {
                    VerifyAgentPowers(group, requestingAgent, GroupPowers.AssignMember);
                    isUnlimited = true;
                }
                catch (GroupInsufficientPowersException)
                {
                    VerifyAgentPowers(group, requestingAgent, GroupPowers.AssignMemberLimited);
                }
            }
            else
            {
                isUnlimited = true;
            }

            if (!isUnlimited && !Rolemembers.ContainsKey(requestingAgent, group, roleID, requestingAgent))
            {
                throw new GroupInsufficientPowersException(GroupPowers.AssignMemberLimited);
            }

            m_InnerService.Rolemembers.Delete(requestingAgent, group, roleID, principal);
        }
    }
}
