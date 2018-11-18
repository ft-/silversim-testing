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

namespace SilverSim.Groups.Common.Broker
{
    public sealed partial class GroupsBrokerService : IGroupRolesInterface
    {
        List<GroupRole> IGroupRolesInterface.this[UGUI requestingAgent, UGI group] =>
            GetGroupsService(group).Roles[requestingAgent, group];

        List<GroupRole> IGroupRolesInterface.this[UGUI requestingAgent, UGI group, UGUI principal] => 
            GetGroupsService(group).Roles[requestingAgent, group, principal];

        void IGroupRolesInterface.Add(UGUI requestingAgent, GroupRole role) =>
            GetGroupsService(role.Group).Roles.Add(requestingAgent, role);

        bool IGroupRolesInterface.ContainsKey(UGUI requestingAgent, UGI group, UUID roleID)
        {
            GroupsServiceInterface groupsService;
            return TryGetGroupsService(group, out groupsService) && groupsService.Roles.ContainsKey(requestingAgent, group, roleID);
        }

        void IGroupRolesInterface.Delete(UGUI requestingAgent, UGI group, UUID roleID)
        {
            GetGroupsService(group).Roles.Delete(requestingAgent, group, roleID);
        }

        bool IGroupRolesInterface.TryGetValue(UGUI requestingAgent, UGI group, UUID roleID, out GroupRole groupRole)
        {
            groupRole = default(GroupRole);
            GroupsServiceInterface groupsService;
            return TryGetGroupsService(group, out groupsService) && groupsService.Roles.TryGetValue(requestingAgent, group, roleID, out groupRole);
        }

        void IGroupRolesInterface.Update(UGUI requestingAgent, GroupRole role)
        {
            GetGroupsService(role.Group).Roles.Update(requestingAgent, role);
        }
    }
}
