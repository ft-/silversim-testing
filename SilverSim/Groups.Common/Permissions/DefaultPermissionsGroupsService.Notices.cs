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
using System.Collections.Generic;

namespace SilverSim.Groups.Common.Permissions
{
    partial class DefaultPermissionsGroupsService : IGroupNoticesInterface
    {
        List<GroupNotice> IGroupNoticesInterface.GetNotices(UGUI requestingAgent, UGI group)
        {
            VerifyAgentPowers(group, requestingAgent, GroupPowers.ReceiveNotices);
            return m_InnerService.Notices.GetNotices(requestingAgent, group);
        }

        bool IGroupNoticesInterface.TryGetValue(UGUI requestingAgent, UUID groupNoticeID, out GroupNotice groupNotice)
        {
            GroupNotice gnot;
            if(m_InnerService.Notices.TryGetValue(requestingAgent, groupNoticeID, out gnot))
            {
                VerifyAgentPowers(gnot.Group, requestingAgent, GroupPowers.ReceiveNotices);
                groupNotice = gnot;
                return true;
            }
            groupNotice = null;
            return false;
        }

        bool IGroupNoticesInterface.ContainsKey(UGUI requestingAgent, UUID groupNoticeID)
        {
            GroupNotice gnot;
            if (m_InnerService.Notices.TryGetValue(requestingAgent, groupNoticeID, out gnot))
            {
                VerifyAgentPowers(gnot.Group, requestingAgent, GroupPowers.ReceiveNotices);
                return true;
            }
            return false;
        }

        void IGroupNoticesInterface.Add(UGUI requestingAgent, GroupNotice notice)
        {
            VerifyAgentPowers(notice.Group, requestingAgent, GroupPowers.SendNotices);
            m_InnerService.Notices.Add(requestingAgent, notice);
        }

        void IGroupNoticesInterface.Delete(UGUI requestingAgent, UUID groupNoticeID)
        {
            GroupNotice gnot;
            if (m_InnerService.Notices.TryGetValue(requestingAgent, groupNoticeID, out gnot))
            {
                VerifyAgentPowers(gnot.Group, requestingAgent, GroupPowers.SendNotices);
                m_InnerService.Notices.Delete(requestingAgent, groupNoticeID);
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }
    }
}
