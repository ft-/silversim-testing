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
    public partial class MemoryGroupsService : IGroupRolemembersInterface
    {
        List<GroupRolemembership> IGroupRolemembersInterface.this[UGUI requestingAgent, UGUI principal]
        {
            get
            {
                var list = new List<GroupRolemembership>();
                foreach(MemoryGroupInfo info in m_Groups.Values)
                {
                    MemoryGroupMember mem;
                    MemoryGroupRole role;
                    if(info.Members.TryGetValue(principal, out mem) &&
                        info.Roles.TryGetValue(mem.ActiveRoleID, out role))
                    {
                        list.Add(new GroupRolemembership
                        {
                            Group = info.ID,
                            GroupTitle = role.Title,
                            Powers = role.Powers,
                            Principal = mem.Principal,
                            RoleID = mem.ActiveRoleID
                        });
                    }
                }
                return list;
            }
        }

        List<GroupRolemember> IGroupRolemembersInterface.this[UGUI requestingAgent, UGI group]
        {
            get
            {
                MemoryGroupInfo info;
                var list = new List<GroupRolemember>();
                if(m_Groups.TryGetValue(group.ID, out info))
                {
                    GroupRole everyonerole = info.Roles[UUID.Zero];
                    foreach(MemoryGroupMember mem in info.Members.Values)
                    {
                        list.Add(new GroupRolemember
                        {
                            RoleID = everyonerole.ID,
                            Group = info.ID,
                            Powers = everyonerole.Powers,
                            Principal = mem.Principal
                        });
                    }

                    foreach(MemoryGroupRole role in info.Roles.Values)
                    {
                        foreach(UGUI rmem in role.Rolemembers.Keys)
                        {
                            list.Add(new GroupRolemember
                            {
                                RoleID = role.ID,
                                Group = info.ID,
                                Powers = role.Powers,
                                Principal = rmem
                            });
                        }
                    }
                }
                return list;
            }
        }

        List<GroupRolemember> IGroupRolemembersInterface.this[UGUI requestingAgent, UGI group, UUID roleID]
        {
            get
            {
                MemoryGroupInfo info;
                var list = new List<GroupRolemember>();
                if (m_Groups.TryGetValue(group.ID, out info))
                {
                    if (roleID != UUID.Zero)
                    {
                        MemoryGroupRole role;
                        if (info.Roles.TryGetValue(roleID, out role))
                        {
                            foreach (UGUI rmem in role.Rolemembers.Keys)
                            {
                                list.Add(new GroupRolemember
                                {
                                    RoleID = role.ID,
                                    Group = info.ID,
                                    Powers = role.Powers,
                                    Principal = rmem
                                });
                            }
                        }
                    }
                    else
                    {
                        GroupRole everyonerole = info.Roles[UUID.Zero];
                        foreach (MemoryGroupMember mem in info.Members.Values)
                        {
                            list.Add(new GroupRolemember
                            {
                                RoleID = everyonerole.ID,
                                Group = info.ID,
                                Powers = everyonerole.Powers,
                                Principal = mem.Principal
                            });
                        }
                    }
                }
                return list;
            }
        }

        void IGroupRolemembersInterface.Add(UGUI requestingAgent, GroupRolemember rolemember)
        {
            m_Groups[rolemember.Group.ID].Roles[rolemember.RoleID].Rolemembers.Add(rolemember.Principal, true);
        }

        bool IGroupRolemembersInterface.ContainsKey(UGUI requestingAgent, UGI group, UUID roleID, UGUI principal)
        {
            MemoryGroupInfo info;
            MemoryGroupRole role;
            if(roleID != UUID.Zero)
            {
                return m_Groups.TryGetValue(group.ID, out info) && info.Roles.TryGetValue(roleID, out role) && role.Rolemembers.ContainsKey(principal);
            }
            else
            {
                return m_Groups.TryGetValue(group.ID, out info) && info.Members.ContainsKey(principal);
            }
        }

        void IGroupRolemembersInterface.Delete(UGUI requestingAgent, UGI group, UUID roleID, UGUI principal)
        {
            if(!m_Groups[group.ID].Roles[roleID].Rolemembers.Remove(principal))
            {
                throw new KeyNotFoundException();
            }
        }

        bool IGroupRolemembersInterface.TryGetValue(UGUI requestingAgent, UGI group, UUID roleID, UGUI principal, out GroupRolemember grolemem)
        {
            MemoryGroupInfo info;
            MemoryGroupRole role;
            grolemem = null;
            if (roleID != UUID.Zero)
            {
                if(m_Groups.TryGetValue(group.ID, out info) && info.Roles.TryGetValue(roleID, out role) && role.Rolemembers.ContainsKey(principal))
                {
                    grolemem = new GroupRolemember
                    {
                        Group = info.ID,
                        Powers = role.Powers,
                        Principal = principal,
                        RoleID = roleID
                    };
                }
            }
            else
            {
                if(m_Groups.TryGetValue(group.ID, out info) && info.Roles.TryGetValue(roleID, out role) && info.Members.ContainsKey(principal))
                {
                    grolemem = new GroupRolemember
                    {
                        Group = info.ID,
                        Powers = role.Powers,
                        Principal = principal,
                        RoleID = roleID
                    };
                }
            }
            return grolemem != null;
        }
    }
}
