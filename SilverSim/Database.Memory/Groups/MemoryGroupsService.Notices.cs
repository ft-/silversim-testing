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

namespace SilverSim.Database.Memory.Groups
{
    public partial class MemoryGroupsService : IGroupNoticesInterface
    {
        void IGroupNoticesInterface.Add(UGUI requestingAgent, GroupNotice notice)
        {
            m_GroupNoticeToGroup.Add(notice.Group.ID, notice.ID);
            try
            {
                m_Groups[notice.Group.ID].Notices.Add(notice.Group.ID, new GroupNotice(notice));
            }
            catch
            {
                m_GroupNoticeToGroup.Remove(notice.Group.ID);
                throw;
            }
        }

        bool IGroupNoticesInterface.ContainsKey(UGUI requestingAgent, UUID groupNoticeID) =>
            m_GroupNoticeToGroup.ContainsKey(groupNoticeID);

        void IGroupNoticesInterface.Delete(UGUI requestingAgent, UUID groupNoticeID)
        {
            UUID groupid;
            MemoryGroupInfo groupInfo;
            if(m_GroupNoticeToGroup.TryGetValue(groupNoticeID, out groupid) && 
                m_Groups.TryGetValue(groupid, out groupInfo) && groupInfo.Notices.Remove(groupNoticeID))
            {
                m_GroupNoticeToGroup.Remove(groupNoticeID);
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }

        List<GroupNotice> IGroupNoticesInterface.GetNotices(UGUI requestingAgent, UGI group)
        {
            var list = new List<GroupNotice>();
            MemoryGroupInfo info;
            if(m_Groups.TryGetValue(group.ID, out info))
            {
                foreach(GroupNotice notice in info.Notices.Values)
                {
                    list.Add(new GroupNotice(notice));
                }
            }
            return list;
        }

        bool IGroupNoticesInterface.TryGetValue(UGUI requestingAgent, UUID groupNoticeID, out GroupNotice groupNotice)
        {
            MemoryGroupInfo info;
            GroupNotice notice;
            UUID groupID;
            groupNotice = null;
            if(m_GroupNoticeToGroup.TryGetValue(groupNoticeID, out groupID) &&
                m_Groups.TryGetValue(groupID, out info) &&
                info.Notices.TryGetValue(groupNoticeID, out notice))
            {
                groupNotice = new GroupNotice(notice);
                return true;
            }
            return false;
        }
    }
}
