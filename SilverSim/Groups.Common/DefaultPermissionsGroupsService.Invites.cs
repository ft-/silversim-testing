// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Groups.Common
{
    partial class DefaultPermissionsGroupsService : GroupsServiceInterface.IGroupInvitesInterface
    {
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        GroupInvite IGroupInvitesInterface.this[UUI requestingAgent, UUID groupInviteID]
        {
            get
            {
                return m_InnerService.Invites[requestingAgent, groupInviteID];
            }
        }

        bool IGroupInvitesInterface.TryGetValue(UUI requestingAgent, UUID groupInviteID, out GroupInvite ginvite)
        {
            return m_InnerService.Invites.TryGetValue(requestingAgent, groupInviteID, out ginvite);
        }

        bool IGroupInvitesInterface.ContainsKey(UUI requestingAgent, UUID groupInviteID)
        {
            return m_InnerService.Invites.ContainsKey(requestingAgent, groupInviteID);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        List<GroupInvite> IGroupInvitesInterface.this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
        {
            get
            {
                return m_InnerService.Invites[requestingAgent, group, roleID, principal];
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        List<GroupInvite> IGroupInvitesInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                return m_InnerService.Invites[requestingAgent, principal];
            }
        }

        List<GroupInvite> IGroupInvitesInterface.GetByGroup(UUI requestingAgent, UGI group)
        {
            return m_InnerService.Invites.GetByGroup(requestingAgent, group);
        }

        void IGroupInvitesInterface.Add(UUI requestingAgent, GroupInvite invite)
        {
            VerifyAgentPowers(invite.Group, requestingAgent, GroupPowers.Invite);
            m_InnerService.Invites.Add(requestingAgent, invite);
        }

        void IGroupInvitesInterface.Delete(UUI requestingAgent, UUID inviteID)
        {
            m_InnerService.Invites.Delete(requestingAgent, inviteID);
        }
    }
}
