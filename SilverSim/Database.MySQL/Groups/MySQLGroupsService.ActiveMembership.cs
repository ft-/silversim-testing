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
                throw new NotImplementedException();
            }
        }

        bool IActiveGroupMembershipInterface.ContainsKey(UUI requestingAgent, UUI principal)
        {
            throw new NotImplementedException();
        }

        bool IActiveGroupMembershipInterface.TryGetValue(UUI requestingAgent, UUI principal, out GroupActiveMembership gam)
        {
            throw new NotImplementedException();
        }
    }
}
