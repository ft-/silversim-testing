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
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupMembershipsInterface
    {
        List<GroupMembership> IGroupMembershipsInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        GroupMembership IGroupMembershipsInterface.this[UUI requestingAgent, UGI group, UUI principal]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool IGroupMembershipsInterface.ContainsKey(UUI requestingAgent, UGI group, UUI principal)
        {
            throw new NotImplementedException();
        }

        bool IGroupMembershipsInterface.TryGetValue(UUI requestingAgent, UGI group, UUI principal, out GroupMembership gmem)
        {
            throw new NotImplementedException();
        }
    }
}
