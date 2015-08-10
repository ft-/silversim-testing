// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Types.Groups;

namespace SilverSim.ServiceInterfaces.Groups
{
    public abstract class GroupsNameServiceInterface
    {
        public GroupsNameServiceInterface()
        {

        }

        public abstract UGI this[UUID groupID]
        {
            get;
        }

        public abstract List<UGI> GetGroupsByName(string groupName, int limit);

        public abstract void Store(UGI group);
        
        public void Store(GroupInfo group)
        {
            Store(group.ID);
        }
    }
}
