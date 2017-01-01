// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Groups.Common
{
    partial class DefaultPermissionsGroupsService : GroupsServiceInterface.IGroupMembersInterface
    {
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        GroupMember IGroupMembersInterface.this[UUI requestingAgent, UGI group, UUI principal]
        {
            get
            {
                return m_InnerService.Members[requestingAgent, group, principal];
            }
        }

        bool IGroupMembersInterface.TryGetValue(UUI requestingAgent, UGI group, UUI principal, out GroupMember gmem)
        {
            return m_InnerService.Members.TryGetValue(requestingAgent, group, principal, out gmem);
        }

        bool IGroupMembersInterface.ContainsKey(UUI requestingAgent, UGI group, UUI principal)
        {
            return m_InnerService.Members.ContainsKey(requestingAgent, group, principal);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        List<GroupMember> IGroupMembersInterface.this[UUI requestingAgent, UGI group]
        {
            get
            {
                return m_InnerService.Members[requestingAgent, group];
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        List<GroupMember> IGroupMembersInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                return m_InnerService.Members[requestingAgent, principal];
            }
        }

        GroupMember IGroupMembersInterface.Add(UUI requestingAgent, UGI group, UUI principal, UUID roleID, string accessToken)
        {
            if(Invites[requestingAgent, group, roleID, principal].Count == 0)
            {
                VerifyAgentPowers(group, requestingAgent, GroupPowers.Invite);
            }
            return m_InnerService.Members.Add(requestingAgent, group, principal, roleID, accessToken);
        }

        void IGroupMembersInterface.SetContribution(UUI requestingagent, UGI group, UUI principal, int contribution)
        {
            m_InnerService.Members.SetContribution(requestingagent, group, principal, contribution);
        }

        void IGroupMembersInterface.Update(UUI requestingagent, UGI group, UUI principal, bool acceptNotices, bool listInProfile)
        {
            m_InnerService.Members.Update(requestingagent, group, principal, acceptNotices, listInProfile);
        }

        void IGroupMembersInterface.Delete(UUI requestingAgent, UGI group, UUI principal)
        {
            if(requestingAgent != principal && !IsGroupOwner(group, requestingAgent))
            {
                VerifyAgentPowers(group, requestingAgent, GroupPowers.Eject);
            }

            m_InnerService.Members.Delete(requestingAgent, group, principal);
        }
    }
}
