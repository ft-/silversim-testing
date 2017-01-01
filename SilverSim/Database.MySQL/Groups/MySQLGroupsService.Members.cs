// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupMembersInterface
    {
        List<GroupMember> IGroupMembersInterface.this[UUI requestingAgent, UUI principal]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        List<GroupMember> IGroupMembersInterface.this[UUI requestingAgent, UGI group]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        GroupMember IGroupMembersInterface.this[UUI requestingAgent, UGI group, UUI principal]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        GroupMember IGroupMembersInterface.Add(UUI requestingAgent, UGI group, UUI principal, UUID roleID, string accessToken)
        {
            throw new NotImplementedException();
        }

        bool IGroupMembersInterface.ContainsKey(UUI requestingAgent, UGI group, UUI principal)
        {
            throw new NotImplementedException();
        }

        void IGroupMembersInterface.Delete(UUI requestingAgent, UGI group, UUI principal)
        {
            throw new NotImplementedException();
        }

        void IGroupMembersInterface.SetContribution(UUI requestingagent, UGI group, UUI principal, int contribution)
        {
            throw new NotImplementedException();
        }

        bool IGroupMembersInterface.TryGetValue(UUI requestingAgent, UGI group, UUI principal, out GroupMember gmem)
        {
            throw new NotImplementedException();
        }

        void IGroupMembersInterface.Update(UUI requestingagent, UGI group, UUI principal, bool acceptNotices, bool listInProfile)
        {
            throw new NotImplementedException();
        }
    }
}
