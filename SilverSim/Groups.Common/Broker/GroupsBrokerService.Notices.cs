// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;

namespace SilverSim.Groups.Common.Broker
{
    public sealed partial class GroupsBrokerService : IGroupNoticesInterface
    {
        void IGroupNoticesInterface.Add(UGUI requestingAgent, GroupNotice notice) => 
            GetGroupsService(notice.Group).Notices.Add(requestingAgent, notice);

        /* TODO: revise this API */
        bool IGroupNoticesInterface.ContainsKey(UGUI requestingAgent, UUID groupNoticeID) => false;

        /* TODO: revise this API */
        void IGroupNoticesInterface.Delete(UGUI requestingAgent, UUID groupNoticeID)
        {
            throw new NotImplementedException();
        }

        List<GroupNotice> IGroupNoticesInterface.GetNotices(UGUI requestingAgent, UGI group)
        {
            return GetGroupsService(group).Notices.GetNotices(requestingAgent, group);
        }

        /* TODO: revise this API */
        bool IGroupNoticesInterface.TryGetValue(UGUI requestingAgent, UUID groupNoticeID, out GroupNotice groupNotice)
        {
            groupNotice = null;
            return false;
        }
    }
}
