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

namespace SilverSim.Database.Memory.Groups
{
    public partial class MemoryGroupsService : IGroupRolesInterface
    {
        List<GroupRole> IGroupRolesInterface.this[UGUI requestingAgent, UGI group]
        {
            get
            {
                MemoryGroupInfo info;
                var list = new List<GroupRole>();
                if (m_Groups.TryGetValue(group.ID, out info))
                {
                    foreach (MemoryGroupRole role in info.Roles.Values)
                    {
                        list.Add(new GroupRole(role)
                        {
                            Members = (uint)(role.ID == UUID.Zero ? info.Members.Count : role.Rolemembers.Count)
                        });
                    }
                }
                return list;
            }
        }

        List<GroupRole> IGroupRolesInterface.this[UGUI requestingAgent, UGI group, UGUI principal]
        {
            get
            {
                MemoryGroupInfo info;
                var list = new List<GroupRole>();
                if (m_Groups.TryGetValue(group.ID, out info) && info.Members.ContainsKey(principal))
                {
                    foreach (MemoryGroupRole role in info.Roles.Values)
                    {
                        if (role.Rolemembers.ContainsKey(principal))
                        {
                            list.Add(new GroupRole(role)
                            {
                                Members = (uint)(role.ID == UUID.Zero ? info.Members.Count : role.Rolemembers.Count)
                            });
                        }
                    }
                }
                return list;
            }
        }

        void IGroupRolesInterface.Add(UGUI requestingAgent, GroupRole role)
        {
            MemoryGroupInfo info;
            if (m_Groups.TryGetValue(role.Group.ID, out info))
            {
                info.Roles.Add(role.ID, new MemoryGroupRole(role));
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }

        bool IGroupRolesInterface.ContainsKey(UGUI requestingAgent, UGI group, UUID roleID)
        {
            MemoryGroupInfo info;
            return m_Groups.TryGetValue(group.ID, out info) && info.Roles.ContainsKey(roleID);
        }

        void IGroupRolesInterface.Delete(UGUI requestingAgent, UGI group, UUID roleID)
        {
            MemoryGroupInfo info;
            if (!m_Groups.TryGetValue(group.ID, out info) || !info.Roles.Remove(roleID))
            {
                throw new KeyNotFoundException();
            }
        }

        bool IGroupRolesInterface.TryGetValue(UGUI requestingAgent, UGI group, UUID roleID, out GroupRole groupRole)
        {
            MemoryGroupInfo info;
            MemoryGroupRole role;
            if (m_Groups.TryGetValue(group.ID, out info) && info.Roles.TryGetValue(roleID, out role))
            {
                groupRole = new GroupRole(role)
                {
                    Members = (uint)(role.ID == UUID.Zero ? info.Members.Count : role.Rolemembers.Count)
                };
                return true;
            }
            groupRole = null;
            return false;
        }

        void IGroupRolesInterface.Update(UGUI requestingAgent, GroupRole role)
        {
            MemoryGroupInfo info;
            MemoryGroupRole mrole;
            if (m_Groups.TryGetValue(role.Group.ID, out info) && info.Roles.TryGetValue(role.ID, out mrole))
            {
                mrole.Name = role.Name;
                mrole.Description = role.Description;
                mrole.Title = role.Title;
                mrole.Powers = role.Powers;
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }
    }
}
