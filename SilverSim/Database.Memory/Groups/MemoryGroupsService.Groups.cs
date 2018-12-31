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
    public partial class MemoryGroupsService : IGroupsInterface
    {
        bool IGroupsInterface.ContainsKey(UGUI requestingAgent, UUID groupID) => m_Groups.ContainsKey(groupID);

        bool IGroupsInterface.ContainsKey(UGUI requestingAgent, UGI groupID) => m_Groups.ContainsKey(groupID.ID);

        bool IGroupsInterface.ContainsKey(UGUI requestingAgent, string groupName)
        {
            foreach(MemoryGroupInfo info in m_Groups.Values)
            {
                if(info.ID.GroupName.ToLowerInvariant() == groupName.ToLowerInvariant())
                {
                    return true;
                }
            }
            return false;
        }

        GroupInfo IGroupsInterface.Create(UGUI requestingAgent, GroupInfo group)
        {
            MemoryGroupInfo grpInfo = new MemoryGroupInfo(group);
            m_Groups.Add(group.ID.ID, grpInfo);
            return new GroupInfo(group);
        }

        void IGroupsInterface.Delete(UGUI requestingAgent, UGI group)
        {
            MemoryGroupInfo info;
            if(m_Groups.Remove(group.ID,out info))
            {
                foreach(UUID inviteid in info.Invites.Keys)
                {
                    m_GroupInvitesToGroup.Remove(inviteid);
                }
                foreach(UUID noticeid in info.Notices.Keys)
                {
                    m_GroupNoticeToGroup.Remove(noticeid);
                }
            }
        }

        List<DirGroupInfo> IGroupsInterface.GetGroupsByName(UGUI requestingAgent, string query)
        {
            string lquery = query.ToLower();
            var result = new List<DirGroupInfo>();
            foreach(MemoryGroupInfo info in m_Groups.Values)
            {
                if(info.ID.GroupName.ToLower().Contains(lquery))
                {
                    result.Add(new DirGroupInfo
                    {
                        ID = info.ID,
                        MemberCount = info.Members.Count,
                        SearchOrder = 0
                    });
                }
            }
            return result;
        }

        bool IGroupsInterface.TryGetValue(UGUI requestingAgent, UUID groupID, out UGI ugi)
        {
            MemoryGroupInfo grpInfo;
            if (m_Groups.TryGetValue(groupID, out grpInfo))
            {
                ugi = new UGI(grpInfo.ID);
                return true;
            }
            ugi = default(UGI);
            return false;
        }

        bool IGroupsInterface.TryGetValue(UGUI requestingAgent, UGI groupID, out GroupInfo groupInfo)
        {
            MemoryGroupInfo grpInfo;
            if(m_Groups.TryGetValue(groupID.ID, out grpInfo))
            {
                groupInfo = new GroupInfo(grpInfo)
                {
                    MemberCount = grpInfo.Members.Count,
                    RoleCount = grpInfo.Roles.Count
                };
                return true;
            }
            groupInfo = null;
            return false;
        }

        bool IGroupsInterface.TryGetValue(UGUI requestingAgent, string groupName, out GroupInfo groupInfo)
        {
            foreach(MemoryGroupInfo info in m_Groups.Values)
            {
                if(info.ID.GroupName.ToLower() == groupName.ToLower())
                {
                    groupInfo = new GroupInfo(info)
                    {
                        MemberCount = info.Members.Count,
                        RoleCount = info.Roles.Count
                    };
                    return true;
                }
            }
            groupInfo = null;
            return false;
        }

        GroupInfo IGroupsInterface.Update(UGUI requestingAgent, GroupInfo group)
        {
            MemoryGroupInfo grpInfo;
            if(m_Groups.TryGetValue(group.ID.ID, out grpInfo))
            {
                grpInfo.Charter = group.Charter;
                grpInfo.InsigniaID = group.InsigniaID;
                grpInfo.Founder = new UGUI(group.Founder);
                grpInfo.MembershipFee = group.MembershipFee;
                grpInfo.IsOpenEnrollment = group.IsOpenEnrollment;
                grpInfo.IsShownInList = group.IsShownInList;
                grpInfo.IsAllowPublish = group.IsAllowPublish;
                grpInfo.IsMaturePublish = group.IsMaturePublish;
                grpInfo.OwnerRoleID = group.OwnerRoleID;
                return new GroupInfo(grpInfo)
                {
                    MemberCount = grpInfo.Members.Count
                };
            }
            throw new KeyNotFoundException();
        }
    }
}
