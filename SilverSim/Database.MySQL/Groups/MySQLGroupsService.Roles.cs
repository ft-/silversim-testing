// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupRolesInterface
    {
        List<GroupRole> IGroupRolesInterface.this[UUI requestingAgent, UGI group]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        List<GroupRole> IGroupRolesInterface.this[UUI requestingAgent, UGI group, UUI principal]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        GroupRole IGroupRolesInterface.this[UUI requestingAgent, UGI group, UUID roleID]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        void IGroupRolesInterface.Add(UUI requestingAgent, GroupRole role)
        {
            throw new NotImplementedException();
        }

        bool IGroupRolesInterface.ContainsKey(UUI requestingAgent, UGI group, UUID roleID)
        {
            throw new NotImplementedException();
        }

        void IGroupRolesInterface.Delete(UUI requestingAgent, UGI group, UUID roleID)
        {
            throw new NotImplementedException();
        }

        bool IGroupRolesInterface.TryGetValue(UUI requestingAgent, UGI group, UUID roleID, out GroupRole groupRole)
        {
            throw new NotImplementedException();
        }

        void IGroupRolesInterface.Update(UUI requestingAgent, GroupRole role)
        {
            throw new NotImplementedException();
        }
    }
}
