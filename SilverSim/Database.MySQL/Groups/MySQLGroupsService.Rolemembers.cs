// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupRolemembersInterface
    {
        List<GroupRolemember> IGroupRolemembersInterface.this[UUI requestingAgent, UGI group]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        List<GroupRolemembership> IGroupRolemembersInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        List<GroupRolemember> IGroupRolemembersInterface.this[UUI requestingAgent, UGI group, UUID roleID]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        GroupRolemember IGroupRolemembersInterface.this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        void IGroupRolemembersInterface.Add(UUI requestingAgent, GroupRolemember rolemember)
        {
            throw new NotImplementedException();
        }

        bool IGroupRolemembersInterface.ContainsKey(UUI requestingAgent, UGI group, UUID roleID, UUI principal)
        {
            throw new NotImplementedException();
        }

        void IGroupRolemembersInterface.Delete(UUI requestingAgent, UGI group, UUID roleID, UUI principal)
        {
            throw new NotImplementedException();
        }

        bool IGroupRolemembersInterface.TryGetValue(UUI requestingAgent, UGI group, UUID roleID, UUI principal, out GroupRolemember grolemem)
        {
            throw new NotImplementedException();
        }
    }
}
