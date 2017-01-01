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
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupInvitesInterface
    {
        List<GroupInvite> IGroupInvitesInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        GroupInvite IGroupInvitesInterface.this[UUI requestingAgent, UUID groupInviteID]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        List<GroupInvite> IGroupInvitesInterface.this[UUI requestingAgent, UGI group, UUID roleID, UUI principal]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        void IGroupInvitesInterface.Add(UUI requestingAgent, GroupInvite invite)
        {
            throw new NotImplementedException();
        }

        bool IGroupInvitesInterface.ContainsKey(UUI requestingAgent, UUID groupInviteID)
        {
            throw new NotImplementedException();
        }

        void IGroupInvitesInterface.Delete(UUI requestingAgent, UUID inviteID)
        {
            throw new NotImplementedException();
        }

        List<GroupInvite> IGroupInvitesInterface.GetByGroup(UUI requestingAgent, UGI group)
        {
            throw new NotImplementedException();
        }

        bool IGroupInvitesInterface.TryGetValue(UUI requestingAgent, UUID groupInviteID, out GroupInvite ginvite)
        {
            throw new NotImplementedException();
        }
    }
}
