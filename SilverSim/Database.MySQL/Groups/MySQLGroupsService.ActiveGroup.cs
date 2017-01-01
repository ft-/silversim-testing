// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using System;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupSelectInterface
    {
        UGI IGroupSelectInterface.this[UUI requestingAgent, UUI princialID]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        UUID IGroupSelectInterface.this[UUI requestingAgent, UGI group, UUI principal]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        bool IGroupSelectInterface.TryGetValue(UUI requestingAgent, UUI principalID, out UGI ugi)
        {
            throw new NotImplementedException();
        }

        bool IGroupSelectInterface.TryGetValue(UUI requestingAgent, UGI group, UUI principal, out UUID id)
        {
            throw new NotImplementedException();
        }
    }
}
