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
    public partial class MemoryGroupsService : IGroupMembersInterface
    {
        List<GroupMember> IGroupMembersInterface.this[UGUI requestingAgent, UGI group]
        {
            get
            {
                var list = new List<GroupMember>();
                MemoryGroupInfo info;
                if(m_Groups.TryGetValue(group.ID, out info))
                {
                    foreach(MemoryGroupMember mem in info.Members.Values)
                    {
                        list.Add(new GroupMember(mem));
                    }
                }
                return list;
            }
        }

        List<GroupMember> IGroupMembersInterface.this[UGUI requestingAgent, UGUI principal]
        {
            get
            {
                var list = new List<GroupMember>();
                foreach(MemoryGroupInfo info in m_Groups.Values)
                {
                    foreach(MemoryGroupMember mem in info.Members.Values)
                    {
                        if(mem.Principal.EqualsGrid(principal))
                        {
                            list.Add(new GroupMember(mem));
                        }
                    }
                }
                return list;
            }
        }

        GroupMember IGroupMembersInterface.Add(UGUI requestingAgent, UGI group, UGUI principal, UUID roleID, string accessToken)
        {
            MemoryGroupInfo info;
            MemoryGroupMember mem;
            if(m_Groups.TryGetValue(group.ID, out info))
            {
                mem = new MemoryGroupMember
                {
                    Group = info.ID,
                    Principal = principal,
                    SelectedRoleID = roleID,
                    Contribution = 0,
                    IsListInProfile = true,
                    IsAcceptNotices = true,
                    AccessToken = accessToken
                };
                info.Members.Add(principal, mem);
                return new GroupMember(mem);
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }

        bool IGroupMembersInterface.ContainsKey(UGUI requestingAgent, UGI group, UGUI principal)
        {
            MemoryGroupInfo info;
            return m_Groups.TryGetValue(group.ID, out info) && info.Members.ContainsKey(principal);
        }

        void IGroupMembersInterface.Delete(UGUI requestingAgent, UGI group, UGUI principal)
        {
            MemoryGroupInfo info;
            if(m_Groups.TryGetValue(group.ID, out info) && info.Members.Remove(principal))
            {
                foreach(MemoryGroupRole role in info.Roles.Values)
                {
                    role.Rolemembers.Remove(principal);
                }
                return;
            }
            throw new KeyNotFoundException();
        }

        void IGroupMembersInterface.SetContribution(UGUI requestingagent, UGI group, UGUI principal, int contribution)
        {
            MemoryGroupInfo info;
            MemoryGroupMember member;
            if(m_Groups.TryGetValue(group.ID, out info) && info.Members.TryGetValue(principal, out member))
            {
                member.Contribution = contribution;
                return;
            }
            throw new KeyNotFoundException();
        }

        bool IGroupMembersInterface.TryGetValue(UGUI requestingAgent, UGI group, UGUI principal, out GroupMember gmem)
        {
            MemoryGroupInfo info;
            MemoryGroupMember member;
            if (m_Groups.TryGetValue(group.ID, out info) && info.Members.TryGetValue(principal, out member))
            {
                gmem = new GroupMember(member);
                return true;
            }
            gmem = null;
            return false;
        }

        void IGroupMembersInterface.Update(UGUI requestingagent, UGI group, UGUI principal, bool acceptNotices, bool listInProfile)
        {
            MemoryGroupInfo info;
            MemoryGroupMember member;
            if (m_Groups.TryGetValue(group.ID, out info) && info.Members.TryGetValue(principal, out member))
            {
                member.IsAcceptNotices = acceptNotices;
                member.IsListInProfile = listInProfile;
                return;
            }
            throw new KeyNotFoundException();
        }
    }
}
