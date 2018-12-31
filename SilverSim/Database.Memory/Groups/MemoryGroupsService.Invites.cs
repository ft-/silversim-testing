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
    public partial class MemoryGroupsService : IGroupInvitesInterface
    {
        List<GroupInvite> IGroupInvitesInterface.this[UGUI requestingAgent, UGUI principal]
        {
            get
            {
                var res = new List<GroupInvite>();
                foreach(KeyValuePair<UUID, UUID> kvp in m_GroupInvitesToGroup)
                {
                    MemoryGroupInfo info;
                    GroupInvite invite;
                    if(m_Groups.TryGetValue(kvp.Value, out info) && 
                        info.Invites.TryGetValue(kvp.Key, out invite) &&
                        invite.Principal.EqualsGrid(principal))
                    {
                        res.Add(new GroupInvite(invite));
                    }
                }
                return res;
            }
        }

        List<GroupInvite> IGroupInvitesInterface.this[UGUI requestingAgent, UGI group, UUID roleID, UGUI principal]
        {
            get
            {
                var res = new List<GroupInvite>();
                foreach (KeyValuePair<UUID, UUID> kvp in m_GroupInvitesToGroup)
                {
                    MemoryGroupInfo info;
                    GroupInvite invite;
                    if (m_Groups.TryGetValue(kvp.Value, out info) &&
                        info.Invites.TryGetValue(kvp.Key, out invite) &&
                        invite.Principal.EqualsGrid(principal) &&
                        invite.RoleID == roleID)
                    {
                        res.Add(new GroupInvite(invite));
                    }
                }
                return res;
            }
        }

        bool IGroupInvitesInterface.DoesSupportListGetters => true;

        void IGroupInvitesInterface.Add(UGUI requestingAgent, GroupInvite invite)
        {
            m_GroupInvitesToGroup.Add(invite.ID, invite.Group.ID);
            try
            {
                m_Groups[invite.Group.ID].Invites.Add(invite.ID, new GroupInvite(invite));
            }
            catch
            {
                m_GroupInvitesToGroup.Remove(invite.ID);
                throw;
            }
        }

        bool IGroupInvitesInterface.ContainsKey(UGUI requestingAgent, UUID groupInviteID) =>
            m_GroupInvitesToGroup.ContainsKey(groupInviteID);

        void IGroupInvitesInterface.Delete(UGUI requestingAgent, UUID inviteID)
        {
            UUID groupID;
            MemoryGroupInfo info;
            if(m_GroupInvitesToGroup.Remove(inviteID, out groupID) &&
                m_Groups.TryGetValue(groupID,out info) &&
                info.Invites.Remove(inviteID))
            {
                return;
            }
            throw new KeyNotFoundException();
        }

        List<GroupInvite> IGroupInvitesInterface.GetByGroup(UGUI requestingAgent, UGI group)
        {
            MemoryGroupInfo info;
            var res = new List<GroupInvite>();
            if(m_Groups.TryGetValue(group.ID, out info))
            {
                foreach(GroupInvite inv in info.Invites.Values)
                {
                    res.Add(new GroupInvite(inv));
                }
            }
            return res;
        }

        bool IGroupInvitesInterface.TryGetValue(UGUI requestingAgent, UUID groupInviteID, out GroupInvite ginvite)
        {
            MemoryGroupInfo grpInfo;
            GroupInvite intInvite;
            UUID groupID;
            if(m_GroupInvitesToGroup.TryGetValue(groupInviteID, out groupID) &&
                m_Groups.TryGetValue(groupID, out grpInfo) &&
                grpInfo.Invites.TryGetValue(groupInviteID, out intInvite))
            {
                ginvite = new GroupInvite(intInvite);
                return true;
            }
            ginvite = null;
            return false;
        }
    }
}
