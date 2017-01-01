// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;

namespace SilverSim.Groups.Common
{
    partial class DefaultPermissionsGroupsService : GroupsServiceInterface.IGroupsInterface
    {
        GroupInfo IGroupsInterface.this[UUI requestingAgent, string groupName]
        {
            get
            {
                return m_InnerService.Groups[requestingAgent, groupName];
            }
        }

        GroupInfo IGroupsInterface.this[UUI requestingAgent, UGI group]
        {
            get
            {
                return m_InnerService.Groups[requestingAgent, group];
            }
        }

        UGI IGroupsInterface.this[UUI requestingAgent, UUID groupID]
        {
            get
            {
                return m_InnerService.Groups[requestingAgent, groupID];
            }
        }

        bool IGroupsInterface.ContainsKey(UUI requestingAgent, string groupName)
        {
            return m_InnerService.Groups.ContainsKey(requestingAgent, groupName);
        }

        bool IGroupsInterface.ContainsKey(UUI requestingAgent, UGI groupID)
        {
            return m_InnerService.Groups.ContainsKey(requestingAgent, groupID);
        }

        bool IGroupsInterface.ContainsKey(UUI requestingAgent, UUID groupID)
        {
            return m_InnerService.Groups.ContainsKey(requestingAgent, groupID);
        }

        GroupInfo IGroupsInterface.Create(UUI requestingAgent, GroupInfo group)
        {
            return m_InnerService.Groups.Create(requestingAgent, group);
        }

        void IGroupsInterface.Delete(UUI requestingAgent, GroupInfo group)
        {
            if(!IsGroupOwner(group.ID, requestingAgent))
            {
                throw new GroupInsufficientPowersException(GroupPowers.OwnerPowers);
            }
            m_InnerService.Groups.Delete(requestingAgent, group);
        }

        List<DirGroupInfo> IGroupsInterface.GetGroupsByName(UUI requestingAgent, string query)
        {
            return m_InnerService.Groups.GetGroupsByName(requestingAgent, query);
        }

        bool IGroupsInterface.TryGetValue(UUI requestingAgent, string groupName, out GroupInfo groupInfo)
        {
            return m_InnerService.Groups.TryGetValue(requestingAgent, groupName, out groupInfo);
        }

        bool IGroupsInterface.TryGetValue(UUI requestingAgent, UGI groupID, out GroupInfo groupInfo)
        {
            return m_InnerService.Groups.TryGetValue(requestingAgent, groupID, out groupInfo);
        }

        bool IGroupsInterface.TryGetValue(UUI requestingAgent, UUID groupID, out UGI ugi)
        {
            return m_InnerService.Groups.TryGetValue(requestingAgent, groupID, out ugi);
        }

        GroupInfo IGroupsInterface.Update(UUI requestingAgent, GroupInfo group)
        {
            VerifyAgentPowers(group.ID, requestingAgent, GroupPowers.ChangeOptions);
            return m_InnerService.Groups.Update(requestingAgent, group);
        }
    }
}
