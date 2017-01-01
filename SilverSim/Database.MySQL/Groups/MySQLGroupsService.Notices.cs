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
    partial class MySQLGroupsService : GroupsServiceInterface.IGroupNoticesInterface
    {
        GroupNotice IGroupNoticesInterface.this[UUI requestingAgent, UUID groupNoticeID]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        void IGroupNoticesInterface.Add(UUI requestingAgent, GroupNotice notice)
        {
            throw new NotImplementedException();
        }

        void IGroupNoticesInterface.Delete(UUI requestingAgent, UUID groupNoticeID)
        {
            throw new NotImplementedException();
        }

        List<GroupNotice> IGroupNoticesInterface.GetNotices(UUI requestingAgent, UGI group)
        {
            throw new NotImplementedException();
        }

        bool IGroupNoticesInterface.TryGetValue(UUI requestingAgent, UUID groupNoticeID, out GroupNotice groupNotice)
        {
            throw new NotImplementedException();
        }
    }
}
