// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Groups.Common
{
    partial class DefaultPermissionsGroupService : GroupsServiceInterface.IGroupRolesInterface
    {
        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        GroupRole IGroupRolesInterface.this[UUI requestingAgent, UGI group, UUID roleID]
        {
            get
            {
                return m_InnerService.Roles[requestingAgent, group, roleID];
            }
        }

        bool IGroupRolesInterface.TryGetValue(UUI requestingAgent, UGI group, UUID roleID, out GroupRole groupRole)
        {
            return m_InnerService.Roles.TryGetValue(requestingAgent, group, roleID, out groupRole);
        }

        bool IGroupRolesInterface.ContainsKey(UUI requestingAgent, UGI group, UUID roleID)
        {
            return m_InnerService.Roles.ContainsKey(requestingAgent, group, roleID);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        List<GroupRole> IGroupRolesInterface.this[UUI requestingAgent, UGI group]
        {
            get
            {
                return m_InnerService.Roles[requestingAgent, group];
            }
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        List<GroupRole> IGroupRolesInterface.this[UUI requestingAgent, UGI group, UUI principal]
        {
            get
            {
                return m_InnerService.Roles[requestingAgent, group, principal];
            }
        }

        void IGroupRolesInterface.Add(UUI requestingAgent, GroupRole role)
        {
            VerifyAgentPowers(role.Group, requestingAgent, GroupPowers.CreateRole);
            m_InnerService.Roles.Add(requestingAgent, role);
        }

        void IGroupRolesInterface.Update(UUI requestingAgent, GroupRole role)
        {
            if(!IsGroupOwner(role.Group, requestingAgent))
            {
                VerifyAgentPowers(role.Group, requestingAgent, GroupPowers.RoleProperties);
            }
            m_InnerService.Roles.Update(requestingAgent, role);
        }

        void IGroupRolesInterface.Delete(UUI requestingAgent, UGI group, UUID roleID)
        {
            if (!IsGroupOwner(group, requestingAgent))
            {
                VerifyAgentPowers(group, requestingAgent, GroupPowers.DeleteRole);
            }
            m_InnerService.Roles.Delete(requestingAgent, group, roleID);
        }
    }
}
