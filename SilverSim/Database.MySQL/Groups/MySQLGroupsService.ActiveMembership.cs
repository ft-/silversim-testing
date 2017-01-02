// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SilverSim.Types;
using SilverSim.Types.Groups;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IActiveGroupMembershipInterface
    {
        GroupActiveMembership IActiveGroupMembershipInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                GroupActiveMembership gam;
                if(!ActiveMembership.TryGetValue(requestingAgent, principal, out gam))
                {
                    throw new KeyNotFoundException();
                }
                return gam;
            }
        }

        bool IActiveGroupMembershipInterface.ContainsKey(UUI requestingAgent, UUI principal)
        {
            GroupActiveMembership gam;
            return ActiveMembership.TryGetValue(requestingAgent, principal, out gam);
        }

        bool IActiveGroupMembershipInterface.TryGetValue(UUI requestingAgent, UUI principal, out GroupActiveMembership gam)
        {
            gam = default(GroupActiveMembership);
            UGI activegroup;
            if(!ActiveGroup.TryGetValue(requestingAgent, principal, out activegroup))
            {
                return false;
            }
            GroupInfo group;
            if(!Groups.TryGetValue(requestingAgent, activegroup, out group))
            {
                return false;
            }

            GroupMember gmem;
            if(!Members.TryGetValue(requestingAgent, activegroup, principal, out gmem))
            {
                return false;
            }

            GroupRole role;
            if(!Roles.TryGetValue(requestingAgent, activegroup, gmem.SelectedRoleID, out role))
            {
                return false;
            }

            gam = new GroupActiveMembership();
            gam.Group = group.ID;
            gam.SelectedRoleID = gmem.SelectedRoleID;
            gam.User = gmem.Principal;
            return true;
        }
    }
}
