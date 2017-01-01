// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Groups.Common
{
    partial class DefaultPermissionsGroupsService: GroupsServiceInterface.IGroupRolemembersInterface
    {
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        GroupRolemember IGroupRolemembersInterface.this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
        {
            get
            {
                return m_InnerService.Rolemembers[requestingAgent, group, roleID, principal];
            }
        }

        bool IGroupRolemembersInterface.TryGetValue(UUI requestingAgent, UGI group, UUID roleID, UUI principal, out GroupRolemember grolemem)
        {
            return m_InnerService.Rolemembers.TryGetValue(requestingAgent, group, roleID, principal, out grolemem);
        }

        bool IGroupRolemembersInterface.ContainsKey(UUI requestingAgent, UGI group, UUID roleID, UUI principal)
        {
            return m_InnerService.Rolemembers.ContainsKey(requestingAgent, group, roleID, principal);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        List<GroupRolemember> IGroupRolemembersInterface.this[UUI requestingAgent, UGI group, UUID roleID]
        {
            get
            {
                return m_InnerService.Rolemembers[requestingAgent, group, roleID];
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        List<GroupRolemembership> IGroupRolemembersInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                return m_InnerService.Rolemembers[requestingAgent, principal];
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        List<GroupRolemember> IGroupRolemembersInterface.this[UUI requestingAgent, UGI group]
        {
            get
            {
                return m_InnerService.Rolemembers[requestingAgent, group];
            }
        }

        void IGroupRolemembersInterface.Add(UUI requestingAgent, GroupRolemember rolemember)
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

        void IGroupRolemembersInterface.Delete(UUI requestingAgent, UGI group, UUID roleID, UUI principal)
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
