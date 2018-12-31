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
    public partial class MemoryGroupsService : IGroupMembershipsInterface
    {
        List<GroupMembership> IGroupMembershipsInterface.this[UGUI requestingAgent, UGUI principal]
        {
            get
            {
                var list = new List<GroupMembership>();

                foreach(MemoryGroupInfo info in m_Groups.Values)
                {
                    MemoryGroupMember mem;
                    if(!info.Members.TryGetValue(principal, out mem))
                    {
                        continue;
                    }
                    foreach(MemoryGroupRole role in info.Roles.Values)
                    {
                        if(!role.Rolemembers.ContainsKey(principal))
                        {
                            continue;
                        }
                        list.Add(new GroupMembership(info, mem, role, mem.ActiveRoleID));
                    }
                }
                return list;
            }
        }

        bool IGroupMembershipsInterface.ContainsKey(UGUI requestingAgent, UGI group, UGUI principal)
        {
            MemoryGroupInfo info;
            return m_Groups.TryGetValue(group.ID, out info) && info.Members.ContainsKey(principal);
        }

        bool IGroupMembershipsInterface.TryGetValue(UGUI requestingAgent, UGI group, UGUI principal, out GroupMembership gmem)
        {
            MemoryGroupInfo info;
            MemoryGroupMember mem;
            MemoryGroupRole role;
            if(m_Groups.TryGetValue(group.ID, out info) && info.Members.TryGetValue(principal, out mem) &&
                info.Roles.TryGetValue(mem.ActiveRoleID, out role))
            {
                gmem = new GroupMembership(info, mem, role, mem.ActiveRoleID);
                return true;
            }
            gmem = null;
            return false;
        }
    }
}
