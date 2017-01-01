// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

namespace SilverSim.Database.MySQL.Groups
{
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupsInterface
    {
        GroupInfo IGroupsInterface.this[UUI requestingAgent, UGI group]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        GroupInfo IGroupsInterface.this[UUI requestingAgent, string groupName]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        UGI IGroupsInterface.this[UUI requestingAgent, UUID groupID]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool IGroupsInterface.ContainsKey(UUI requestingAgent, string groupName)
        {
            throw new NotImplementedException();
        }

        bool IGroupsInterface.ContainsKey(UUI requestingAgent, UGI groupID)
        {
            throw new NotImplementedException();
        }

        bool IGroupsInterface.ContainsKey(UUI requestingAgent, UUID groupID)
        {
            throw new NotImplementedException();
        }

        GroupInfo IGroupsInterface.Create(UUI requestingAgent, GroupInfo group)
        {
            throw new NotImplementedException();
        }

        void IGroupsInterface.Delete(UUI requestingAgent, GroupInfo group)
        {
            throw new NotImplementedException();
        }

        List<DirGroupInfo> IGroupsInterface.GetGroupsByName(UUI requestingAgent, string query)
        {
            throw new NotImplementedException();
        }

        bool IGroupsInterface.TryGetValue(UUI requestingAgent, string groupName, out GroupInfo groupInfo)
        {
            throw new NotImplementedException();
        }

        bool IGroupsInterface.TryGetValue(UUI requestingAgent, UGI groupID, out GroupInfo groupInfo)
        {
            throw new NotImplementedException();
        }

        bool IGroupsInterface.TryGetValue(UUI requestingAgent, UUID groupID, out UGI ugi)
        {
            throw new NotImplementedException();
        }

        GroupInfo IGroupsInterface.Update(UUI requestingAgent, GroupInfo group)
        {
            throw new NotImplementedException();
        }
    }
}
