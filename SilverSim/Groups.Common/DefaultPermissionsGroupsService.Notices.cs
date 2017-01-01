// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SilverSim.Groups.Common
{
    partial class DefaultPermissionsGroupsService : GroupsServiceInterface.IGroupNoticesInterface
    {
        List<GroupNotice> IGroupNoticesInterface.GetNotices(UUI requestingAgent, UGI group)
        {
            VerifyAgentPowers(group, requestingAgent, GroupPowers.ReceiveNotices);
            return m_InnerService.Notices.GetNotices(requestingAgent, group);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        GroupNotice IGroupNoticesInterface.this[UUI requestingAgent, UUID groupNoticeID]
        {
            get
            {
                GroupNotice notice;
                if(!Notices.TryGetValue(requestingAgent, groupNoticeID, out notice))
                {
                    throw new KeyNotFoundException();
                }
                return notice;
            }
        }

        bool IGroupNoticesInterface.TryGetValue(UUI requestingAgent, UUID groupNoticeID, out GroupNotice groupNotice)
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

        void IGroupNoticesInterface.Add(UUI requestingAgent, GroupNotice notice)
        {
            VerifyAgentPowers(notice.Group, requestingAgent, GroupPowers.SendNotices);
            m_InnerService.Notices.Add(requestingAgent, notice);
        }

        void IGroupNoticesInterface.Delete(UUI requestingAgent, UUID groupNoticeID)
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
